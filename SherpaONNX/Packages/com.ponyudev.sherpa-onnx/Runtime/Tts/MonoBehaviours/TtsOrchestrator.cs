using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Thin MonoBehaviour wrapper around <see cref="TtsService"/>.
    /// Intended for users who do not use a DI container.
    /// Drop this component on a GameObject, and it will auto-initialize
    /// the TTS engine from StreamingAssets settings on Awake.
    /// Access the full API via <see cref="Service"/> (ITtsService).
    /// </summary>
    public class TtsOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private bool _initializeOnAwake = true;

        private TtsService _service;

        /// <summary>
        /// The TTS service exposed as an interface.
        /// Use this to generate speech, switch profiles, etc.
        /// </summary>
        public ITtsService Service => _service;

        private void Awake()
        {
            _service = new TtsService();

            if (_initializeOnAwake)
                _service.Initialize();
        }

        private void OnDestroy()
        {
            _service?.Dispose();
            _service = null;
        }
    }
}
