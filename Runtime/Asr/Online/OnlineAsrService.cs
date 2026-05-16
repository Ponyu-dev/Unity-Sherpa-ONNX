using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Asr.Config;
using PonyuDev.SherpaOnnx.Asr.Online.Config;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;

namespace PonyuDev.SherpaOnnx.Asr.Online
{
    /// <summary>
    /// High-level POCO facade for streaming ASR.
    /// Never throws at runtime — logs errors instead.
    /// </summary>
    public sealed class OnlineAsrService : IOnlineAsrService
    {
        private OnlineAsrSettingsData _settings;
        private IOnlineAsrEngine _engine;
        private OnlineAsrProfile _activeProfile;
        // Last InitializeAsync's onEvent — re-fired by SwitchProfile so
        // bus subscribers see profile changes after the initial load.
        private Action<ProfileReadyEvent> _onEvent;
        // True while a profile switch is mid-flight. Hot-path streaming
        // methods (AcceptSamples / ProcessAvailableFrames / ResetStream)
        // bail early so a frame-by-frame mic feeder never collides with
        // the native engine being torn down on a worker thread.
        private volatile bool _isSwitching;

        public OnlineAsrService() { }

        /// <summary>Test-only: injects a pre-built engine.</summary>
        internal OnlineAsrService(IOnlineAsrEngine engine)
        {
            _engine = engine;
        }

        /// <summary>Test-only: directly sets settings data.</summary>
        internal void SetSettings(OnlineAsrSettingsData settings)
        {
            _settings = settings;
        }

        public bool IsReady => _engine?.IsLoaded ?? false;
        public bool IsSessionActive => _engine?.IsSessionActive ?? false;
        public OnlineAsrProfile ActiveProfile => _activeProfile;
        public OnlineAsrSettingsData Settings => _settings;

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

        public event Action<OnlineAsrResult> PartialResultReady;
        public event Action<OnlineAsrResult> FinalResultReady;
        public event Action EndpointDetected;

        // ── Lifecycle ──

        public void Initialize()
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService initializing...");
            _settings = OnlineAsrSettingsLoader.Load();
            var profile = OnlineAsrSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] OnlineAsrService: no active profile.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = AsrModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
                if (!System.IO.Directory.Exists(dir))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] OnlineAsrService: LocalZip profile not yet extracted. Use InitializeAsync() instead.");
                    return;
                }
            }

            LoadProfile(profile);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService initialized.");
        }

        public async UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService async initializing...");

            _onEvent = onEvent;
            _settings = await OnlineAsrSettingsLoader.LoadAsync(ProfileReadyEvents.AsExtractProgress(onEvent), ct);

            var profile = OnlineAsrSettingsLoader.GetActiveProfile(_settings);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] OnlineAsrService: no active profile.");
                ProfileReadyEvents.EmitFailed(onEvent, "No active profile.");
                return;
            }

            const string serviceName = "OnlineAsrService";
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

            EnforceKeepOnlyActive();

            ProfileReadyEvents.EmitReady(onEvent);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService async initialized.");
        }

        private OnlineAsrProfile _profilePendingLoad;
        // Method-group target for UniTask.RunOnThreadPool — keeps the
        // InitializeAsync call site lambda-free.
        private void LoadPendingProfile()
        {
            var p = _profilePendingLoad;
            _profilePendingLoad = null;
            if (p != null)
                LoadProfile(p);
        }

        public void LoadProfile(OnlineAsrProfile profile)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] OnlineAsrService.LoadProfile: profile is null.");
                return;
            }

            UnsubscribeEngine();
            EnsureEngine();

            if (_engine == null)
                return;

            string modelDir = AsrModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
            _engine.Load(profile, modelDir);
            _activeProfile = profile;
            SubscribeEngine();
        }

        // ── Profile switching ──

        public void SwitchProfile(int index)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] OnlineAsrService.SwitchProfile: no profiles loaded.");
                return;
            }

            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OnlineAsrService.SwitchProfile: index {index} out of range (0..{_settings.profiles.Count - 1}).");
                return;
            }

            SwitchToProfile(_settings.profiles[index]);
        }
        public void SwitchProfile(string profileName)
        {
            if (_settings?.profiles == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] OnlineAsrService.SwitchProfile: no profiles loaded.");
                return;
            }

            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OnlineAsrService.SwitchProfile: profile '{profileName}' not found.");
                return;
            }

            SwitchToProfile(profile);
        }

        /// <summary>
        /// Async profile switch — native engine load runs on the
        /// thread pool so the UI thread is free during the sherpa-
        /// onnx OnlineRecognizer ctor.
        /// </summary>
        public async UniTask SwitchProfileAsync(int index, CancellationToken ct = default)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] OnlineAsrService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OnlineAsrService.SwitchProfileAsync: " +
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
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] OnlineAsrService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OnlineAsrService.SwitchProfileAsync: profile '{profileName}' not found.");
                return;
            }
            await SwitchToProfileAsync(profile, ct);
        }

        // Loads the new profile and, on success, runs the keep-only-
        // active sweep + re-fires Ready through the cached onEvent so
        // bus subscribers re-render with the new ActiveProfile.
        // Re-fires Failed if the load fails.
        private void SwitchToProfile(OnlineAsrProfile newProfile)
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
        // path InitializeAsync uses, so the sherpa-onnx OnlineRecognizer
        // ctor never runs on the main thread. _isSwitching guards every
        // hot-path streaming method (AcceptSamples / ProcessAvailableFrames
        // / etc.) for the duration so the mic feeder can keep firing
        // without crashing while the engine is reloaded.
        private async UniTask SwitchToProfileAsync(OnlineAsrProfile newProfile, CancellationToken ct)
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

        // Same logic as AsrService.EnforceKeepOnlyActive: iterates
        // only this service's own profile list, so the offline-ASR
        // active model — which lives in the same asr-models/ directory
        // — is never touched.
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
                $"[SherpaOnnx] ASR (online) keep-only-active: keeping " +
                $"'{keep}', sweeping {_settings.profiles.Count - 1} other " +
                "online profile(s)…");

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
                    "[SherpaOnnx] ASR (online) keep-only-active: nothing on disk to remove.");
            }
        }
        // ── Session & Audio ──

        public void StartSession()
        {
            if (!CheckReady())
                return;
            _engine.StartSession();
        }

        // Hot-path streaming methods are called once per mic frame from
        // the audio feeder. They bypass CheckReady for throughput, but
        // every one of them must skip while a profile switch is in
        // flight — the native recognizer is being recreated on a worker
        // thread and any concurrent native call segfaults.

        public void StopSession()
        {
            if (_isSwitching) return;
            _engine?.StopSession();
        }

        public void AcceptSamples(float[] samples, int sampleRate)
        {
            if (_isSwitching) return;
            _engine?.AcceptSamples(samples, sampleRate);
        }

        public void ProcessAvailableFrames()
        {
            if (_isSwitching) return;
            _engine?.ProcessAvailableFrames();
        }

        public void ResetStream()
        {
            if (_isSwitching) return;
            _engine?.ResetStream();
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

        public void Dispose()
        {
            UnsubscribeEngine();
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
            _engine ??= new OnlineAsrEngine();
#else
            if (_engine == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] SHERPA_ONNX scripting define is not set. " +
                    "Online ASR engine cannot be created.");
            }
#endif
        }

        private bool CheckReady()
        {
            if (_isSwitching)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] OnlineAsrService is busy switching profile — request ignored. " +
                    "Wait for the next ProfileReadyPhase.Ready event before retrying.");
                return false;
            }
            if (_engine != null && _engine.IsLoaded)
                return true;

            SherpaOnnxLog.RuntimeError(
                "[SherpaOnnx] OnlineAsrService is not initialized. Call Initialize() first.");
            return false;
        }

        private void SubscribeEngine()
        {
            if (_engine == null)
                return;
            _engine.PartialResultReady += OnPartialResultReady;
            _engine.FinalResultReady += OnFinalResultReady;
            _engine.EndpointDetected += OnEndpointDetected;
        }

        private void UnsubscribeEngine()
        {
            if (_engine == null)
                return;
            _engine.PartialResultReady -= OnPartialResultReady;
            _engine.FinalResultReady -= OnFinalResultReady;
            _engine.EndpointDetected -= OnEndpointDetected;
        }

        private void OnPartialResultReady(OnlineAsrResult result) => PartialResultReady?.Invoke(result);
        private void OnFinalResultReady(OnlineAsrResult result) => FinalResultReady?.Invoke(result);
        private void OnEndpointDetected() => EndpointDetected?.Invoke();
    }
}
