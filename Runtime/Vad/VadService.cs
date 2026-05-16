using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Vad.Config;
using PonyuDev.SherpaOnnx.Vad.Data;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Vad
{
    /// <summary>
    /// High-level facade for VAD operations.
    /// Pure POCO — suitable for constructor injection via VContainer or
    /// manual instantiation from a MonoBehaviour.
    /// Never throws at runtime — logs errors instead.
    /// Tracks speech state transitions and raises events.
    /// </summary>
    public sealed class VadService : IVadService
    {
        private VadSettingsData _settings;
        private IVadEngine _engine;
        private VadProfile _activeProfile;
        // Last InitializeAsync's onEvent — re-fired by SwitchProfile so
        // bus subscribers see profile changes after the initial load.
        private Action<ProfileReadyEvent> _onEvent;
        // True while a profile switch is mid-flight. CheckReady bails
        // so AcceptWaveform / DrainSegments / Flush / Reset return as
        // no-ops instead of feeding samples into a native VAD detector
        // that is being torn down on a worker thread.
        private volatile bool _isSwitching;
        private VadProfile _profilePendingLoad;

        // Method-group target for UniTask.RunOnThreadPool inside
        // InitializeAsync — keeps the call site lambda-free.
        private void LoadPendingProfile()
        {
            var p = _profilePendingLoad;
            _profilePendingLoad = null;
            if (p != null)
                LoadProfile(p);
        }
        private bool _wasSpeech;

        public event Action<VadSegment> OnSegment;
        public event Action OnSpeechStart;
        public event Action OnSpeechEnd;

        public VadService() { }

        /// <summary>Test-only: injects a pre-built engine.</summary>
        internal VadService(IVadEngine engine)
        {
            _engine = engine;
        }

        /// <summary>Test-only: directly sets settings data.</summary>
        internal void SetSettings(VadSettingsData settings)
        {
            _settings = settings;
        }

        public bool IsReady => _engine?.IsLoaded ?? false;
        public VadProfile ActiveProfile => _activeProfile;
        public VadSettingsData Settings => _settings;
        public int WindowSize => _engine?.WindowSize ?? 0;

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

            string modelDir = VadModelPathResolver.GetModelDirectory(
                profile.profileName, profile.modelSource);
            return ProfileAvailability.IsAvailable(profile, modelDir);
        }

        // ── Lifecycle ──

        public void Initialize()
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] VadService initializing...");
            _settings = VadSettingsLoader.Load();
            var profile = VadSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] VadService: no active profile found.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = VadModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
                if (!System.IO.Directory.Exists(dir))
                {
                    SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService: LocalZip profile not yet extracted. Use InitializeAsync() instead.");
                    return;
                }
            }

            LoadProfile(profile);

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] VadService initialized.");
        }

        public async UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] VadService async initializing...");

            _onEvent = onEvent;
            _settings = await VadSettingsLoader.LoadAsync(ProfileReadyEvents.AsExtractProgress(onEvent), ct);

            var profile = VadSettingsLoader.GetActiveProfile(_settings);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] VadService: no active profile found.");
                ProfileReadyEvents.EmitFailed(onEvent, "No active profile.");
                return;
            }

            const string serviceName = "VadService";
            string subfolder = VadModelPathResolver.ModelsSubfolder;
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
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] VadService async initialized.");
        }

        public void LoadProfile(VadProfile profile)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService.LoadProfile: profile is null.");
                return;
            }

            EnsureEngine();

            if (_engine == null)
                return;

            string modelDir = VadModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);

            _engine.Load(profile, modelDir);
            _activeProfile = profile;
            _wasSpeech = false;
        }

        public void SwitchProfile(int index)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService.SwitchProfile: no profiles loaded.");
                return;
            }

            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] VadService.SwitchProfile: index {index} out of range (0..{_settings.profiles.Count - 1}).");
                return;
            }

            SwitchToProfile(_settings.profiles[index]);
        }

        public void SwitchProfile(string profileName)
        {
            if (_settings?.profiles == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService.SwitchProfile: no profiles loaded.");
                return;
            }

            var profile = _settings.profiles
                .FirstOrDefault(p => p.profileName == profileName);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] VadService.SwitchProfile: profile '{profileName}' not found.");
                return;
            }

            SwitchToProfile(profile);
        }

        /// <summary>
        /// Async profile switch — native engine load runs on the
        /// thread pool so the UI thread is free during the sherpa-
        /// onnx VoiceActivityDetector ctor.
        /// </summary>
        public async UniTask SwitchProfileAsync(int index, CancellationToken ct = default)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] VadService.SwitchProfileAsync: " +
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
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService.SwitchProfileAsync: no profiles loaded.");
                return;
            }
            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] VadService.SwitchProfileAsync: profile '{profileName}' not found.");
                return;
            }
            await SwitchToProfileAsync(profile, ct);
        }

        // Loads the new profile and, on success, runs the keep-only-
        // active sweep + re-fires Ready through the cached onEvent so
        // bus subscribers re-render with the new ActiveProfile.
        // Re-fires Failed if the load fails.
        private void SwitchToProfile(VadProfile newProfile)
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
        // path InitializeAsync uses, so the sherpa-onnx VoiceActivityDetector
        // ctor never runs on the main thread. _isSwitching guards
        // AcceptWaveform / DrainSegments / Flush / Reset for the
        // duration so the mic feeder can keep pushing samples without
        // crashing while the engine is reloaded.
        private async UniTask SwitchToProfileAsync(VadProfile newProfile, CancellationToken ct)
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

        // Same logic as the other services' EnforceKeepOnlyActive:
        // iterates only this service's profile list, so we never touch
        // extractions owned by anything else. VAD owns its own
        // vad-models/ directory, so the iteration vs full-sweep
        // distinction is not strictly required here, but the pattern
        // stays consistent across services.
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
                $"[SherpaOnnx] VAD keep-only-active: keeping '{keep}', " +
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
                    VadModelPathResolver.ModelsSubfolder, p.profileName))
                    removed++;
            }

            if (removed == 0)
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] VAD keep-only-active: nothing on disk to remove.");
            }
        }

        // ── Processing ──

        public void AcceptWaveform(float[] samples)
        {
            if (!CheckReady())
                return;

            _engine.AcceptWaveform(samples);
            ProcessStateTransitions();
        }

        public bool IsSpeechDetected()
        {
            return _engine != null && _engine.IsSpeechDetected();
        }

        public List<VadSegment> DrainSegments()
        {
            if (!CheckReady())
                return new List<VadSegment>();

            var segments = _engine.DrainSegments();

            foreach (var segment in segments)
                OnSegment?.Invoke(segment);

            return segments;
        }

        public void Flush()
        {
            // Skip during switch — the native detector is being
            // recreated on a worker thread and DrainSegments would
            // collide with that.
            if (_isSwitching || _engine == null)
                return;

            _engine.Flush();

            var segments = _engine.DrainSegments();

            foreach (var segment in segments)
                OnSegment?.Invoke(segment);
        }

        public void Reset()
        {
            if (_isSwitching) return;
            _engine?.Reset();
            _wasSpeech = false;
        }

        // ── Disk usage ──

        /// <inheritdoc />
        public IReadOnlyList<string> GetExtractedProfiles()
            => LocalZipExtractor.ListExtractedProfiles(VadModelPathResolver.ModelsSubfolder);

        /// <inheritdoc />
        public long GetExtractedProfileSizeBytes(string profileName)
            => LocalZipExtractor.GetExtractedSizeBytes(VadModelPathResolver.ModelsSubfolder, profileName);

        /// <inheritdoc />
        public bool TryDeleteExtractedProfile(string profileName)
            => LocalZipExtractor.TryDeleteExtractedModel(VadModelPathResolver.ModelsSubfolder, profileName);

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
                VadModelPathResolver.ModelsSubfolder, keep);
        }

        // ── Cleanup ──

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
            _activeProfile = null;
            _settings = null;
            _onEvent = null;

            OnSegment = null;
            OnSpeechStart = null;
            OnSpeechEnd = null;
        }

        // ── Private ──

        private void EnsureEngine()
        {
#if SHERPA_ONNX
            _engine ??= new VadEngine();
#else
            if (_engine == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] SHERPA_ONNX scripting define " +
                    "is not set. VAD engine cannot be created.");
            }
#endif
        }

        private bool CheckReady()
        {
            if (_isSwitching)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] VadService is busy switching profile — request ignored. " +
                    "Wait for the next ProfileReadyPhase.Ready event before retrying.");
                return false;
            }
            if (_engine != null && _engine.IsLoaded)
                return true;

            SherpaOnnxLog.RuntimeError(
                "[SherpaOnnx] VadService is not initialized. Call Initialize() first.");
            return false;
        }

        private void ProcessStateTransitions()
        {
            bool isSpeech = IsSpeechDetected();

            if (isSpeech && !_wasSpeech)
                OnSpeechStart?.Invoke();
            else if (!isSpeech && _wasSpeech)
                OnSpeechEnd?.Invoke();

            _wasSpeech = isSpeech;

            // Auto-drain segments and raise events.
            if (isSpeech)
                DrainSegments();
        }
    }
}