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

        /// <summary>
        /// Service-level cancellation source. Cancelled in <see cref="Dispose"/>
        /// so any in-flight async generation aborts cleanly when the service
        /// goes away. Linked into every async method via
        /// <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, CancellationToken)"/>.
        /// </summary>
        private CancellationTokenSource _serviceCts = new();

        /// <summary>True when the engine is loaded and ready to generate.</summary>
        public bool IsReady => _engine?.IsLoaded ?? false;

        /// <summary>Currently loaded TTS profile.</summary>
        public TtsProfile ActiveProfile => _activeProfile;

        /// <summary>All loaded profiles (available after Initialize).</summary>
        public TtsSettingsData Settings => _settings;

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

        // Captures the previously active profile, loads the new one, and —
        // if the engine reports itself ready and TtsSettingsData.
        // autoDeletePreviousProfile is set — frees the disk space used by
        // the previous LocalZip extraction. Failed loads leave the old
        // extraction intact.
        private void SwitchToProfile(TtsProfile newProfile)
        {
            var previous = _activeProfile;
            LoadProfile(newProfile);

            if (!IsReady)
                return;

            if (_settings != null
                && _settings.autoDeletePreviousProfile
                && previous != null
                && !string.IsNullOrEmpty(previous.profileName)
                && !string.Equals(previous.profileName, newProfile.profileName, StringComparison.Ordinal))
            {
                // Local / Remote / LocalZip all land in the same per-profile
                // dir under persistentDataPath on Android — drop it. On
                // non-Android nothing is extracted, so this is a no-op.
                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] TTS auto-delete: switching " +
                    $"'{previous.profileName}' → '{newProfile.profileName}', " +
                    $"removing previous extraction…");
                LocalZipExtractor.TryDeleteExtractedModel(
                    TtsModelPathResolver.ModelsSubfolder, previous.profileName);
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
            if (_engine != null && _engine.IsLoaded)
                return true;
            
            SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService is not initialized. Call Initialize() first.");
            return false;

        }
    }
}
