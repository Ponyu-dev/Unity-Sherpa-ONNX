using System;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Thin MonoBehaviour wrapper around <see cref="TtsService"/>.
    /// Intended for users who do not use a DI container.
    /// Drop this component on a GameObject, and it will auto-initialize
    /// the TTS engine from StreamingAssets settings on Awake.
    /// On Android, files are extracted from APK first (async).
    /// Access the full API via <see cref="Service"/> (ITtsService).
    /// Cache management via <see cref="CacheControl"/> (ITtsCacheControl).
    /// </summary>
    public class TtsOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private bool _initializeOnAwake = true;

        private TtsService _innerService;
        private CachedTtsService _cachedService;

        /// <summary>True when async initialization has completed.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Fires once after async initialization completes.</summary>
        public event Action Initialized;

        /// <summary>
        /// The TTS service exposed as an interface.
        /// Returns the cached decorator if caching is configured;
        /// otherwise the raw TtsService.
        /// </summary>
        public ITtsService Service =>
            (ITtsService)_cachedService ?? _innerService;

        /// <summary>
        /// Cache management interface. Null if caching is disabled.
        /// </summary>
        public ITtsCacheControl CacheControl => _cachedService;

        // ── GenerateAndPlay shortcuts ──

        /// <summary>
        /// Generates speech and plays it using pooled objects if cache
        /// is available, otherwise creates a new AudioClip each time.
        /// </summary>
        public TtsResult GenerateAndPlay(string text)
        {
            var svc = Service;
            var cache = CacheControl;
            if (cache != null)
                return svc.GenerateAndPlay(text, cache, this);

            return svc.GenerateAndPlay(text, GetOrCreateSource());
        }

        /// <summary>
        /// Generates speech on a background thread and plays it.
        /// Uses pooled objects when cache is available.
        /// </summary>
        public Task<TtsResult> GenerateAndPlayAsync(string text)
        {
            var svc = Service;
            var cache = CacheControl;
            if (cache != null)
                return svc.GenerateAndPlayAsync(text, cache, this);

            return svc.GenerateAndPlayAsync(text, GetOrCreateSource());
        }

        // ── Lifecycle ──

        private async void Awake()
        {
            _innerService = new TtsService();

            if (_initializeOnAwake)
                await _innerService.InitializeAsync();

            var cache = _innerService.Settings?.cache;
            if (cache != null)
            {
                _cachedService = new CachedTtsService(
                    _innerService, cache, transform);
            }

            IsInitialized = true;
            Initialized?.Invoke();
        }

        private void OnDestroy()
        {
            if (_cachedService != null)
            {
                _cachedService.Dispose();
                _cachedService = null;
                _innerService = null;
            }
            else
            {
                _innerService?.Dispose();
                _innerService = null;
            }
        }

        // ── Private helpers ──

        private AudioSource _fallbackSource;

        private AudioSource GetOrCreateSource()
        {
            if (_fallbackSource != null)
                return _fallbackSource;

            _fallbackSource = gameObject.AddComponent<AudioSource>();
            _fallbackSource.playOnAwake = false;
            return _fallbackSource;
        }
    }
}
