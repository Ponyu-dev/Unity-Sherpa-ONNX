using System;
using PonyuDev.SherpaOnnx.Tts.Cache;
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
    }
}
