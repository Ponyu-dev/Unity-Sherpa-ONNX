using System;
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
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService async initializing...");

            _settings = await TtsSettingsLoader.LoadAsync(progress, ct);
            var profile = TtsSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] TtsService: no active profile found.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = await LocalZipExtractor.EnsureExtractedAsync(
                    TtsModelPathResolver.ModelsSubfolder, profile.profileName, progress, ct);
                if (dir == null)
                {
                    SherpaOnnxLog.RuntimeError("[SherpaOnnx] TtsService: LocalZip extraction failed.");
                    return;
                }
            }

            LoadProfile(profile);

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService async initialized.");
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

            LoadProfile(_settings.profiles[index]);
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

            LoadProfile(profile);
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
