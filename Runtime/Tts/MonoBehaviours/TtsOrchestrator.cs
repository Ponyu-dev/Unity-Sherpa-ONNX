using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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

        [SerializeField]
        [Tooltip("Playback mode for the non-pooled fallback path " +
                 "(when no cache is configured). Pooled playback ignores this.")]
        private TtsPlaybackMode _defaultPlaybackMode = TtsPlaybackMode.Overlap;

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
        /// is available, otherwise creates a new AudioClip each time
        /// (auto-disposed after playback per <see cref="DefaultPlaybackMode"/>).
        /// </summary>
        public TtsResult GenerateAndPlay(string text)
        {
            var svc = Service;
            var cache = CacheControl;
            if (cache != null)
                return svc.GenerateAndPlay(text, cache, this);

            return svc.GenerateAndPlay(text, GetOrCreateSource(), _defaultPlaybackMode);
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

            return svc.GenerateAndPlayAsync(text, GetOrCreateSource(), _defaultPlaybackMode);
        }

        /// <summary>
        /// Mode used by the non-pooled fallback (when no cache is configured).
        /// Set in inspector or at runtime before calling GenerateAndPlay.
        /// </summary>
        public TtsPlaybackMode DefaultPlaybackMode
        {
            get => _defaultPlaybackMode;
            set => _defaultPlaybackMode = value;
        }

        // ── Handle-based playback ──

        private readonly List<TtsPlaybackHandle> _activeHandles = new();

        /// <summary>
        /// Generates speech and starts playback, returning a handle for
        /// stop / fade / completion observation. Uses pooled AudioSource
        /// when cache is available, otherwise the fallback source.
        /// <para/>
        /// The handle is auto-tracked by this orchestrator and disposed
        /// in <c>OnDestroy</c>; <see cref="StopAll"/> can fade them out
        /// at any time.
        /// </summary>
        public async Task<TtsPlaybackHandle> GenerateAndPlayWithHandleAsync(
            string text, CancellationToken ct = default)
        {
            var svc = Service;
            var cache = CacheControl;

            TtsPlaybackHandle handle = cache != null
                ? await svc.GenerateAndPlayWithHandleAsync(text, cache, ct)
                : await svc.GenerateAndPlayWithHandleAsync(text, GetOrCreateSource(), ct);

            if (handle != null)
                Track(handle);

            return handle;
        }

        /// <summary>
        /// Stops all currently-playing handles. If <paramref name="fadeSeconds"/>
        /// is &gt; 0, fades each one out over that duration in parallel.
        /// </summary>
        public async UniTask StopAll(float fadeSeconds = 0f)
        {
            // Snapshot to avoid mutation during iteration (handle.Stopped
            // event removes itself from _activeHandles).
            var snapshot = _activeHandles.ToArray();
            if (snapshot.Length == 0)
                return;

            var tasks = new UniTask[snapshot.Length];
            for (int i = 0; i < snapshot.Length; i++)
                tasks[i] = snapshot[i].StopAsync(fadeSeconds);

            await UniTask.WhenAll(tasks);
        }

        /// <summary>Number of currently tracked playback handles.</summary>
        public int ActivePlaybackCount => _activeHandles.Count;

        private void Track(TtsPlaybackHandle handle)
        {
            _activeHandles.Add(handle);

            void OnEnd()
            {
                handle.Completed -= OnEnd;
                handle.Stopped -= OnEnd;
                _activeHandles.Remove(handle);
            }

            handle.Completed += OnEnd;
            handle.Stopped += OnEnd;
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
            // Dispose any in-flight playbacks first so their cleanup
            // (clip destroy / pool return) runs before we dispose the
            // service (which cancels the service-level CTS).
            for (int i = _activeHandles.Count - 1; i >= 0; i--)
                _activeHandles[i]?.Dispose();
            _activeHandles.Clear();

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
