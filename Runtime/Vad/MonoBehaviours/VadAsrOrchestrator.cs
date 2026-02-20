using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Vad
{
    /// <summary>
    /// Thin MonoBehaviour wrapper around <see cref="VadAsrPipeline"/>.
    /// Intended for users who do not use a DI container.
    /// Auto-initializes VAD + ASR on Awake, connects to
    /// <see cref="MicrophoneSource"/> for live audio.
    /// </summary>
    public class VadAsrOrchestrator : MonoBehaviour
    {
        [SerializeField]
        private bool _initializeOnAwake = true;

        [SerializeField]
        private bool _startRecordingOnInit = true;

        private VadService _vadService;
        private AsrService _asrService;
        private VadAsrPipeline _pipeline;
        private MicrophoneSource _mic;

        /// <summary>True when async initialization has completed.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Fires once after async initialization completes.</summary>
        public event Action Initialized;

        /// <summary>Fires when ASR produces a result from a speech segment.</summary>
        public event Action<AsrResult> OnResult;

        /// <summary>Fires when speech starts.</summary>
        public event Action OnSpeechStart;

        /// <summary>Fires when speech ends.</summary>
        public event Action OnSpeechEnd;

        /// <summary>The VAD service exposed as an interface.</summary>
        public IVadService VadService => _vadService;

        /// <summary>The ASR service exposed as an interface.</summary>
        public IAsrService AsrService => _asrService;

        /// <summary>The combined pipeline.</summary>
        public VadAsrPipeline Pipeline => _pipeline;

        /// <summary>The microphone source.</summary>
        public MicrophoneSource Microphone => _mic;

        // ── Lifecycle ──

        private async void Awake()
        {
            if (!_initializeOnAwake)
                return;

            _vadService = new VadService();
            _asrService = new AsrService();

            await _vadService.InitializeAsync();
            await _asrService.InitializeAsync();

            _pipeline = new VadAsrPipeline(_vadService, _asrService);
            _pipeline.OnResult += HandleResult;
            _pipeline.OnSpeechStart += HandleSpeechStart;
            _pipeline.OnSpeechEnd += HandleSpeechEnd;

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

            if (_pipeline != null)
            {
                _pipeline.OnResult -= HandleResult;
                _pipeline.OnSpeechStart -= HandleSpeechStart;
                _pipeline.OnSpeechEnd -= HandleSpeechEnd;
                _pipeline.Dispose();
                _pipeline = null;
            }

            _vadService?.Dispose();
            _vadService = null;

            _asrService?.Dispose();
            _asrService = null;
        }

        // ── Private ──

        private void HandleSamples(float[] samples)
        {
            _pipeline?.AcceptSamples(samples);
        }

        private void HandleResult(AsrResult result)
        {
            OnResult?.Invoke(result);
        }

        private void HandleSpeechStart()
        {
            OnSpeechStart?.Invoke();
        }

        private void HandleSpeechEnd()
        {
            OnSpeechEnd?.Invoke();
        }
    }
}