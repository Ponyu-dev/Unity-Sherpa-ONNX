using System;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Cache
{
    /// <summary>
    /// Decorator over <see cref="ITtsService"/> adding LRU result caching,
    /// AudioClip pooling and AudioSource pooling.
    /// Each cache can be toggled at runtime via <see cref="ITtsCacheControl"/>.
    /// Callback-based methods are forwarded without caching.
    /// </summary>
    public sealed class CachedTtsService : ITtsService, ITtsCacheControl
    {
        private readonly ITtsService _inner;
        private readonly TtsResultCache _resultCache;
        private readonly AudioClipPool _clipPool;
        private readonly AudioSourcePool _sourcePool;

        private bool _resultCacheEnabled;
        private bool _clipPoolEnabled;
        private bool _sourcePoolEnabled;

        public CachedTtsService(
            ITtsService inner,
            TtsCacheSettings settings,
            Transform sourceParent = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

            if (settings == null)
                settings = new TtsCacheSettings();

            // Always create pools so they can be toggled at runtime.
            _resultCache = new TtsResultCache(settings.resultCacheSize);
            _clipPool = new AudioClipPool(settings.audioClipPoolSize);
            _sourcePool = sourceParent != null
                ? new AudioSourcePool(sourceParent, settings.audioSourcePoolSize)
                : null;

            _resultCacheEnabled = settings.resultCacheEnabled;
            _clipPoolEnabled = settings.audioClipEnabled;
            _sourcePoolEnabled = settings.audioSourceEnabled;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] CachedTtsService created " +
                $"(results={_resultCacheEnabled}, " +
                $"clips={_clipPoolEnabled}, " +
                $"sources={_sourcePoolEnabled}).");
        }

        // ── ITtsService properties ──

        public bool IsReady => _inner.IsReady;
        public TtsProfile ActiveProfile => _inner.ActiveProfile;
        public TtsSettingsData Settings => _inner.Settings;

        public int EnginePoolSize
        {
            get => _inner.EnginePoolSize;
            set => _inner.EnginePoolSize = value;
        }

        // ── Lifecycle ──

        public void Initialize()
        {
            _inner.Initialize();
        }

        public void LoadProfile(TtsProfile profile)
        {
            _inner.LoadProfile(profile);
            _resultCache.Clear();
        }

        public void SwitchProfile(int index)
        {
            _inner.SwitchProfile(index);
            _resultCache.Clear();
        }

        public void SwitchProfile(string profileName)
        {
            _inner.SwitchProfile(profileName);
            _resultCache.Clear();
        }

        // ── Cached generation ──

        public TtsResult Generate(string text)
        {
            if (!_inner.IsReady)
                return _inner.Generate(text);

            var profile = _inner.ActiveProfile;
            return CachedGenerate(
                text, profile.speed, profile.speakerId,
                () => _inner.Generate(text));
        }

        public TtsResult Generate(string text, float speed, int speakerId)
        {
            return CachedGenerate(
                text, speed, speakerId,
                () => _inner.Generate(text, speed, speakerId));
        }

        public Task<TtsResult> GenerateAsync(string text)
        {
            if (!_inner.IsReady)
                return _inner.GenerateAsync(text);

            var profile = _inner.ActiveProfile;
            return CachedGenerateAsync(
                text, profile.speed, profile.speakerId,
                () => _inner.GenerateAsync(text));
        }

        public Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId)
        {
            return CachedGenerateAsync(
                text, speed, speakerId,
                () => _inner.GenerateAsync(text, speed, speakerId));
        }

        // ── Callback methods (forwarded, not cached) ──

        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            return _inner.GenerateWithCallback(text, speed, speakerId, callback);
        }

        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            return _inner.GenerateWithCallbackProgress(
                text, speed, speakerId, callback);
        }

        public TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            return _inner.GenerateWithConfig(text, config, callback);
        }

        public Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            return _inner.GenerateWithCallbackAsync(
                text, speed, speakerId, callback);
        }

        public Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            return _inner.GenerateWithCallbackProgressAsync(
                text, speed, speakerId, callback);
        }

        public Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            return _inner.GenerateWithConfigAsync(text, config, callback);
        }

        // ── ITtsCacheControl: enable/disable ──

        public bool ResultCacheEnabled
        {
            get => _resultCacheEnabled;
            set
            {
                _resultCacheEnabled = value;
                if (!value)
                    _resultCache.Clear();
            }
        }

        public bool AudioClipPoolEnabled
        {
            get => _clipPoolEnabled;
            set
            {
                _clipPoolEnabled = value;
                if (!value)
                    _clipPool.Clear();
            }
        }

        public bool AudioSourcePoolEnabled
        {
            get => _sourcePoolEnabled;
            set
            {
                _sourcePoolEnabled = value;
                if (!value)
                    _sourcePool?.Clear();
            }
        }

        // ── ITtsCacheControl: sizes ──

        public int ResultCacheMaxSize
        {
            get => _resultCache.MaxSize;
            set => _resultCache.MaxSize = value;
        }

        public int AudioClipPoolMaxSize
        {
            get => _clipPool.MaxSize;
            set => _clipPool.MaxSize = value;
        }

        public int AudioSourcePoolMaxSize
        {
            get => _sourcePool?.MaxSize ?? 0;
            set
            {
                if (_sourcePool != null)
                    _sourcePool.MaxSize = value;
            }
        }

        // ── ITtsCacheControl: counts ──

        public int ResultCacheCount => _resultCache.Count;
        public int AudioClipAvailableCount => _clipPool.AvailableCount;
        public int AudioSourceAvailableCount => _sourcePool?.AvailableCount ?? 0;

        // ── ITtsCacheControl: clear ──

        public void ClearAll()
        {
            _resultCache.Clear();
            _clipPool.Clear();
            _sourcePool?.Clear();
        }

        public void ClearResultCache()
        {
            _resultCache.Clear();
        }

        public void ClearClipPool()
        {
            _clipPool.Clear();
        }

        public void ClearSourcePool()
        {
            _sourcePool?.Clear();
        }

        // ── ITtsCacheControl: rent/return ──

        public AudioClip RentClip(TtsResult result)
        {
            if (!_clipPoolEnabled || result == null || !result.IsValid)
                return null;

            var clip = _clipPool.Rent(result.NumSamples, result.SampleRate);
            if (clip != null)
                clip.SetData(result.Samples, 0);
            return clip;
        }

        public void ReturnClip(AudioClip clip)
        {
            if (_clipPoolEnabled)
                _clipPool.Return(clip);
        }

        public AudioSource RentSource()
        {
            if (!_sourcePoolEnabled)
                return null;
            return _sourcePool?.Rent();
        }

        public void ReturnSource(AudioSource source)
        {
            _sourcePool?.Return(source);
        }

        // ── Dispose ──

        public void Dispose()
        {
            _resultCache.Dispose();
            _clipPool.Dispose();
            _sourcePool?.Dispose();
            _inner.Dispose();
        }

        // ── Private ──

        private TtsResult CachedGenerate(
            string text, float speed, int speakerId,
            Func<TtsResult> generate)
        {
            if (!_resultCacheEnabled)
                return generate();

            var key = new TtsCacheKey(text, speed, speakerId);
            var cached = _resultCache.TryGet(key);
            if (cached != null)
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Cache hit: " + key);
                return cached;
            }

            var result = generate();
            if (result != null && result.IsValid)
                _resultCache.Add(key, result);
            return result;
        }

        private async Task<TtsResult> CachedGenerateAsync(
            string text, float speed, int speakerId,
            Func<Task<TtsResult>> generateAsync)
        {
            if (!_resultCacheEnabled)
                return await generateAsync();

            var key = new TtsCacheKey(text, speed, speakerId);
            var cached = _resultCache.TryGet(key);
            if (cached != null)
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Cache hit: " + key);
                return cached;
            }

            var result = await generateAsync();
            if (result != null && result.IsValid)
                _resultCache.Add(key, result);
            return result;
        }
    }
}
