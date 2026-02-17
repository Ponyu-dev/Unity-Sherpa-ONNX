#if SHERPA_ONNX
using System;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
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

            var config = OnlineAsrConfigBuilder.Build(profile, modelDir);
            var guard = NativeLocaleGuard.Begin();

            try
            {
                _recognizer = new OnlineRecognizer(config);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OnlineAsrEngine.Load failed: {ex.Message}");
                _recognizer = null;
                return;
            }
            finally
            {
                guard.Dispose();
            }

            // Validate by creating a test stream.
            try
            {
                using var testStream = _recognizer.CreateStream();
                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] OnlineAsrEngine loaded: '{profile.profileName}'");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] OnlineAsrEngine validation failed: {ex.Message}");
                _recognizer.Dispose();
                _recognizer = null;
            }
        }

        public void Unload()
        {
            StopSession();
            if (_recognizer == null)
                return;

            _recognizer.Dispose();
            _recognizer = null;
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] OnlineAsrEngine unloaded.");
        }

        public void StartSession()
        {
            if (!IsLoaded)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] OnlineAsrEngine: cannot start session, not loaded.");
                return;
            }

            if (IsSessionActive)
                return;

            _stream = _recognizer.CreateStream();
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
            lock (_processLock)
            {
                if (!IsSessionActive)
                    return;

                while (_recognizer.IsReady(_stream))
                    _recognizer.Decode(_stream);

                var nativeResult = _recognizer.GetResult(_stream);
                if (string.IsNullOrEmpty(nativeResult.Text))
                    return;

                bool isEndpoint = _recognizer.IsEndpoint(_stream);
                var result = WrapResult(nativeResult, isEndpoint);

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
