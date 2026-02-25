#if SHERPA_ONNX
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Common.Validation;
using PonyuDev.SherpaOnnx.Asr.Online.Config;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Asr.Online.Engine
{
    /// <summary>
    /// Wraps native <see cref="OnlineRecognizer"/> and
    /// <see cref="OnlineStream"/>. No pool — streaming is 1:1.
    /// </summary>
    public sealed class OnlineAsrEngine : IOnlineAsrEngine
    {
        private readonly object _processLock = new();

        private OnlineRecognizer _recognizer;
        private OnlineStream _stream;
        private bool _disposed;

        public bool IsLoaded => _recognizer != null;
        public bool IsSessionActive => _stream != null;

        public event Action<OnlineAsrResult> PartialResultReady;
        public event Action<OnlineAsrResult> FinalResultReady;
        public event Action EndpointDetected;

        public void Load(OnlineAsrProfile profile, string modelDir)
        {
            Unload();

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] OnlineAsrEngine loading: " +
                $"'{profile.profileName}', " +
                $"modelDir='{modelDir}'");

            if (!Directory.Exists(modelDir))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Online ASR model directory " +
                    $"not found: '{modelDir}'. " +
                    "Import models via Window → Sherpa ONNX → " +
                    "ASR Model Import.");
                return;
            }

            if (ModelFileValidator.BlockIfInt8Model(modelDir, "Online ASR", profile.allowInt8))
                return;

            var config = OnlineAsrConfigBuilder.Build(profile, modelDir);
            var guard = NativeLocaleGuard.Begin();

            try
            {
                _recognizer = new OnlineRecognizer(config);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] OnlineAsrEngine.Load failed: " +
                    ex.Message);
                _recognizer = null;
                return;
            }
            finally
            {
                guard.Dispose();
            }

            // Guard: native constructor returns NULL handle when
            // model files are missing or config is invalid.
            // Calling CreateStream on a NULL handle causes a
            // segfault that cannot be caught by try/catch.
            if (!IsNativeHandleValid(_recognizer))
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] OnlineRecognizer created with " +
                    "null native handle. Model files may be " +
                    "missing or config is invalid.");
                _recognizer = null;
                return;
            }

            // Validate by creating a test stream.
            try
            {
                using var testStream = _recognizer.CreateStream();
                EngineRegistry.Register(this);
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] OnlineAsrEngine loaded: " +
                    $"'{profile.profileName}'");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] OnlineAsrEngine validation " +
                    $"failed: {ex.Message}");
                _recognizer.Dispose();
                _recognizer = null;
            }
        }

        public void Unload()
        {
            StopSession();
            if (_recognizer == null)
                return;

            EngineRegistry.Unregister(this);

            _recognizer.Dispose();
            _recognizer = null;
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrEngine unloaded.");
        }

        public void StartSession()
        {
            if (!IsLoaded)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] OnlineAsrEngine: " +
                    "cannot start session, not loaded.");
                return;
            }

            lock (_processLock)
            {
                if (IsSessionActive)
                    return;

                _stream = _recognizer.CreateStream();
            }

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrEngine: session started.");
        }

        public void StopSession()
        {
            if (!IsSessionActive)
                return;

            lock (_processLock)
            {
                _stream.InputFinished();
                DrainAndDecode();
                _stream.Dispose();
                _stream = null;
            }

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrEngine: session stopped.");
        }

        public void AcceptSamples(float[] samples, int sampleRate)
        {
            if (samples == null || samples.Length == 0)
                return;

            lock (_processLock)
            {
                if (!IsSessionActive)
                    return;

                _stream.AcceptWaveform(sampleRate, samples);
            }
        }

        public void ProcessAvailableFrames()
        {
            OnlineAsrResult result = null;
            bool isEndpoint = false;

            lock (_processLock)
            {
                if (!IsSessionActive)
                    return;

                while (_recognizer.IsReady(_stream))
                    _recognizer.Decode(_stream);

                var nativeResult = _recognizer.GetResult(_stream);
                if (string.IsNullOrEmpty(nativeResult.Text))
                    return;

                isEndpoint = _recognizer.IsEndpoint(_stream);
                result = WrapResult(nativeResult, isEndpoint);
            }

            // Fire events outside lock to prevent deadlock
            // if subscribers call back into the engine.
            if (isEndpoint)
            {
                FinalResultReady?.Invoke(result);
                EndpointDetected?.Invoke();
            }
            else
            {
                PartialResultReady?.Invoke(result);
            }
        }

        public void ResetStream()
        {
            lock (_processLock)
            {
                if (!IsSessionActive)
                    return;

                _recognizer.Reset(_stream);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Unload();
        }

        // ── Handle validation ──

        /// <summary>
        /// Checks whether the native handle inside an OnlineRecognizer
        /// is valid (non-zero). Uses reflection because the field is
        /// private in the sherpa-onnx managed DLL.
        /// </summary>
        private static bool IsNativeHandleValid(
            OnlineRecognizer recognizer)
        {
            try
            {
                var field = typeof(OnlineRecognizer).GetField(
                    "_handle",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] Cannot find _handle field " +
                        "in OnlineRecognizer — " +
                        "skipping null check.");
                    return true;
                }

                var handleRef =
                    (HandleRef)field.GetValue(recognizer);
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

        // ── Private ──

        private void DrainAndDecode()
        {
            while (_recognizer.IsReady(_stream))
                _recognizer.Decode(_stream);
        }

        private static OnlineAsrResult WrapResult(
            OnlineRecognizerResult native, bool isFinal)
        {
            string text = native.Text?.Trim();

            string[] tokens = native.Tokens;
            if (tokens != null && tokens.Length == 0)
                tokens = null;

            float[] timestamps = native.Timestamps;
            if (timestamps != null && timestamps.Length == 0)
                timestamps = null;

            return new OnlineAsrResult(text, tokens, timestamps, isFinal);
        }
    }
}
#endif
