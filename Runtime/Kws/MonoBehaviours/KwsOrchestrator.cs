using System;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Kws.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Kws
{
    /// <summary>
    /// Thin MonoBehaviour wrapper around <see cref="KwsService"/>.
    /// Intended for users who do not use a DI container.
    /// Auto-initializes KWS on Awake, connects to
    /// <see cref="MicrophoneSource"/> for always-on keyword listening.
    /// </summary>
    public class KwsOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private bool _initializeOnAwake = true;

        [SerializeField]
        private bool _startRecordingOnInit = true;

        private KwsService _kwsService;
        private MicrophoneSource _mic;

        /// <summary>True when async initialization has completed.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Fires once after async initialization completes.</summary>
        public event Action Initialized;

        /// <summary>Fires when a keyword is detected.</summary>
        public event Action<KwsResult> OnKeywordDetected;

        /// <summary>The KWS service exposed as an interface.</summary>
        public IKwsService KwsService => _kwsService;

        /// <summary>The microphone source.</summary>
        public MicrophoneSource Microphone => _mic;

        // ── Lifecycle ──

        private async void Awake()
        {
            if (!_initializeOnAwake)
                return;

            _kwsService = new KwsService();

            await _kwsService.InitializeAsync();

            if (!_kwsService.IsReady)
            {
                Debug.LogWarning("[SherpaOnnx] KwsOrchestrator: KWS service failed to initialize.");
                return;
            }

            _kwsService.OnKeywordDetected += HandleKeywordDetected;
            _kwsService.StartSession();

            _mic = new MicrophoneSource();
            _mic.SamplesAvailable += HandleSamples;

            IsInitialized = true;
            Initialized?.Invoke();

            if (_startRecordingOnInit)
                await _mic.StartRecordingAsync();
        }

        private void OnDestroy()
        {
            if (_mic != null)
            {
                _mic.SamplesAvailable -= HandleSamples;
                _mic.Dispose();
                _mic = null;
            }

            if (_kwsService != null)
            {
                _kwsService.OnKeywordDetected -= HandleKeywordDetected;
                _kwsService.Dispose();
                _kwsService = null;
            }
        }

        // ── Private ──

        private void HandleSamples(float[] samples)
        {
            if (_kwsService == null)
                return;

            int sampleRate = _kwsService.ActiveProfile?.sampleRate ?? 16000;
            _kwsService.AcceptSamples(samples, sampleRate);
            _kwsService.ProcessAvailableFrames();
        }

        private void HandleKeywordDetected(KwsResult result)
        {
            OnKeywordDetected?.Invoke(result);
        }
    }
}
