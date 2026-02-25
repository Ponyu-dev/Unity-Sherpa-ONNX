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
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] VadService async initializing...");

            _settings = await VadSettingsLoader.LoadAsync(progress, ct);
            var profile = VadSettingsLoader.GetActiveProfile(_settings);

            if (profile == null)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] VadService: no active profile found.");
                return;
            }

            if (profile.modelSource == ModelSource.LocalZip)
            {
                string dir = await LocalZipExtractor.EnsureExtractedAsync(VadModelPathResolver.ModelsSubfolder, profile.profileName, progress, ct);
                if (dir == null)
                {
                    SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadService: LocalZip extraction failed.");
                    return;
                }
            }

            LoadProfile(profile);

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

            LoadProfile(_settings.profiles[index]);
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

            LoadProfile(profile);
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
            if (_engine == null)
                return;

            _engine.Flush();

            var segments = _engine.DrainSegments();

            foreach (var segment in segments)
                OnSegment?.Invoke(segment);
        }

        public void Reset()
        {
            _engine?.Reset();
            _wasSpeech = false;
        }

        // ── Cleanup ──

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
            _activeProfile = null;
            _settings = null;

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