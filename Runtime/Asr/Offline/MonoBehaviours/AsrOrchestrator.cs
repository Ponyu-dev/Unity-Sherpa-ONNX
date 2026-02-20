using System;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Asr.Offline
{
    /// <summary>
    /// Thin MonoBehaviour wrapper around <see cref="AsrService"/>.
    /// Intended for users who do not use a DI container.
    /// Drop this component on a GameObject, and it will auto-initialize
    /// the ASR engine from StreamingAssets settings on Awake.
    /// Access the full API via <see cref="Service"/> (IAsrService).
    /// </summary>
    public class AsrOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private bool _initializeOnAwake = true;

        private AsrService _service;

        /// <summary>True when async initialization has completed.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Fires once after async initialization completes.</summary>
        public event Action Initialized;

        /// <summary>The ASR service exposed as an interface.</summary>
        public IAsrService Service => _service;

        // ── Convenience methods ──

        /// <summary>
        /// Extracts PCM samples from an AudioClip and runs recognition.
        /// Must be called on the main thread (AudioClip.GetData is not
        /// thread-safe).
        /// </summary>
        public AsrResult RecognizeFromClip(AudioClip clip)
        {
            if (_service == null || !_service.IsReady)
                return null;

            if (clip == null)
                return null;

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            return _service.Recognize(samples, clip.frequency);
        }

        /// <summary>
        /// Async version of RecognizeFromClip.
        /// Audio data extraction happens on main thread,
        /// recognition runs on a background thread.
        /// </summary>
        public Task<AsrResult> RecognizeFromClipAsync(AudioClip clip)
        {
            if (_service == null || !_service.IsReady)
                return Task.FromResult<AsrResult>(null);

            if (clip == null)
                return Task.FromResult<AsrResult>(null);

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            return _service.RecognizeAsync(samples, clip.frequency);
        }

        // ── Lifecycle ──

        private async void Awake()
        {
            _service = new AsrService();

            if (_initializeOnAwake)
                await _service.InitializeAsync();

            IsInitialized = true;
            Initialized?.Invoke();
        }

        private void OnDestroy()
        {
            _service?.Dispose();
            _service = null;
        }
    }
}
