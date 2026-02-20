using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Streaming recognition sample — uses <see cref="IOnlineAsrService"/>
    /// with <see cref="MicrophoneSource"/>. Shows partial results in
    /// real-time and accumulates final results in a transcript.
    /// </summary>
    public sealed class AsrStreamPanel : IAsrSamplePanel
    {
        private IOnlineAsrService _service;
        private MicrophoneSource _microphone;
        private Action _onBack;

        private Button _toggleButton;
        private Button _clearButton;
        private Button _backButton;
        private Label _partialLabel;
        private ScrollView _transcriptScroll;
        private Label _statusLabel;
        private Label _infoLabel;

        private bool _isRecording;

        // ── IAsrSamplePanel ──

        public void Bind(
            VisualElement root,
            IAsrService offlineService,
            IOnlineAsrService onlineService,
            MicrophoneSource microphone,
            AudioClip sampleClip,
            Action onBack)
        {
            _service = onlineService;
            _microphone = microphone;
            _onBack = onBack;

            _toggleButton = root.Q<Button>("toggleButton");
            _clearButton = root.Q<Button>("clearButton");
            _backButton = root.Q<Button>("backButton");
            _partialLabel = root.Q<Label>("partialLabel");
            _transcriptScroll = root.Q<ScrollView>("transcriptScroll");
            _statusLabel = root.Q<Label>("statusLabel");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_toggleButton != null)
                _toggleButton.clicked += HandleToggle;
            if (_clearButton != null)
                _clearButton.clicked += HandleClear;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            SubscribeService();
            UpdateInfo();
        }

        public void Unbind()
        {
            StopRecordingIfActive();
            UnsubscribeService();

            if (_toggleButton != null)
                _toggleButton.clicked -= HandleToggle;
            if (_clearButton != null)
                _clearButton.clicked -= HandleClear;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;

            _toggleButton = null;
            _clearButton = null;
            _backButton = null;
            _partialLabel = null;
            _transcriptScroll = null;
            _statusLabel = null;
            _infoLabel = null;
            _service = null;
            _microphone = null;
            _onBack = null;
        }

        // ── Handlers ──

        private async void HandleToggle()
        {
            if (_isRecording)
            {
                StopRecordingIfActive();
                return;
            }

            if (_service == null || !_service.IsReady)
            {
                SetStatus("Engine not loaded.");
                return;
            }

            if (_microphone == null)
            {
                SetStatus("No microphone available.");
                return;
            }

            SetStatus("Starting...");
            _toggleButton?.SetEnabled(false);

            try
            {
                bool started = await _microphone.StartRecordingAsync();
                if (!started)
                {
                    SetStatus("Microphone failed to start.");
                    return;
                }

                _microphone.SamplesAvailable += HandleMicSamples;
                _microphone.SilenceDetected += HandleSilenceDetected;
                _service.StartSession();
                _isRecording = true;

                UpdateToggleButton();
                SetStatus("Recording...");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrStreamPanel start error: {ex}");
            }
            finally
            {
                _toggleButton?.SetEnabled(true);
            }
        }

        private void HandleClear()
        {
            if (_transcriptScroll != null)
                _transcriptScroll.contentContainer.Clear();

            if (_partialLabel != null)
                _partialLabel.text = "";
        }

        private void HandleBack()
        {
            StopRecordingIfActive();
            _onBack?.Invoke();
        }

        // ── Microphone ──

        private void HandleMicSamples(float[] samples)
        {
            if (_service == null || !_service.IsSessionActive)
                return;

            _service.AcceptSamples(samples, _microphone.SampleRate);
            _service.ProcessAvailableFrames();
        }

        private void HandleSilenceDetected(string diagnosis)
        {
            StopRecordingIfActive();

            SetStatus(
                "Microphone silence detected — " +
                "voice capture is not available on this device. " +
                "This may be a hardware or OS-level issue.");

            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] AsrStreamPanel: " +
                "silence detected, recording stopped. " +
                "Diag: " + diagnosis);
        }

        // ── Service events ──

        private void HandlePartialResult(OnlineAsrResult result)
        {
            if (_partialLabel != null)
                _partialLabel.text = result.Text;
        }

        private void HandleFinalResult(OnlineAsrResult result)
        {
            if (_partialLabel != null)
                _partialLabel.text = "";

            AppendTranscriptLine(result.Text);
        }

        private void HandleEndpoint()
        {
            _service?.ResetStream();
        }

        // ── Helpers ──

        private void StopRecordingIfActive()
        {
            if (!_isRecording)
                return;

            if (_microphone != null)
            {
                _microphone.SamplesAvailable -= HandleMicSamples;
                _microphone.SilenceDetected -= HandleSilenceDetected;
                _microphone.StopRecording();
            }

            _service?.StopSession();
            _isRecording = false;

            UpdateToggleButton();
            SetStatus("Stopped.");
        }

        private void SubscribeService()
        {
            if (_service == null)
                return;

            _service.PartialResultReady += HandlePartialResult;
            _service.FinalResultReady += HandleFinalResult;
            _service.EndpointDetected += HandleEndpoint;
        }

        private void UnsubscribeService()
        {
            if (_service == null)
                return;

            _service.PartialResultReady -= HandlePartialResult;
            _service.FinalResultReady -= HandleFinalResult;
            _service.EndpointDetected -= HandleEndpoint;
        }

        private void UpdateToggleButton()
        {
            if (_toggleButton == null)
                return;

            _toggleButton.text = _isRecording
                ? "Stop Recording"
                : "Start Recording";
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void AppendTranscriptLine(string text)
        {
            if (_transcriptScroll == null ||
                string.IsNullOrWhiteSpace(text))
                return;

            var line = new Label(text);
            line.AddToClassList("stream-transcript-line");
            _transcriptScroll.contentContainer.Add(line);
        }

        private void UpdateInfo()
        {
            if (_infoLabel == null)
                return;

            if (_service == null || !_service.IsReady)
            {
                _infoLabel.text = "Engine not loaded.";
                return;
            }

            var profile = _service.ActiveProfile;
            _infoLabel.text =
                $"Profile: {profile?.profileName ?? "—"} | " +
                $"Type: {profile?.modelType}";
        }
    }
}
