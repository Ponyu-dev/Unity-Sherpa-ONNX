#if SHERPA_ONNX
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Common.Validation;
using PonyuDev.SherpaOnnx.Asr.Offline.Config;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Asr.Offline.Engine
{
    /// <summary>
    /// Pool of native <see cref="OfflineRecognizer"/> instances.
    /// Allows N concurrent recognitions via SemaphoreSlim + ConcurrentQueue.
    /// Thread-safe. Never throws — logs errors instead.
    /// </summary>
    public sealed class AsrEngine : IAsrEngine
    {
        private readonly ConcurrentQueue<OfflineRecognizer> _available = new();
        private readonly object _resizeLock = new();

        private OfflineRecognizer[] _pool;
        private SemaphoreSlim _semaphore;
        private OfflineRecognizerConfig _lastConfig;
        private int _poolSize;

        public bool IsLoaded => _pool != null && _pool.Length > 0;
        public int PoolSize => _poolSize;

        // ── Lifecycle ──

        public void Load(AsrProfile profile, string modelDir, int poolSize = 1)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AsrEngine.Load: profile is null.");
                return;
            }

            Unload();

            poolSize = Math.Max(1, poolSize);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ASR engine loading: {profile.profileName}" +
                $" (pool={poolSize}), modelDir='{modelDir}'");

            if (!ValidateModelDirectory(modelDir, profile))
                return;

            if (ModelFileValidator.BlockIfInt8Model(
                    modelDir, "ASR", profile.allowInt8))
                return;

            _lastConfig = AsrConfigBuilder.Build(profile, modelDir);

            // Create first instance and validate it.
            var first = CreateInstance(_lastConfig);
            if (first == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrEngine: failed to create " +
                    $"OfflineRecognizer for '{profile.profileName}'. " +
                    "Check model paths and config.");
                return;
            }

            _pool = new OfflineRecognizer[poolSize];
            _pool[0] = first;
            _available.Enqueue(first);

            for (int i = 1; i < poolSize; i++)
            {
                var recognizer = CreateInstance(_lastConfig);
                if (recognizer != null)
                {
                    _pool[i] = recognizer;
                    _available.Enqueue(recognizer);
                }
                else
                {
                    SherpaOnnxLog.RuntimeWarning(
                        $"[SherpaOnnx] AsrEngine: pool instance {i} " +
                        "creation failed, skipping.");
                }
            }

            _poolSize = poolSize;
            _semaphore = new SemaphoreSlim(poolSize, poolSize);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ASR engine loaded: {profile.profileName} " +
                $"(pool={poolSize})");
        }

        public void Resize(int newPoolSize)
        {
            newPoolSize = Math.Max(1, newPoolSize);
            if (newPoolSize == _poolSize || !IsLoaded)
                return;

            lock (_resizeLock)
            {
                if (newPoolSize > _poolSize)
                    GrowPool(newPoolSize);
                else
                    ShrinkPool(newPoolSize);

                // Recreate semaphore with new capacity.
                var oldSem = _semaphore;
                _semaphore = new SemaphoreSlim(newPoolSize, newPoolSize);
                oldSem?.Dispose();
                _poolSize = newPoolSize;

                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] AsrEngine resized to {newPoolSize}.");
            }
        }

        public void Unload()
        {
            if (_pool == null)
                return;

            // Drain and dispose all instances.
            while (_available.TryDequeue(out _)) { }

            foreach (var recognizer in _pool)
                recognizer?.Dispose();

            _pool = null;
            _poolSize = 0;

            _semaphore?.Dispose();
            _semaphore = null;

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] ASR engine unloaded.");
        }

        public void Dispose()
        {
            Unload();
        }

        // ── Recognition ──

        public AsrResult Recognize(float[] samples, int sampleRate)
        {
            if (!ValidateBeforeRecognize(samples))
                return null;

            return RentAndRecognize(recognizer =>
            {
                OfflineStream stream = null;
                try
                {
                    stream = recognizer.CreateStream();
                    stream.AcceptWaveform(sampleRate, samples);
                    recognizer.Decode(stream);

                    var nativeResult = stream.Result;
                    return WrapResult(nativeResult);
                }
                finally
                {
                    stream?.Dispose();
                }
            });
        }

        // ── Pool core ──

        private AsrResult RentAndRecognize(
            Func<OfflineRecognizer, AsrResult> action)
        {
            _semaphore.Wait();
            OfflineRecognizer recognizer = null;
            try
            {
                if (!_available.TryDequeue(out recognizer))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] AsrEngine: no engine available.");
                    return null;
                }
                return action(recognizer);
            }
            finally
            {
                if (recognizer != null)
                    _available.Enqueue(recognizer);
                _semaphore.Release();
            }
        }

        // ── Resize helpers ──

        private void GrowPool(int newSize)
        {
            int oldSize = _pool.Length;
            var newPool = new OfflineRecognizer[newSize];
            Array.Copy(_pool, newPool, oldSize);

            for (int i = oldSize; i < newSize; i++)
            {
                var recognizer = CreateInstance(_lastConfig);
                if (recognizer != null)
                {
                    newPool[i] = recognizer;
                    _available.Enqueue(recognizer);
                }
            }

            _pool = newPool;
        }

        private void ShrinkPool(int newSize)
        {
            int toRemove = _poolSize - newSize;
            int removed = 0;

            // Dispose idle instances from the queue.
            while (removed < toRemove
                   && _available.TryDequeue(out var recognizer))
            {
                recognizer.Dispose();
                removed++;
            }

            // Rebuild pool array from remaining queue contents.
            var remaining = _available.ToArray();
            _pool = new OfflineRecognizer[remaining.Length];
            Array.Copy(remaining, _pool, remaining.Length);
        }

        // ── Model validation ──

        /// <summary>
        /// Checks that the model directory and essential files exist.
        /// Prevents native segfaults caused by missing models.
        /// </summary>
        private static bool ValidateModelDirectory(
            string modelDir, AsrProfile profile)
        {
            if (!Directory.Exists(modelDir))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] ASR model directory not found: " +
                    $"'{modelDir}'. " +
                    "Import models via Window → Sherpa ONNX → " +
                    "ASR Model Import.");
                return false;
            }

            // Tokens file is required for all model types.
            if (!string.IsNullOrEmpty(profile.tokens))
            {
                string tokensPath = Path.Combine(
                    modelDir, profile.tokens);
                if (!File.Exists(tokensPath))
                {
                    SherpaOnnxLog.RuntimeError(
                        $"[SherpaOnnx] ASR tokens file not found: " +
                        $"'{tokensPath}'. " +
                        "Re-import the model or check the profile.");
                    return false;
                }
            }

            return true;
        }

        // ── Instance creation ──

        /// <summary>
        /// Creates a native OfflineRecognizer and validates it
        /// by checking the native handle. Returns null on failure.
        /// Uses <see cref="NativeLocaleGuard"/> to force C locale —
        /// required on Android where system locale may use comma
        /// as decimal separator, causing sherpa-onnx to fail.
        /// </summary>
        private static OfflineRecognizer CreateInstance(
            OfflineRecognizerConfig config)
        {
            try
            {
                OfflineRecognizer recognizer;
                using (NativeLocaleGuard.Begin())
                {
                    recognizer = new OfflineRecognizer(config);
                }

                // Guard: native constructor returns NULL handle when
                // model files are missing or config is invalid.
                // Calling CreateStream on a NULL handle causes a
                // segfault that cannot be caught by try/catch.
                if (!IsNativeHandleValid(recognizer))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] OfflineRecognizer created with " +
                        "null native handle. Model files may be " +
                        "missing or config is invalid. " +
                        $"Tokens='{config.ModelConfig.Tokens}', " +
                        $"Provider='{config.ModelConfig.Provider}'");
                    return null;
                }

                // Validate: create and immediately dispose a stream.
                using (recognizer.CreateStream()) { }

                return recognizer;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OfflineRecognizer creation failed: " +
                    $"{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks whether the native handle inside an OfflineRecognizer
        /// is valid (non-zero). Uses reflection because the field is
        /// private in the sherpa-onnx managed DLL.
        /// </summary>
        private static bool IsNativeHandleValid(OfflineRecognizer recognizer)
        {
            try
            {
                var field = typeof(OfflineRecognizer).GetField(
                    "_handle",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] Cannot find _handle field " +
                        "in OfflineRecognizer — skipping null check.");
                    return true;
                }

                var handleRef = (HandleRef)field.GetValue(recognizer);
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

        // ── Private helpers ──

        private bool ValidateBeforeRecognize(float[] samples)
        {
            if (!IsLoaded)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AsrEngine: engine not loaded.");
                return false;
            }

            if (samples == null || samples.Length == 0)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] AsrEngine: samples are empty.");
                return false;
            }

            return true;
        }

        private static AsrResult WrapResult(OfflineRecognizerResult result)
        {
            if (result == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AsrEngine: native returned null result.");
                return null;
            }

            string text = result.Text;
            string[] tokens = result.Tokens;
            float[] timestamps = result.Timestamps;
            float[] durations = result.Durations;

            // Normalize empty arrays to null.
            if (tokens != null && tokens.Length == 0) tokens = null;
            if (timestamps != null && timestamps.Length == 0) timestamps = null;
            if (durations != null && durations.Length == 0) durations = null;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ASR recognized: " +
                $"\"{Truncate(text, 80)}\"");

            return new AsrResult(text, tokens, timestamps, durations);
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? "";

            return text.Substring(0, maxLength) + "...";
        }
    }
}
#endif
