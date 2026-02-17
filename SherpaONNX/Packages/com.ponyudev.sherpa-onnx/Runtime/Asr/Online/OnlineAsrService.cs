using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
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

        public bool IsReady => _engine?.IsLoaded ?? false;
        public bool IsSessionActive => _engine?.IsSessionActive ?? false;
        public OnlineAsrProfile ActiveProfile => _activeProfile;
        public OnlineAsrSettingsData Settings => _settings;

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

            LoadProfile(profile);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService initialized.");
        }

        public async UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService async initializing...");
            _settings = await OnlineAsrSettingsLoader.LoadAsync(progress, ct);
            var profile = OnlineAsrSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] OnlineAsrService: no active profile.");
                return;
            }

            LoadProfile(profile);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrService async initialized.");
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

            string modelDir = AsrModelPathResolver.GetModelDirectory(profile.profileName);
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

            LoadProfile(_settings.profiles[index]);
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

        public void ProcessAvailableFrames() => _engine?.ProcessAvailableFrames();
        public void ResetStream() => _engine?.ResetStream();

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
