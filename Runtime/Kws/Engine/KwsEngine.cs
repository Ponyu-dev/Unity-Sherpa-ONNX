#if SHERPA_ONNX
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Common.Validation;
using PonyuDev.SherpaOnnx.Kws.Config;
using PonyuDev.SherpaOnnx.Kws.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Kws.Engine
{
    /// <summary>
    /// Wraps native <see cref="KeywordSpotter"/> and
    /// <see cref="OnlineStream"/>. Streaming 1:1 — one
    /// stream per engine instance, always-on listening.
    /// Auto-resets the stream after keyword detection.
    /// </summary>
    public sealed class KwsEngine : IKwsEngine
    {
        private readonly object _processLock = new();

        private KeywordSpotter _spotter;
        private OnlineStream _stream;
        private bool _disposed;

        public bool IsLoaded => _spotter != null;
        public bool IsSessionActive => _stream != null;

        public event Action<KwsResult> KeywordDetected;

        public void Load(KwsProfile profile, string modelDir)
        {
            Unload();

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] KwsEngine loading: " +
                $"'{profile.profileName}', " +
                $"modelDir='{modelDir}'");

            if (!Directory.Exists(modelDir))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] KWS model directory " +
                    $"not found: '{modelDir}'. " +
                    "Import models via Project Settings → " +
                    "Sherpa-ONNX → KWS.");
                return;
            }

            if (ModelFileValidator.BlockIfInt8Model(modelDir, "KWS", profile.allowInt8))
                return;

            // Validate keyword tokens against vocabulary
            // before native constructor to prevent SEGFAULT.
            string tokensPath = KwsModelPathResolver.Resolve(modelDir, profile.tokens);
            string kwFilePath = KwsModelPathResolver.Resolve(modelDir, profile.keywordsFile);
            if (KeywordTokenValidator.BlockIfInvalidTokens(tokensPath, kwFilePath, profile.customKeywords, "KWS"))
                return;

            var config = KwsConfigBuilder.Build(profile, modelDir);
            var guard = NativeLocaleGuard.Begin();

            try
            {
                _spotter = new KeywordSpotter(config);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsEngine.Load failed: " + ex.Message);
                _spotter = null;
                return;
            }
            finally
            {
                guard.Dispose();
            }

            if (!IsNativeHandleValid(_spotter))
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] KeywordSpotter created with " +
                    "null native handle. Model files may be " +
                    "missing or config is invalid.");
                _spotter = null;
                return;
            }

            // Validate by creating a test stream.
            try
            {
                using var testStream = _spotter.CreateStream();
                EngineRegistry.Register(this);
                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] KwsEngine loaded: '{profile.profileName}'");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] KwsEngine validation failed: {ex.Message}");
                _spotter.Dispose();
                _spotter = null;
            }
        }

        public void Unload()
        {
            StopSession();
            if (_spotter == null)
                return;

            EngineRegistry.Unregister(this);

            _spotter.Dispose();
            _spotter = null;
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsEngine unloaded.");
        }

        public void StartSession()
        {
            if (!IsLoaded)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] KwsEngine: cannot start session, not loaded.");
                return;
            }

            lock (_processLock)
            {
                if (IsSessionActive)
                    return;

                _stream = _spotter.CreateStream();
            }

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsEngine: session started.");
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

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] KwsEngine: session stopped.");
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
            KwsResult result = null;

            lock (_processLock)
            {
                if (!IsSessionActive)
                    return;

                while (_spotter.IsReady(_stream))
                    _spotter.Decode(_stream);

                var nativeResult = _spotter.GetResult(_stream);
                string keyword = nativeResult?.Keyword;

                if (string.IsNullOrEmpty(keyword))
                    return;

                result = new KwsResult(keyword);

                // Auto-reset stream after keyword detection
                // so spotting continues immediately.
                _spotter.Reset(_stream);
            }

            // Fire event outside lock to prevent deadlock.
            KeywordDetected?.Invoke(result);
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
        /// Checks whether the native handle inside a KeywordSpotter
        /// is valid (non-zero). Uses reflection because the field is
        /// private in the sherpa-onnx managed DLL.
        /// </summary>
        private static bool IsNativeHandleValid(KeywordSpotter spotter)
        {
            try
            {
                var field = typeof(KeywordSpotter).GetField("_handle", BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] Cannot find _handle field " +
                        "in KeywordSpotter — " +
                        "skipping null check.");
                    return true;
                }

                var handleRef = (HandleRef)field.GetValue(spotter);
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
            while (_spotter.IsReady(_stream))
                _spotter.Decode(_stream);
        }
    }
}
#endif
