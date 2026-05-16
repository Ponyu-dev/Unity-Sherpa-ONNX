using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Tts.Config;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// High-level facade for TTS operations.
    /// Pure POCO — suitable for constructor injection via VContainer or
    /// manual instantiation from a MonoBehaviour.
    /// Never throws at runtime — logs errors instead.
    /// </summary>
    public sealed partial class TtsService : ITtsService
    {
        private TtsSettingsData _settings;
        private ITtsEngine _engine;
        private TtsProfile _activeProfile;
        // Last InitializeAsync's onEvent — re-fired by SwitchProfile so
        // bus subscribers see profile changes after the initial load.
        private Action<ProfileReadyEvent> _onEvent;

        /// <summary>
        /// Service-level cancellation source. Cancelled in <see cref="Dispose"/>
        /// so any in-flight async generation aborts cleanly when the service
        /// goes away. Linked into every async method via
        /// <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, CancellationToken)"/>.
        /// Also cancelled-and-replaced at the start of every profile switch
        /// so worker-thread generations exit before the native engine is
        /// torn down on another worker by SwitchToProfileAsync.
        /// </summary>
        private CancellationTokenSource _serviceCts = new();

        /// <summary>
        /// True while a profile switch (sync or async) is mid-flight.
        /// Read by <see cref="CheckReady"/> so every API entry point
        /// (Generate, GenerateAsync, callbacks, …) bails with a warning
        /// instead of racing against a half-loaded native engine.
        /// <c>volatile</c> because the flag is written from the worker
        /// thread that owns the switch and read from the main thread
        /// (and other workers running async generations).
        /// </summary>
        private volatile bool _isSwitching;

        /// <summary>True when the engine is loaded and ready to generate.</summary>
        public bool IsReady => _engine?.IsLoaded ?? false;

        /// <summary>Currently loaded TTS profile.</summary>
        public TtsProfile ActiveProfile => _activeProfile;

        /// <summary>All loaded profiles (available after Initialize).</summary>
        public TtsSettingsData Settings => _settings;

        /// <inheritdoc />
        public bool IsProfileAvailable(string profileName)
        {
            if (_settings?.profiles == null || string.IsNullOrEmpty(profileName))
                return false;

            // Currently-active profile stays in memory even if a sweep
            // just deleted its files — switching to itself is a no-op,
            // so we always report it as available.
            if (_activeProfile != null
                && string.Equals(_activeProfile.profileName, profileName, StringComparison.Ordinal))
                return true;

            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
                return false;

            string modelDir = TtsModelPathResolver.GetModelDirectory(
                profile.profileName, profile.modelSource);
            return ProfileAvailability.IsAvailable(profile, modelDir);
        }

        /// <summary>Sample rate of the loaded engine in Hz, or 0 if not loaded.</summary>
        public int SampleRate => _engine?.SampleRate ?? 0;

        /// <summary>Number of concurrent native engine instances.</summary>
        public int EnginePoolSize
        {
            get => _engine?.PoolSize ?? 1;
            set => _engine?.Resize(value);
        }

        // ── Lifecycle ──

        /// <summary>
        /// Loads tts-settings.json and initializes the engine
        /// with the active profile.
        /// </summary>
        public void Initialize()
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService initializing...");

            _settings = TtsSettingsLoader.Load();
            var profile = TtsSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] TtsService: no active profile found.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = TtsModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
                if (!System.IO.Directory.Exists(dir))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] TtsService: LocalZip profile not yet extracted. Use InitializeAsync() instead.");
                    return;
                }
            }

            LoadProfile(profile);

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService initialized.");
        }

        /// <summary>
        /// Async initialization: extracts files on Android,
        /// loads settings, and starts the engine.
        /// Works on all platforms.
        /// </summary>
        public async UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService async initializing...");

            // Cache the callback so SwitchProfile can republish Ready /
            // Failed when the user picks a different profile at runtime
            // — without that, status subscribers wired up through the
            // bus stay frozen on the original Ready snapshot.
            _onEvent = onEvent;

            // Shared StreamingAssets (settings JSON + anything outside
            // a per-profile dir) is the first piece to land on disk.
            // We bridge its IProgress<float> into the Extract phase so
            // first-launch progress is visible while the manifest gets
            // staged on Android.
            _settings = await TtsSettingsLoader.LoadAsync(ProfileReadyEvents.AsExtractProgress(onEvent), ct);

            var profile = TtsSettingsLoader.GetActiveProfile(_settings);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] TtsService: no active profile found.");
                ProfileReadyEvents.EmitFailed(onEvent, "No active profile.");
                return;
            }

            const string serviceName = "TtsService";
            string subfolder = TtsModelPathResolver.ModelsSubfolder;
            if (!await ProfileSourceResolver.EnsureLocalZipReadyAsync(profile, subfolder, serviceName, onEvent, ct))
                return;
            if (!await ProfileSourceResolver.EnsureRemoteReadyAsync(profile, subfolder, serviceName, onEvent, ct))
                return;
            if (!await ProfileSourceResolver.EnsureLocalReadyAsync(profile, subfolder, serviceName, onEvent, ct))
                return;

            // Native engine construction (sherpa-onnx OfflineTts ctor —
            // ONNX/lexicon/data parsing) is multi-second per instance and
            // scales with EnginePoolSize. It is pure C/C++ via P/Invoke
            // with no Unity API access, so run it off the main thread to
            // keep the UI responsive while the engines warm up.
            ProfileReadyEvents.EmitInit(onEvent, 0);
            _profilePendingLoad = profile;
            await UniTask.RunOnThreadPool(LoadPendingProfile, cancellationToken: ct);

            if (!IsReady)
            {
                ProfileReadyEvents.EmitFailed(onEvent, "Engine failed to load.");
                return;
            }

            ProfileReadyEvents.EmitInit(onEvent, 100);

            // Sweep stale extractions (from previous switches or
            // cancelled imports) once we have a confirmed-loaded
            // active profile. SwitchToProfile re-runs the same sweep
            // every time the user picks a different profile.
            EnforceKeepOnlyActive();

            ProfileReadyEvents.EmitReady(onEvent);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService async initialized.");
        }

        private TtsProfile _profilePendingLoad;
        private void LoadPendingProfile()
        {
            var p = _profilePendingLoad;
            _profilePendingLoad = null;
            if (p != null)
                LoadProfile(p);
        }

        /// <summary>
        /// Loads (or reloads) the engine with the given profile.
        /// </summary>
        public void LoadProfile(TtsProfile profile)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService.LoadProfile: profile is null.");
                return;
            }

            EnsureEngine();

            if (_engine == null)
                return;

            string modelDir = TtsModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);

            int poolSize = _settings?.cache?.offlineTtsPoolSize ?? 1;
            _engine.Load(profile, modelDir, poolSize);
            _activeProfile = profile;
        }

        /// <summary>
        /// Switches to a profile by index in the profiles list.
        /// </summary>
        public void SwitchProfile(int index)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService.SwitchProfile: no profiles loaded.");
                return;
            }

            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsService.SwitchProfile: " +
                    $"index {index} out of range (0..{_settings.profiles.Count - 1}).");
                return;
            }

            SwitchToProfile(_settings.profiles[index]);
        }

        /// <summary>
        /// Switches to a profile by name.
        /// </summary>
        public void SwitchProfile(string profileName)
        {
            if (_settings?.profiles == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService.SwitchProfile: no profiles loaded.");
                return;
            }

            var profile = _settings.profiles
                .FirstOrDefault(p => p.profileName == profileName);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsService.SwitchProfile: " +
                    $"profile '{profileName}' not found.");
                return;
            }

            SwitchToProfile(profile);
        }

        /// <summary>
        /// Async profile switch — native engine load runs on the
        /// thread pool so the UI thread is free during the multi-
        /// second sherpa-onnx OfflineTts ctor. Re-fires the cached
        /// <c>onEvent</c> with Init 0 → 100 → Ready (or Failed on
        /// engine load failure) so bus subscribers see the same
        /// progress phases as during InitializeAsync.
        /// </summary>
        public async UniTask SwitchProfileAsync(int index, CancellationToken ct = default)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsService.SwitchProfileAsync: " +
                    $"index {index} out of range (0..{_settings.profiles.Count - 1}).");
                return;
            }
            await SwitchToProfileAsync(_settings.profiles[index], ct);
        }

        /// <summary>Async profile switch by name.</summary>
        public async UniTask SwitchProfileAsync(string profileName, CancellationToken ct = default)
        {
            if (_settings?.profiles == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsService.SwitchProfileAsync: profile '{profileName}' not found.");
                return;
            }
            await SwitchToProfileAsync(profile, ct);
        }

        // Loads the new profile and, on success, runs the keep-only-
        // active sweep + re-fires the cached onEvent with Ready so
        // status-line subscribers (e.g. TtsInitProgressBus.Changed)
        // re-render with the new ActiveProfile name. On failure
        // re-fires Failed so the UI can switch to the red error path.
        // Sync overload — blocks the calling thread for the duration of
        // the native engine ctor, but uses the same busy-flag + cancel
        // pattern as the async overload to keep concurrent worker-thread
        // generations from racing against the load.
        private void SwitchToProfile(TtsProfile newProfile)
        {
            BeginSwitch();
            try
            {
                LoadProfile(newProfile);

                if (!IsReady)
                {
                    ProfileReadyEvents.EmitFailed(_onEvent, "Switch failed.");
                    return;
                }

                EnforceKeepOnlyActive();
                ProfileReadyEvents.EmitReady(_onEvent);
            }
            finally
            {
                EndSwitch();
            }
        }

        // Async variant — offloads the native engine ctor to the thread
        // pool through the same _profilePendingLoad / LoadPendingProfile
        // path InitializeAsync uses, so the multi-second sherpa-onnx
        // OfflineTts ctor never runs on the main thread.
        private async UniTask SwitchToProfileAsync(TtsProfile newProfile, CancellationToken ct)
        {
            BeginSwitch();
            try
            {
                ProfileReadyEvents.EmitInit(_onEvent, 0);

                _profilePendingLoad = newProfile;
                await UniTask.RunOnThreadPool(LoadPendingProfile, cancellationToken: ct);

                if (!IsReady)
                {
                    ProfileReadyEvents.EmitFailed(_onEvent, "Switch failed.");
                    return;
                }

                ProfileReadyEvents.EmitInit(_onEvent, 100);
                EnforceKeepOnlyActive();
                ProfileReadyEvents.EmitReady(_onEvent);
            }
            finally
            {
                EndSwitch();
            }
        }

        // Sets the busy flag and rotates the service-level CTS so any
        // in-flight async generation (LinkCt-bound to the previous CTS)
        // gets cancelled and exits its worker thread before the native
        // engine is torn down on another worker by LoadProfile. Paired
        // with EndSwitch in the finally block.
        private void BeginSwitch()
        {
            try { _serviceCts?.Cancel(); } catch { /* swallow */ }
            _serviceCts?.Dispose();
            _serviceCts = new CancellationTokenSource();
            _isSwitching = true;
        }

        private void EndSwitch()
        {
            _isSwitching = false;
        }

        // When keepOnlyActiveProfile (or its build-time alias
        // buildOnlyActiveProfile) is set, removes every extracted
        // profile dir registered in this service's profile list
        // except the currently-active one. Covers all three sources
        // (Local / Remote / LocalZip) — they share the same per-profile
        // path on disk and TryDeleteExtractedModel is source-agnostic.
        // Iterates the service's own settings.profiles rather than
        // sweeping the whole models directory so we never touch
        // extractions owned by other services that share the parent
        // (offline + online ASR both use asr-models/, for example).
        // Safe on the main thread; on non-Android desktop nothing is
        // extracted to persistentDataPath, so this is a no-op.
        private void EnforceKeepOnlyActive()
        {
            if (_settings == null || _settings.profiles == null)
                return;
            if (!_settings.keepOnlyActiveProfile && !_settings.buildOnlyActiveProfile)
                return;
            if (_activeProfile == null || string.IsNullOrEmpty(_activeProfile.profileName))
                return;

            string keep = _activeProfile.profileName;
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS keep-only-active: keeping '{keep}', " +
                $"sweeping {_settings.profiles.Count - 1} other profile(s)…");

            int removed = 0;
            for (int i = 0; i < _settings.profiles.Count; i++)
            {
                var p = _settings.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.profileName))
                    continue;
                if (string.Equals(p.profileName, keep, StringComparison.Ordinal))
                    continue;
                if (LocalZipExtractor.TryDeleteExtractedModel(
                    TtsModelPathResolver.ModelsSubfolder, p.profileName))
                    removed++;
            }

            if (removed == 0)
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] TTS keep-only-active: nothing on disk to remove.");
            }
        }

        // ── Generation ──

        /// <summary>
        /// Generates speech using the active profile's speed and speakerId.
        /// Returns null if the service is not ready.
        /// </summary>
        public TtsResult Generate(string text)
        {
            if (!CheckReady())
                return null;

            return _engine.Generate(
                text, _activeProfile.speed, _activeProfile.speakerId);
        }

        /// <summary>
        /// Generates speech with explicit speed and speakerId.
        /// Returns null if the service is not ready.
        /// </summary>
        public TtsResult Generate(string text, float speed, int speakerId)
        {
            if (!CheckReady())
                return null;

            return _engine.Generate(text, speed, speakerId);
        }

        /// <summary>
        /// Generates speech on a background thread, cancellable via
        /// <paramref name="ct"/>. Throws <see cref="OperationCanceledException"/>
        /// if cancelled (or if the service is disposed mid-flight).
        /// Returns null if the service is not ready.
        /// </summary>
        public async Task<TtsResult> GenerateAsync(
            string text, CancellationToken ct = default)
        {
            if (!CheckReady())
                return null;

            var engine = _engine;
            var profile = _activeProfile;
            if (engine == null || profile == null)
                return null;

            using var linked = LinkCt(ct);
            return await engine.GenerateAsync(
                text, profile.speed, profile.speakerId, linked.Token);
        }

        /// <summary>
        /// Generates speech on a background thread with explicit parameters,
        /// cancellable via <paramref name="ct"/>.
        /// Returns null if the service is not ready.
        /// </summary>
        public async Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId,
            CancellationToken ct = default)
        {
            if (!CheckReady())
                return null;

            var engine = _engine;
            if (engine == null)
                return null;

            using var linked = LinkCt(ct);
            return await engine.GenerateAsync(text, speed, speakerId, linked.Token);
        }

        // ── Disk usage ──

        /// <inheritdoc />
        public IReadOnlyList<string> GetExtractedProfiles()
            => LocalZipExtractor.ListExtractedProfiles(TtsModelPathResolver.ModelsSubfolder);

        /// <inheritdoc />
        public long GetExtractedProfileSizeBytes(string profileName)
            => LocalZipExtractor.GetExtractedSizeBytes(TtsModelPathResolver.ModelsSubfolder, profileName);

        /// <inheritdoc />
        public bool TryDeleteExtractedProfile(string profileName)
            => LocalZipExtractor.TryDeleteExtractedModel(TtsModelPathResolver.ModelsSubfolder, profileName);

        /// <inheritdoc />
        public int CleanupUnusedExtractedProfiles()
        {
            var keep = new List<string>();
            if (_settings?.profiles != null)
            {
                foreach (var p in _settings.profiles)
                {
                    if (!string.IsNullOrEmpty(p?.profileName))
                        keep.Add(p.profileName);
                }
            }
            return LocalZipExtractor.CleanupUnusedProfiles(
                TtsModelPathResolver.ModelsSubfolder, keep);
        }

        // ── Cleanup ──

        public void Dispose()
        {
            // Cancel any in-flight async generations so their callbacks
            // return 0 and the native call exits before we dispose the engine.
            try { _serviceCts?.Cancel(); } catch { /* swallow */ }

            _engine?.Dispose();
            _engine = null;
            _activeProfile = null;
            _settings = null;
            // Drop the cached InitializeAsync callback so we don't
            // hold the reference past the service's lifetime.
            _onEvent = null;

            _serviceCts?.Dispose();
            _serviceCts = null;
        }

        /// <summary>
        /// Builds a linked CTS from the caller's token and the service-level
        /// token. Either source cancelling aborts the operation. Always
        /// dispose with <c>using</c> on the call site.
        /// </summary>
        internal CancellationTokenSource LinkCt(CancellationToken ct)
        {
            var serviceToken = _serviceCts?.Token ?? CancellationToken.None;
            return CancellationTokenSource.CreateLinkedTokenSource(ct, serviceToken);
        }

        // ── Private ──

        private void EnsureEngine()
        {
#if SHERPA_ONNX
            _engine ??= new TtsEngine();
#else
            if (_engine == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] SHERPA_ONNX scripting define is not set. " +
                    "TTS engine cannot be created.");
            }
#endif
        }

        private bool CheckReady()
        {
            if (_isSwitching)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsService is busy switching profile — request ignored. " +
                    "Wait for the next ProfileReadyPhase.Ready event before retrying.");
                return false;
            }
            if (_engine != null && _engine.IsLoaded)
                return true;

            SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService is not initialized. Call Initialize() first.");
            return false;
        }
    }
}
