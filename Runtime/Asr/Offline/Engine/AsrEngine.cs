#if SHERPA_ONNX
using System;
using System.Collections.Concurrent;
using System.Threading;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
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
                $" (pool={poolSize})");

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

        // ── Instance creation ──

        /// <summary>
        /// Creates a native OfflineRecognizer and validates it
        /// by creating a test stream. Returns null on failure.
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
