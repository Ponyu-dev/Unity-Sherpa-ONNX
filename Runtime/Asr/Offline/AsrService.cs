using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Asr.Config;
using PonyuDev.SherpaOnnx.Asr.Offline.Config;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;

namespace PonyuDev.SherpaOnnx.Asr.Offline
{
    /// <summary>
    /// High-level facade for ASR operations.
    /// Pure POCO — suitable for constructor injection via VContainer or
    /// manual instantiation from a MonoBehaviour.
    /// Never throws at runtime — logs errors instead.
    /// </summary>
    public sealed class AsrService : IAsrService
    {
        private AsrSettingsData _settings;
        private IAsrEngine _engine;
        private AsrProfile _activeProfile;
        // Last InitializeAsync's onEvent — re-fired by SwitchProfile so
        // bus subscribers see profile changes after the initial load.
        private Action<ProfileReadyEvent> _onEvent;
        // True while a profile switch is mid-flight. CheckReady bails
        // so Recognize / RecognizeAsync return null instead of racing
        // against the native engine being torn down.
        private volatile bool _isSwitching;
        private AsrProfile _profilePendingLoad;

        // Method-group target for UniTask.RunOnThreadPool inside
        // InitializeAsync — keeps the call site lambda-free.
        private void LoadPendingProfile()
        {
            var p = _profilePendingLoad;
            _profilePendingLoad = null;
            if (p != null)
                LoadProfile(p);
        }

        public AsrService() { }

        /// <summary>Test-only: injects a pre-built engine.</summary>
        internal AsrService(IAsrEngine engine)
        {
            _engine = engine;
        }

        /// <summary>Test-only: directly sets settings data.</summary>
        internal void SetSettings(AsrSettingsData settings)
        {
            _settings = settings;
        }

        /// <summary>True when the engine is loaded and ready to recognize.</summary>
        public bool IsReady => _engine?.IsLoaded ?? false;

        /// <summary>Currently loaded ASR profile.</summary>
        public AsrProfile ActiveProfile => _activeProfile;

        /// <summary>All loaded profiles (available after Initialize).</summary>
        public AsrSettingsData Settings => _settings;

        /// <inheritdoc />
        public bool IsProfileAvailable(string profileName)
        {
            if (_settings?.profiles == null || string.IsNullOrEmpty(profileName))
                return false;
            if (_activeProfile != null
                && string.Equals(_activeProfile.profileName, profileName, StringComparison.Ordinal))
                return true;

            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
                return false;

            string modelDir = AsrModelPathResolver.GetModelDirectory(
                profile.profileName, profile.modelSource);
            return ProfileAvailability.IsAvailable(profile, modelDir);
        }

        /// <summary>Number of concurrent native engine instances.</summary>
        public int EnginePoolSize
        {
            get => _engine?.PoolSize ?? 1;
            set => _engine?.Resize(value);
        }

        // ── Lifecycle ──

        /// <summary>
        /// Loads asr-settings.json and initializes the engine
        /// with the active profile.
        /// </summary>
        public void Initialize()
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] AsrService initializing...");

            _settings = AsrSettingsLoader.Load();
            var profile = AsrSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] AsrService: no active profile found.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = AsrModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
                if (!System.IO.Directory.Exists(dir))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] AsrService: LocalZip profile not yet extracted. Use InitializeAsync() instead.");
                    return;
                }
            }

            LoadProfile(profile);

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] AsrService initialized.");
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
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] AsrService async initializing...");

            _onEvent = onEvent;
            _settings = await AsrSettingsLoader.LoadAsync(ProfileReadyEvents.AsExtractProgress(onEvent), ct);

            var profile = AsrSettingsLoader.GetActiveProfile(_settings);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] AsrService: no active profile found.");
                ProfileReadyEvents.EmitFailed(onEvent, "No active profile.");
                return;
            }

            const string serviceName = "AsrService";
            string subfolder = AsrModelPathResolver.ModelsSubfolder;
            if (!await ProfileSourceResolver.EnsureLocalZipReadyAsync(profile, subfolder, serviceName, onEvent, ct))
                return;
            if (!await ProfileSourceResolver.EnsureRemoteReadyAsync(profile, subfolder, serviceName, onEvent, ct))
                return;
            if (!await ProfileSourceResolver.EnsureLocalReadyAsync(profile, subfolder, serviceName, onEvent, ct))
                return;

            ProfileReadyEvents.EmitInit(onEvent, 0);
            _profilePendingLoad = profile;
            await UniTask.RunOnThreadPool(LoadPendingProfile, cancellationToken: ct);

            if (!IsReady)
            {
                ProfileReadyEvents.EmitFailed(onEvent, "Engine failed to load.");
                return;
            }

            ProfileReadyEvents.EmitInit(onEvent, 100);

            // Sweep stale on-disk extractions once we have a confirmed
            // active profile. SwitchToProfile re-runs the same sweep
            // each time the user picks a different profile.
            EnforceKeepOnlyActive();

            ProfileReadyEvents.EmitReady(onEvent);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] AsrService async initialized.");
        }

        /// <summary>
        /// Loads (or reloads) the engine with the given profile.
        /// </summary>
        public void LoadProfile(AsrProfile profile)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService.LoadProfile: profile is null.");
                return;
            }

            EnsureEngine();

            if (_engine == null)
                return;

            string modelDir = AsrModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
            int poolSize = _settings?.offlineRecognizerPoolSize ?? 1;
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
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService.SwitchProfile: no profiles loaded.");
                return;
            }

            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrService.SwitchProfile: " +
                    $"index {index} out of range " +
                    $"(0..{_settings.profiles.Count - 1}).");
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
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService.SwitchProfile: no profiles loaded.");
                return;
            }

            var profile = _settings.profiles
                .FirstOrDefault(p => p.profileName == profileName);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] AsrService.SwitchProfile: profile '{profileName}' not found.");
                return;
            }

            SwitchToProfile(profile);
        }

        /// <summary>
        /// Async profile switch — native engine load runs on the
        /// thread pool so the UI thread is free during the multi-
        /// second sherpa-onnx OfflineRecognizer ctor.
        /// </summary>
        public async UniTask SwitchProfileAsync(int index, CancellationToken ct = default)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrService.SwitchProfileAsync: " +
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
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrService.SwitchProfileAsync: profile '{profileName}' not found.");
                return;
            }
            await SwitchToProfileAsync(profile, ct);
        }

        // Loads the new profile and, on success, runs the keep-only-
        // active sweep + re-fires Ready through the cached onEvent so
        // bus subscribers re-render with the new ActiveProfile.
        // Re-fires Failed if the load fails.
        private void SwitchToProfile(AsrProfile newProfile)
        {
            _isSwitching = true;
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
                _isSwitching = false;
            }
        }

        // Async variant — offloads the native engine ctor to the thread
        // pool through the same _profilePendingLoad / LoadPendingProfile
        // path InitializeAsync uses, so the multi-second sherpa-onnx
        // OfflineRecognizer ctor never runs on the main thread. The
        // _isSwitching flag gates Recognize / RecognizeAsync for the
        // duration so worker-thread recognitions don't race against
        // the native engine reload.
        private async UniTask SwitchToProfileAsync(AsrProfile newProfile, CancellationToken ct)
        {
            _isSwitching = true;
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
                _isSwitching = false;
            }
        }

        // When keepOnlyActiveProfile (or its build-time alias
        // buildOnlyActiveProfile) is set, removes every extracted
        // profile dir registered in this service's profile list
        // except the currently-active one. Critical: iterates only
        // OFFLINE profiles, never touches online-ASR extractions —
        // both share asr-models/ on disk, and a directory-wide sweep
        // would delete the other service's active model.
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
                $"[SherpaOnnx] ASR (offline) keep-only-active: keeping " +
                $"'{keep}', sweeping {_settings.profiles.Count - 1} other " +
                "offline profile(s)…");

            int removed = 0;
            for (int i = 0; i < _settings.profiles.Count; i++)
            {
                var p = _settings.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.profileName))
                    continue;
                if (string.Equals(p.profileName, keep, StringComparison.Ordinal))
                    continue;
                if (LocalZipExtractor.TryDeleteExtractedModel(
                    AsrModelPathResolver.ModelsSubfolder, p.profileName))
                    removed++;
            }

            if (removed == 0)
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] ASR (offline) keep-only-active: nothing on disk to remove.");
            }
        }

        // ── Recognition ──

        /// <summary>
        /// Recognizes speech from PCM audio samples.
        /// Returns null if the service is not ready.
        /// </summary>
        public AsrResult Recognize(float[] samples, int sampleRate)
        {
            if (!CheckReady())
                return null;

            return _engine.Recognize(samples, sampleRate);
        }

        /// <summary>
        /// Recognizes speech on a background thread.
        /// Returns null if the service is not ready.
        /// </summary>
        public Task<AsrResult> RecognizeAsync(float[] samples, int sampleRate)
        {
            if (!CheckReady())
                return Task.FromResult<AsrResult>(null);

            var engine = _engine;
            if (engine == null)
                return Task.FromResult<AsrResult>(null);

            return Task.Run(() => engine.Recognize(samples, sampleRate));
        }

        // ── Disk usage ──

        /// <inheritdoc />
        public IReadOnlyList<string> GetExtractedProfiles()
            => LocalZipExtractor.ListExtractedProfiles(AsrModelPathResolver.ModelsSubfolder);

        /// <inheritdoc />
        public long GetExtractedProfileSizeBytes(string profileName)
            => LocalZipExtractor.GetExtractedSizeBytes(AsrModelPathResolver.ModelsSubfolder, profileName);

        /// <inheritdoc />
        public bool TryDeleteExtractedProfile(string profileName)
            => LocalZipExtractor.TryDeleteExtractedModel(AsrModelPathResolver.ModelsSubfolder, profileName);

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
                AsrModelPathResolver.ModelsSubfolder, keep);
        }

        // ── Cleanup ──

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
            _activeProfile = null;
            _settings = null;
            _onEvent = null;
        }

        // ── Private ──

        private void EnsureEngine()
        {
#if SHERPA_ONNX
            _engine ??= new AsrEngine();
#else
            if (_engine == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] SHERPA_ONNX scripting define is not set. " +
                    "ASR engine cannot be created.");
            }
#endif
        }

        private bool CheckReady()
        {
            if (_isSwitching)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] AsrService is busy switching profile — request ignored. " +
                    "Wait for the next ProfileReadyPhase.Ready event before retrying.");
                return false;
            }
            if (_engine != null && _engine.IsLoaded)
                return true;

            SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService is not initialized. Call Initialize() first.");
            return false;
        }
    }
}