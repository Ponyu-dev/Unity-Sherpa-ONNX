#if SHERPA_ONNX
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Config;
using PonyuDev.SherpaOnnx.Tts.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Pool of native <see cref="OfflineTts"/> instances.
    /// Allows N concurrent generations via SemaphoreSlim + ConcurrentQueue.
    /// Thread-safe. Never throws — logs errors instead.
    /// </summary>
    public sealed class TtsEngine : ITtsEngine
    {
        private readonly ConcurrentQueue<OfflineTts> _available = new();
        private readonly object _resizeLock = new();

        private OfflineTts[] _pool;
        private SemaphoreSlim _semaphore;
        private OfflineTtsConfig _lastConfig;
        private int _poolSize;

        public int SampleRate { get; private set; }
        public int NumSpeakers { get; private set; }
        public bool IsLoaded => _pool != null && _pool.Length > 0;
        public int PoolSize => _poolSize;

        // ── Lifecycle ──

        public void Load(TtsProfile profile, string modelDir, int poolSize = 1)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine.Load: profile is null.");
                return;
            }

            Unload();

            poolSize = Math.Max(1, poolSize);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS engine loading: {profile.profileName}" +
                $" (pool={poolSize})");

            _lastConfig = TtsConfigBuilder.Build(profile, modelDir);
            _pool = new OfflineTts[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                _pool[i] = new OfflineTts(_lastConfig);
                _available.Enqueue(_pool[i]);
            }

            _poolSize = poolSize;
            _semaphore = new SemaphoreSlim(poolSize, poolSize);

            // Read properties from the first instance.
            SampleRate = _pool[0].SampleRate;
            NumSpeakers = _pool[0].NumSpeakers;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS engine loaded: {profile.profileName} " +
                $"(sampleRate={SampleRate}, speakers={NumSpeakers}, " +
                $"pool={poolSize})");
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
                    $"[SherpaOnnx] TtsEngine resized to {newPoolSize}.");
            }
        }

        public void Unload()
        {
            if (_pool == null)
                return;

            // Drain and dispose all instances.
            while (_available.TryDequeue(out _)) { }

            foreach (var tts in _pool)
                tts?.Dispose();

            _pool = null;
            _poolSize = 0;

            _semaphore?.Dispose();
            _semaphore = null;

            SampleRate = 0;
            NumSpeakers = 0;

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TTS engine unloaded.");
        }

        public void Dispose()
        {
            Unload();
        }

        // ── Simple generation ──

        public TtsResult Generate(string text, float speed, int speakerId)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerate(tts =>
            {
                var audio = tts.Generate(text, speed, speakerId);
                return WrapAudio(audio);
            });
        }

        // ── Callback generation ──

        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerate(tts =>
            {
                OfflineTtsCallback nativeCallback = (IntPtr samples, int n) =>
                {
                    var managed = CopySamplesFromNative(samples, n);
                    return callback(managed, n);
                };

                var audio = tts.GenerateWithCallback(
                    text, speed, speakerId, nativeCallback);
                GC.KeepAlive(nativeCallback);
                return WrapAudio(audio);
            });
        }

        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerate(tts =>
            {
                OfflineTtsCallbackProgress nativeCallback =
                    (IntPtr samples, int n, float progress) =>
                    {
                        var managed = CopySamplesFromNative(samples, n);
                        return callback(managed, n, progress);
                    };

                var audio = tts.GenerateWithCallbackProgress(
                    text, speed, speakerId, nativeCallback);
                GC.KeepAlive(nativeCallback);
                return WrapAudio(audio);
            });
        }

        public TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            if (config == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine.GenerateWithConfig: config is null.");
                return null;
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generating with config: " +
                $"\"{Truncate(text, 60)}\" " +
                $"(speed={config.Speed}, sid={config.SpeakerId})");

            var nativeConfig = TtsGenerationConfigMapper.ToNative(config);

            var result = RentAndGenerate(tts =>
            {
                OfflineTtsCallbackProgressWithArg nativeCallback =
                    (IntPtr samples, int n, float progress, IntPtr arg) =>
                    {
                        var managed = CopySamplesFromNative(samples, n);
                        return callback(managed, n, progress);
                    };

                var audio = tts.GenerateWithConfig(
                    text, nativeConfig, nativeCallback);
                GC.KeepAlive(nativeCallback);
                return WrapAudio(audio);
            });

            if (result != null)
                return result;

            // Fallback: model may not support GenerateWithConfig.
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] GenerateWithConfig failed — " +
                "falling back to GenerateWithCallbackProgress.");

            return GenerateWithCallbackProgress(
                text, config.Speed, config.SpeakerId, callback);
        }

        // ── Pool core ──

        private TtsResult RentAndGenerate(Func<OfflineTts, TtsResult> action)
        {
            _semaphore.Wait();
            OfflineTts tts = null;
            try
            {
                if (!_available.TryDequeue(out tts))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] TtsEngine: no engine available.");
                    return null;
                }
                return action(tts);
            }
            finally
            {
                if (tts != null)
                    _available.Enqueue(tts);
                _semaphore.Release();
            }
        }

        // ── Resize helpers ──

        private void GrowPool(int newSize)
        {
            int oldSize = _pool.Length;
            var newPool = new OfflineTts[newSize];
            Array.Copy(_pool, newPool, oldSize);

            for (int i = oldSize; i < newSize; i++)
            {
                newPool[i] = new OfflineTts(_lastConfig);
                _available.Enqueue(newPool[i]);
            }

            _pool = newPool;
        }

        private void ShrinkPool(int newSize)
        {
            int toRemove = _poolSize - newSize;
            int removed = 0;

            // Dispose idle instances from the queue.
            while (removed < toRemove && _available.TryDequeue(out var tts))
            {
                tts.Dispose();
                removed++;
            }

            // Rebuild pool array from remaining queue contents.
            var remaining = _available.ToArray();
            _pool = new OfflineTts[remaining.Length];
            Array.Copy(remaining, _pool, remaining.Length);
        }

        // ── Private helpers ──

        private bool ValidateBeforeGenerate(string text)
        {
            if (!IsLoaded)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine: engine not loaded.");
                return false;
            }

            if (string.IsNullOrEmpty(text))
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsEngine: text is empty.");
                return false;
            }

            return true;
        }

        private static void LogGenerationStart(
            string text, float speed, int speakerId)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generating: \"{Truncate(text, 60)}\" " +
                $"(speed={speed}, speakerId={speakerId})");
        }

        private static TtsResult WrapAudio(OfflineTtsGeneratedAudio audio)
        {
            if (audio == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine: native returned null audio.");
                return null;
            }

            float[] samples;
            int sampleRate;

            try
            {
                samples = audio.Samples;
                sampleRate = audio.SampleRate;
            }
            catch (Exception)
            {
                // Expected for models that don't support GenerateWithConfig.
                return null;
            }

            if (samples == null || samples.Length == 0)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsEngine: native returned empty samples.");
                return null;
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generated: {samples.Length} samples, " +
                $"{sampleRate}Hz, {samples.Length / (float)sampleRate:F2}s");

            return new TtsResult(samples, sampleRate);
        }

        private static float[] CopySamplesFromNative(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0)
                return Array.Empty<float>();

            var managed = new float[count];
            Marshal.Copy(ptr, managed, 0, count);
            return managed;
        }

        private static string Truncate(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }
    }
}
#endif
