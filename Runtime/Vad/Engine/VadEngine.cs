#if SHERPA_ONNX
using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Vad.Config;
using PonyuDev.SherpaOnnx.Vad.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Vad.Engine
{
    /// <summary>
    /// Wraps native <see cref="VoiceActivityDetector"/>.
    /// Not thread-safe — designed for single-thread use
    /// from the main Unity update loop or a dedicated audio thread.
    /// Never throws — logs errors instead.
    /// </summary>
    public sealed class VadEngine : IVadEngine
    {
        private VoiceActivityDetector _detector;
        private int _windowSize;
        private int _sampleRate;

        public bool IsLoaded => _detector != null;
        public int WindowSize => _windowSize;

        // ── Lifecycle ──

        public void Load(VadProfile profile, string modelDir)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadEngine.Load: profile is null.");
                return;
            }

            Unload();

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] VAD engine loading: {profile.profileName} ({profile.modelType})");
            var config = VadConfigBuilder.Build(profile, modelDir);

            try
            {
                VoiceActivityDetector detector;
                using (NativeLocaleGuard.Begin())
                {
                    detector = new VoiceActivityDetector(
                        config, profile.bufferSizeInSeconds);
                }

                _detector = detector;
                _windowSize = profile.windowSize;
                _sampleRate = profile.sampleRate;

                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] VAD engine loaded: {profile.profileName} (window={_windowSize}, rate={_sampleRate})");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] VadEngine creation failed: {ex.Message}");
            }
        }

        public void Unload()
        {
            if (_detector == null)
                return;

            _detector.Dispose();
            _detector = null;
            _windowSize = 0;
            _sampleRate = 0;

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] VAD engine unloaded.");
        }

        public void Dispose()
        {
            Unload();
        }

        // ── Processing ──

        public void AcceptWaveform(float[] samples)
        {
            if (_detector == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadEngine: engine not loaded.");
                return;
            }

            _detector.AcceptWaveform(samples);
        }

        public bool IsSpeechDetected()
        {
            return _detector != null && _detector.IsSpeechDetected();
        }

        public List<VadSegment> DrainSegments()
        {
            var segments = new List<VadSegment>();

            if (_detector == null)
                return segments;

            while (!_detector.IsEmpty())
            {
                SpeechSegment native = _detector.Front();
                segments.Add(new VadSegment(native.Start, native.Samples, _sampleRate));
                _detector.Pop();
            }

            return segments;
        }

        public void Flush()
        {
            _detector?.Flush();
        }

        public void Reset()
        {
            _detector?.Reset();
        }
    }
}
#endif
