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

        // Captures the previously active profile, loads the new one, and —
        // if the engine reports itself ready and AsrSettingsData.
        // autoDeletePreviousProfile is set — frees the disk space used by
        // the previous LocalZip extraction. Failed loads leave the old
        // extraction intact.
        private void SwitchToProfile(AsrProfile newProfile)
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
                // dir under persistentDataPath on Android — drop it.
                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] ASR (offline) auto-delete: switching " +
                    $"'{previous.profileName}' → '{newProfile.profileName}', " +
                    $"removing previous extraction…");
                LocalZipExtractor.TryDeleteExtractedModel(
                    AsrModelPathResolver.ModelsSubfolder, previous.profileName);
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
            if (_engine != null && _engine.IsLoaded)
                return true;
            
            SherpaOnnxLog.RuntimeError("[SherpaOnnx] AsrService is not initialized. Call Initialize() first.");
            return false;

        }
    }
}