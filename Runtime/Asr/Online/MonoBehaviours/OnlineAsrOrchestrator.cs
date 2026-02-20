using System;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Asr.Online
{
    /// <summary>
    /// Thin MonoBehaviour wrapper around <see cref="OnlineAsrService"/>.
    /// Wires <see cref="MicrophoneSource"/> to the streaming ASR pipeline.
    /// </summary>
    public class OnlineAsrOrchestrator : MonoBehaviour
    {
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _autoResetOnEndpoint = true;

        private OnlineAsrService _service;
        private MicrophoneSource _microphone;

        public bool IsInitialized { get; private set; }
        public IOnlineAsrService Service => _service;

        public event Action Initialized;
        public event Action<OnlineAsrResult> PartialResultReady;
        public event Action<OnlineAsrResult> FinalResultReady;
        public event Action EndpointDetected;

        // ── Microphone ──

        public void ConnectMicrophone(MicrophoneSource mic)
        {
            DisconnectMicrophone();

            if (mic == null)
                return;

            _microphone = mic;
            _microphone.SamplesAvailable += OnMicSamplesAvailable;
        }

        public void DisconnectMicrophone()
        {
            if (_microphone == null)
                return;

            _microphone.SamplesAvailable -= OnMicSamplesAvailable;
            _microphone = null;
        }

        // ── Lifecycle ──

        private async void Awake()
        {
            _service = new OnlineAsrService();

            if (_initializeOnAwake)
                await _service.InitializeAsync();

            SubscribeService();
            IsInitialized = true;
            Initialized?.Invoke();
        }

        private void OnDestroy()
        {
            DisconnectMicrophone();
            UnsubscribeService();
            _service?.Dispose();
            _service = null;
        }

        // ── Private: event wiring ──

        private void SubscribeService()
        {
            if (_service == null)
                return;
            _service.PartialResultReady += OnPartialResult;
            _service.FinalResultReady += OnFinalResult;
            _service.EndpointDetected += OnEndpoint;
        }

        private void UnsubscribeService()
        {
            if (_service == null)
                return;
            _service.PartialResultReady -= OnPartialResult;
            _service.FinalResultReady -= OnFinalResult;
            _service.EndpointDetected -= OnEndpoint;
        }

        private void OnMicSamplesAvailable(float[] samples)
        {
            if (_service == null || !_service.IsSessionActive)
                return;

            _service.AcceptSamples(samples, _microphone.SampleRate);
            _service.ProcessAvailableFrames();
        }

        private void OnPartialResult(OnlineAsrResult result)
        {
            PartialResultReady?.Invoke(result);
        }

        private void OnFinalResult(OnlineAsrResult result)
        {
            FinalResultReady?.Invoke(result);
        }

        private void OnEndpoint()
        {
            if (_autoResetOnEndpoint)
                _service?.ResetStream();

            EndpointDetected?.Invoke();
        }
    }
}
