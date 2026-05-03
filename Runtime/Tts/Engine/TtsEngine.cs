#if SHERPA_ONNX
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Common.Validation;
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
        private readonly ReaderWriterLockSlim _lifecycleLock = new();

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

            if (ModelFileValidator.BlockIfInt8Model(
                    modelDir, "TTS", profile.allowInt8))
                return;

            _lastConfig = TtsConfigBuilder.Build(profile, modelDir);

            // Create first instance and validate it.
            var first = CreateInstance(_lastConfig);
            if (first == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsEngine: failed to create OfflineTts " +
                    $"for '{profile.profileName}'. Check model paths and config.");
                return;
            }

            _pool = new OfflineTts[poolSize];
            _pool[0] = first;
            _available.Enqueue(first);

            for (int i = 1; i < poolSize; i++)
            {
                var tts = CreateInstance(_lastConfig);
                if (tts != null)
                {
                    _pool[i] = tts;
                    _available.Enqueue(tts);
                }
                else
                {
                    SherpaOnnxLog.RuntimeWarning(
                        $"[SherpaOnnx] TtsEngine: pool instance {i} " +
                        "creation failed, skipping.");
                }
            }

            _poolSize = poolSize;
            _semaphore = new SemaphoreSlim(poolSize, poolSize);

            SampleRate = first.SampleRate;
            NumSpeakers = first.NumSpeakers;

            EngineRegistry.Register(this);

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

            _lifecycleLock.EnterWriteLock();
            try
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
            finally
            {
                _lifecycleLock.ExitWriteLock();
            }
        }

        public void Unload()
        {
            EngineRegistry.Unregister(this);

            _lifecycleLock.EnterWriteLock();
            try
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
            finally
            {
                _lifecycleLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Unload();
            _lifecycleLock.Dispose();
        }

        // ── Simple generation ──

        public TtsResult Generate(string text, float speed, int speakerId)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerate(tts => RunGenerate(tts, text, speed, speakerId));
        }

        // ── Callback generation ──

        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerate(
                tts => RunGenerateWithCallback(tts, text, speed, speakerId, callback));
        }

        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerate(
                tts => RunGenerateWithCallbackProgress(tts, text, speed, speakerId, callback));
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

            var result = RentAndGenerate(
                tts => RunGenerateWithConfig(tts, text, nativeConfig, callback));

            if (result != null)
                return result;

            // Fallback: model may not support GenerateWithConfig.
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] GenerateWithConfig failed — " +
                "falling back to GenerateWithCallbackProgress.");

            return GenerateWithCallbackProgress(
                text, config.Speed, config.SpeakerId, callback);
        }

        // ── Async generation (cancellable) ──

        public Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId, CancellationToken ct)
        {
            if (!ValidateBeforeGenerate(text))
                return Task.FromResult<TtsResult>(null);

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerateAsync(
                tts => RunGenerate(tts, text, speed, speakerId, ct),
                ct);
        }

        public Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback,
            CancellationToken ct)
        {
            if (!ValidateBeforeGenerate(text))
                return Task.FromResult<TtsResult>(null);

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerateAsync(
                tts => RunGenerateWithCallback(tts, text, speed, speakerId, callback, ct),
                ct);
        }

        public Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId, TtsCallbackProgress callback,
            CancellationToken ct)
        {
            if (!ValidateBeforeGenerate(text))
                return Task.FromResult<TtsResult>(null);

            LogGenerationStart(text, speed, speakerId);

            return RentAndGenerateAsync(
                tts => RunGenerateWithCallbackProgress(tts, text, speed, speakerId, callback, ct),
                ct);
        }

        public Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config, TtsCallbackProgress callback,
            CancellationToken ct)
        {
            if (!ValidateBeforeGenerate(text))
                return Task.FromResult<TtsResult>(null);

            if (config == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine.GenerateWithConfigAsync: config is null.");
                return Task.FromResult<TtsResult>(null);
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generating async with config: " +
                $"\"{Truncate(text, 60)}\" " +
                $"(speed={config.Speed}, sid={config.SpeakerId})");

            var nativeConfig = TtsGenerationConfigMapper.ToNative(config);

            return RentAndGenerateAsync(
                tts => RunGenerateWithConfig(tts, text, nativeConfig, callback, ct),
                ct);
        }

        // ── Sync generation bodies (named for debug stack traces) ──

        /// <summary>Body of sync <see cref="Generate(string, float, int)"/>.</summary>
        private static TtsResult RunGenerate(
            OfflineTts tts, string text, float speed, int speakerId)
        {
            var audio = tts.Generate(text, speed, speakerId);
            return WrapAudio(audio);
        }

        /// <summary>Body of sync <see cref="GenerateWithCallback"/>.</summary>
        private static TtsResult RunGenerateWithCallback(
            OfflineTts tts, string text, float speed, int speakerId, TtsCallback callback)
        {
            var bridge = MakeCallback(callback);
            var audio = tts.GenerateWithCallback(text, speed, speakerId, bridge);
            GC.KeepAlive(bridge);
            return WrapAudio(audio);
        }

        /// <summary>Body of sync <see cref="GenerateWithCallbackProgress"/>.</summary>
        private static TtsResult RunGenerateWithCallbackProgress(
            OfflineTts tts, string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            var bridge = MakeProgressCallback(callback);
            var audio = tts.GenerateWithCallbackProgress(text, speed, speakerId, bridge);
            GC.KeepAlive(bridge);
            return WrapAudio(audio);
        }

        /// <summary>Body of sync <see cref="GenerateWithConfig"/>.</summary>
        private static TtsResult RunGenerateWithConfig(
            OfflineTts tts, string text, OfflineTtsGenerationConfig nativeConfig,
            TtsCallbackProgress callback)
        {
            var bridge = MakeProgressWithArgCallback(callback);
            var audio = tts.GenerateWithConfig(text, nativeConfig, bridge);
            GC.KeepAlive(bridge);
            return WrapAudio(audio);
        }

        // ── Async generation bodies (named for debug stack traces) ──

        /// <summary>
        /// Body of <see cref="GenerateAsync"/> — runs on a worker thread via Task.Run.
        /// Wraps in a cancellation-only callback so sherpa-onnx can be aborted.
        /// </summary>
        private static TtsResult RunGenerate(
            OfflineTts tts, string text, float speed, int speakerId, CancellationToken ct)
        {
            var cancelCheck = MakeCancellationCallback(ct);
            var audio = tts.GenerateWithCallback(text, speed, speakerId, cancelCheck);
            GC.KeepAlive(cancelCheck);
            ct.ThrowIfCancellationRequested();
            return WrapAudio(audio);
        }

        /// <summary>Body of <see cref="GenerateWithCallbackAsync"/>.</summary>
        private static TtsResult RunGenerateWithCallback(
            OfflineTts tts, string text, float speed, int speakerId,
            TtsCallback callback, CancellationToken ct)
        {
            var bridge = MakeCancellableCallback(callback, ct);
            var audio = tts.GenerateWithCallback(text, speed, speakerId, bridge);
            GC.KeepAlive(bridge);
            ct.ThrowIfCancellationRequested();
            return WrapAudio(audio);
        }

        /// <summary>Body of <see cref="GenerateWithCallbackProgressAsync"/>.</summary>
        private static TtsResult RunGenerateWithCallbackProgress(
            OfflineTts tts, string text, float speed, int speakerId,
            TtsCallbackProgress callback, CancellationToken ct)
        {
            var bridge = MakeCancellableProgressCallback(callback, ct);
            var audio = tts.GenerateWithCallbackProgress(text, speed, speakerId, bridge);
            GC.KeepAlive(bridge);
            ct.ThrowIfCancellationRequested();
            return WrapAudio(audio);
        }

        /// <summary>Body of <see cref="GenerateWithConfigAsync"/>.</summary>
        private static TtsResult RunGenerateWithConfig(
            OfflineTts tts, string text, OfflineTtsGenerationConfig nativeConfig,
            TtsCallbackProgress callback, CancellationToken ct)
        {
            var bridge = MakeCancellableProgressWithArgCallback(callback, ct);
            var audio = tts.GenerateWithConfig(text, nativeConfig, bridge);
            GC.KeepAlive(bridge);
            ct.ThrowIfCancellationRequested();
            return WrapAudio(audio);
        }

        // ── Callback factories (encapsulate the native-bridge lambdas) ──

        /// <summary>
        /// Wraps a user <see cref="TtsCallback"/> into a native callback.
        /// Defensive: returns 1 (continue) if user callback is null.
        /// </summary>
        private static OfflineTtsCallback MakeCallback(TtsCallback userCallback)
        {
            return (IntPtr samples, int n) =>
            {
                var managed = CopySamplesFromNative(samples, n);
                return userCallback != null ? userCallback(managed, n) : 1;
            };
        }

        /// <summary>
        /// Wraps a user <see cref="TtsCallbackProgress"/> into a native callback.
        /// </summary>
        private static OfflineTtsCallbackProgress MakeProgressCallback(
            TtsCallbackProgress userCallback)
        {
            return (IntPtr samples, int n, float progress) =>
            {
                var managed = CopySamplesFromNative(samples, n);
                return userCallback != null ? userCallback(managed, n, progress) : 1;
            };
        }

        /// <summary>
        /// Wraps a user <see cref="TtsCallbackProgress"/> into a native callback
        /// using the variant that carries an arg pointer (unused).
        /// </summary>
        private static OfflineTtsCallbackProgressWithArg MakeProgressWithArgCallback(
            TtsCallbackProgress userCallback)
        {
            return (IntPtr samples, int n, float progress, IntPtr arg) =>
            {
                var managed = CopySamplesFromNative(samples, n);
                return userCallback != null ? userCallback(managed, n, progress) : 1;
            };
        }

        /// <summary>
        /// Builds a no-op-but-cancellable native callback. Returns 0 (abort)
        /// when <paramref name="ct"/> is triggered, else 1 (continue).
        /// Used by <see cref="GenerateAsync"/> when there's no user callback.
        /// </summary>
        private static OfflineTtsCallback MakeCancellationCallback(CancellationToken ct)
            => (samples, n) => ct.IsCancellationRequested ? 0 : 1;

        /// <summary>
        /// Wraps a user <see cref="TtsCallback"/> so it runs alongside the
        /// cancellation check. Marshals samples from native and dispatches.
        /// </summary>
        private static OfflineTtsCallback MakeCancellableCallback(
            TtsCallback userCallback, CancellationToken ct)
        {
            return (IntPtr samples, int n) =>
            {
                if (ct.IsCancellationRequested)
                    return 0;
                var managed = CopySamplesFromNative(samples, n);
                return userCallback != null ? userCallback(managed, n) : 1;
            };
        }

        /// <summary>
        /// Wraps a user <see cref="TtsCallbackProgress"/> with cancellation.
        /// </summary>
        private static OfflineTtsCallbackProgress MakeCancellableProgressCallback(
            TtsCallbackProgress userCallback, CancellationToken ct)
        {
            return (IntPtr samples, int n, float progress) =>
            {
                if (ct.IsCancellationRequested)
                    return 0;
                var managed = CopySamplesFromNative(samples, n);
                return userCallback != null ? userCallback(managed, n, progress) : 1;
            };
        }

        /// <summary>
        /// Wraps a user <see cref="TtsCallbackProgress"/> with cancellation
        /// using the variant that carries a native arg pointer (unused).
        /// </summary>
        private static OfflineTtsCallbackProgressWithArg MakeCancellableProgressWithArgCallback(
            TtsCallbackProgress userCallback, CancellationToken ct)
        {
            return (IntPtr samples, int n, float progress, IntPtr arg) =>
            {
                if (ct.IsCancellationRequested)
                    return 0;
                var managed = CopySamplesFromNative(samples, n);
                return userCallback != null ? userCallback(managed, n, progress) : 1;
            };
        }

        // ── Pool core ──

        /// <summary>
        /// Async-friendly counterpart of <see cref="RentAndGenerate"/>. Uses
        /// <see cref="SemaphoreSlim.WaitAsync(CancellationToken)"/> so a
        /// caller waiting for a free engine instance can be cancelled.
        /// <para/>
        /// Important: does NOT hold <see cref="_lifecycleLock"/> across the
        /// await, because <see cref="ReaderWriterLockSlim"/> is thread-affine
        /// and would deadlock. Callers must not <c>Unload</c>/<c>Resize</c>
        /// while async generations are in flight; cancel them first via CT.
        /// </summary>
        private async Task<TtsResult> RentAndGenerateAsync(
            Func<OfflineTts, TtsResult> action, CancellationToken ct)
        {
            if (!IsLoaded)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine: engine not loaded.");
                return null;
            }

            var semaphore = _semaphore;
            if (semaphore == null)
                return null;

            await semaphore.WaitAsync(ct).ConfigureAwait(false);

            OfflineTts tts = null;
            bool dequeued = false;
            try
            {
                if (!_available.TryDequeue(out tts))
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] TtsEngine: no engine available.");
                    return null;
                }
                dequeued = true;

                ct.ThrowIfCancellationRequested();

                return await Task.Run(() => action(tts), ct).ConfigureAwait(false);
            }
            finally
            {
                if (dequeued && tts != null)
                    _available.Enqueue(tts);
                semaphore.Release();
            }
        }

        private TtsResult RentAndGenerate(Func<OfflineTts, TtsResult> action)
        {
            _lifecycleLock.EnterReadLock();
            try
            {
                if (!IsLoaded)
                {
                    SherpaOnnxLog.RuntimeError(
                        "[SherpaOnnx] TtsEngine: engine not loaded.");
                    return null;
                }

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
            finally
            {
                _lifecycleLock.ExitReadLock();
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
                var tts = CreateInstance(_lastConfig);
                if (tts != null)
                {
                    newPool[i] = tts;
                    _available.Enqueue(tts);
                }
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

        // ── Instance creation ──

        /// <summary>
        /// Creates a native OfflineTts and validates it via SampleRate access.
        /// Returns null if creation fails (invalid config, missing model, etc.).
        /// Uses <see cref="NativeLocaleGuard"/> to force C locale — required
        /// on Android where system locale may use comma as decimal separator,
        /// causing sherpa-onnx float validation to fail.
        /// </summary>
        private static OfflineTts CreateInstance(OfflineTtsConfig config)
        {
            try
            {
                OfflineTts tts;
                using (NativeLocaleGuard.Begin())
                {
                    tts = new OfflineTts(config);
                }

                // Validate: accessing SampleRate will crash if the native
                // handle is null (e.g. invalid config). Catch that here.
                _ = tts.SampleRate;
                return tts;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] OfflineTts creation failed: {ex.Message}");
                return null;
            }
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

            SherpaOnnxLog.RuntimeLog(FormattableString.Invariant(
                $"[SherpaOnnx] TTS generated: {samples.Length} samples, {sampleRate}Hz, {samples.Length / (float)sampleRate:F2}s"));

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
