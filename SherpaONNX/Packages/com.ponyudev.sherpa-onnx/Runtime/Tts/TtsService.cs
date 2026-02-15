using System;
using System.Linq;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
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

        /// <summary>True when the engine is loaded and ready to generate.</summary>
        public bool IsReady => _engine?.IsLoaded ?? false;

        /// <summary>Currently loaded TTS profile.</summary>
        public TtsProfile ActiveProfile => _activeProfile;

        /// <summary>All loaded profiles (available after Initialize).</summary>
        public TtsSettingsData Settings => _settings;

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
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsService: no active profile found.");
                return;
            }

            LoadProfile(profile);

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TtsService initialized.");
        }

        /// <summary>
        /// Loads (or reloads) the engine with the given profile.
        /// </summary>
        public void LoadProfile(TtsProfile profile)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsService.LoadProfile: profile is null.");
                return;
            }

            EnsureEngine();

            if (_engine == null)
                return;

            string modelDir = TtsModelPathResolver.GetModelDirectory(
                profile.profileName);

            _engine.Load(profile, modelDir);
            _activeProfile = profile;
        }

        /// <summary>
        /// Switches to a profile by index in the profiles list.
        /// </summary>
        public void SwitchProfile(int index)
        {
            if (_settings?.profiles == null || _settings.profiles.Count == 0)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsService.SwitchProfile: no profiles loaded.");
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
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsService.SwitchProfile: no profiles loaded.");
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
        /// Generates speech on a background thread.
        /// Returns null if the service is not ready.
        /// </summary>
        public Task<TtsResult> GenerateAsync(string text)
        {
            if (!CheckReady())
                return Task.FromResult<TtsResult>(null);

            float speed = _activeProfile.speed;
            int speakerId = _activeProfile.speakerId;
            return Task.Run(() => _engine.Generate(text, speed, speakerId));
        }

        /// <summary>
        /// Generates speech on a background thread with explicit parameters.
        /// Returns null if the service is not ready.
        /// </summary>
        public Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId)
        {
            if (!CheckReady())
                return Task.FromResult<TtsResult>(null);

            return Task.Run(() => _engine.Generate(text, speed, speakerId));
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
            if (_engine == null || !_engine.IsLoaded)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsService is not initialized. " +
                    "Call Initialize() first.");
                return false;
            }

            return true;
        }
    }
}
