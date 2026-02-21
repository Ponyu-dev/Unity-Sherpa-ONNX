#if SHERPA_ONNX
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] VAD engine loading: " +
                $"{profile.profileName} ({profile.modelType}), " +
                $"modelDir='{modelDir}'");

            if (!ValidateModelDirectory(modelDir, profile))
                return;

            var config = VadConfigBuilder.Build(profile, modelDir);

            try
            {
                VoiceActivityDetector detector;
                using (NativeLocaleGuard.Begin())
                {
                    detector = new VoiceActivityDetector(
                        config, profile.bufferSizeInSeconds);
                }

                // Guard: native constructor returns NULL handle when
                // model file is missing or config is invalid.
                // Subsequent calls on a NULL handle cause segfaults.
                if (!IsNativeHandleValid(detector))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] VoiceActivityDetector created " +
                        "with null native handle. Model file may " +
                        "be missing or config is invalid. " +
                        $"Model='{config.SileroVad.Model}'");
                    return;
                }

                _detector = detector;
                _windowSize = profile.windowSize;
                _sampleRate = profile.sampleRate;

                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] VAD engine loaded: " +
                    $"{profile.profileName} " +
                    $"(window={_windowSize}, rate={_sampleRate})");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] VadEngine creation failed: " +
                    $"{ex.Message}");
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

        // ── Model validation ──

        /// <summary>
        /// Checks that the model directory and model file exist.
        /// Prevents native segfaults caused by missing models.
        /// </summary>
        private static bool ValidateModelDirectory(
            string modelDir, VadProfile profile)
        {
            if (!Directory.Exists(modelDir))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] VAD model directory not found: " +
                    $"'{modelDir}'. " +
                    "Import models via Window → Sherpa ONNX → " +
                    "VAD Model Import.");
                return false;
            }

            if (!string.IsNullOrEmpty(profile.model))
            {
                string modelPath = Path.Combine(
                    modelDir, profile.model);
                if (!File.Exists(modelPath))
                {
                    SherpaOnnxLog.RuntimeError(
                        $"[SherpaOnnx] VAD model file not found: " +
                        $"'{modelPath}'. " +
                        "Re-import the model or check the profile.");
                    return false;
                }
            }

            return true;
        }

        // ── Handle validation ──

        /// <summary>
        /// Checks whether the native handle inside a
        /// VoiceActivityDetector is valid (non-zero).
        /// Uses reflection because the field is private
        /// in the sherpa-onnx managed DLL.
        /// </summary>
        private static bool IsNativeHandleValid(
            VoiceActivityDetector detector)
        {
            try
            {
                var field = typeof(VoiceActivityDetector).GetField(
                    "_handle",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] Cannot find _handle field " +
                        "in VoiceActivityDetector — " +
                        "skipping null check.");
                    return true;
                }

                var handleRef =
                    (HandleRef)field.GetValue(detector);
                return handleRef.Handle != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] Handle validation failed: " +
                    $"{ex.Message} — skipping null check.");
                return true;
            }
        }
    }
}
#endif
