using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Kws.Config;
using PonyuDev.SherpaOnnx.Kws.Data;
using PonyuDev.SherpaOnnx.Kws.Engine;

namespace PonyuDev.SherpaOnnx.Kws
{
    /// <summary>
    /// High-level POCO facade for keyword spotting.
    /// Never throws at runtime — logs errors instead.
    /// </summary>
    public sealed class KwsService : IKwsService
    {
        private KwsSettingsData _settings;
        private IKwsEngine _engine;
        private KwsProfile _activeProfile;

        public KwsService() { }

        /// <summary>Test-only: injects a pre-built engine.</summary>
        internal KwsService(IKwsEngine engine)
        {
            _engine = engine;
        }

        /// <summary>Test-only: directly sets settings data.</summary>
        internal void SetSettings(KwsSettingsData settings)
        {
            _settings = settings;
        }

        public bool IsReady => _engine?.IsLoaded ?? false;
        public bool IsSessionActive => _engine?.IsSessionActive ?? false;
        public KwsProfile ActiveProfile => _activeProfile;
        public KwsSettingsData Settings => _settings;

        public event Action<KwsResult> OnKeywordDetected;

        // ── Lifecycle ──

        public void Initialize()
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsService initializing...");
            _settings = KwsSettingsLoader.Load();
            var profile = KwsSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] KwsService: no active profile.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = KwsModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
                if (!System.IO.Directory.Exists(dir))
                {
                    SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsService: LocalZip profile not yet extracted. Use InitializeAsync().");
                    return;
                }
            }

            LoadProfile(profile);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsService initialized.");
        }

        public async UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsService async initializing...");

            _settings = await KwsSettingsLoader.LoadAsync(progress, ct);
            var profile = KwsSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] KwsService: no active profile.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = await LocalZipExtractor
                    .EnsureExtractedAsync(KwsModelPathResolver.ModelsSubfolder, profile.profileName, progress, ct);
                if (dir == null)
                {
                    SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsService: LocalZip extraction failed.");
                    return;
                }
            }

            LoadProfile(profile);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsService async initialized.");
        }

        public void LoadProfile(KwsProfile profile)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsService.LoadProfile: profile is null.");
                return;
            }

            UnsubscribeEngine();
            EnsureEngine();

            if (_engine == null)
                return;

            string modelDir = KwsModelPathResolver.GetModelDirectory(profile.profileName, profile.modelSource);
            _engine.Load(profile, modelDir);
            _activeProfile = profile;
            SubscribeEngine();
        }

        // ── Profile switching ──

        public void SwitchProfile(int index)
        {
            if (_settings?.profiles == null ||
                _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsService.SwitchProfile: no profiles loaded.");
                return;
            }

            if (index < 0 || index >= _settings.profiles.Count)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] KwsService.SwitchProfile: index {index} out of range (0..{_settings.profiles.Count - 1}).");
                return;
            }

            LoadProfile(_settings.profiles[index]);
        }

        public void SwitchProfile(string profileName)
        {
            if (_settings?.profiles == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsService.SwitchProfile: no profiles loaded.");
                return;
            }

            var profile = _settings.profiles.FirstOrDefault(p => p.profileName == profileName);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] KwsService.SwitchProfile: profile '{profileName}' not found.");
                return;
            }

            LoadProfile(profile);
        }

        // ── Session & Audio ──

        public void StartSession()
        {
            if (!CheckReady())
                return;
            _engine.StartSession();
        }

        public void StopSession() => _engine?.StopSession();

        public void AcceptSamples(float[] samples, int sampleRate) =>
            _engine?.AcceptSamples(samples, sampleRate);

        public void ProcessAvailableFrames() =>
            _engine?.ProcessAvailableFrames();

        public void Dispose()
        {
            UnsubscribeEngine();
            _engine?.Dispose();
            _engine = null;
            _activeProfile = null;
            _settings = null;
        }

        // ── Private ──

        private void EnsureEngine()
        {
#if SHERPA_ONNX
            _engine ??= new KwsEngine();
#else
            if (_engine == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] SHERPA_ONNX scripting define is not set. KWS engine cannot be created.");
            }
#endif
        }

        private bool CheckReady()
        {
            if (_engine != null && _engine.IsLoaded)
                return true;

            SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsService is not initialized. Call Initialize() first.");
            return false;
        }

        private void SubscribeEngine()
        {
            if (_engine == null)
                return;
            _engine.KeywordDetected += OnEngineKeywordDetected;
        }

        private void UnsubscribeEngine()
        {
            if (_engine == null)
                return;
            _engine.KeywordDetected -= OnEngineKeywordDetected;
        }

        private void OnEngineKeywordDetected(KwsResult result) =>
            OnKeywordDetected?.Invoke(result);
    }
}
