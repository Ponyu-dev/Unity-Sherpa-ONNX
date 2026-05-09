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
    public sealed class AsrStreamPanel : IDemoView
    {
        private IOnlineAsrService _service;
        private MicrophoneSource _microphone;
        private IDemoNavigator _nav;

        private Button _toggleButton;
        private Button _clearButton;
        private Button _backButton;
        private Label _partialLabel;
        private Label _levelLabel;
        private ScrollView _transcriptScroll;
        private Label _statusLabel;
        private Label _infoLabel;

        private bool _isRecording;

        // ── IDemoView ──

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _service = services?.OnlineAsr;
            _microphone = services?.Microphone;
            _nav = nav;

            _toggleButton = root.Q<Button>("toggleButton");
            _clearButton = root.Q<Button>("clearButton");
            _backButton = root.Q<Button>("backButton");
            _partialLabel = root.Q<Label>("partialLabel");
            _levelLabel = root.Q<Label>("levelLabel");
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

            AsrInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            AsrInitProgressBus.Changed -= HandleInitProgressChanged;
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
            _levelLabel = null;
            _transcriptScroll = null;
            _statusLabel = null;
            _infoLabel = null;
            _service = null;
            _microphone = null;
            _nav = null;
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
            if (_partialLabel != null)
                _partialLabel.text = string.Empty;
            _toggleButton?.SetEnabled(false);

            try
            {
                // Start the session and subscribe BEFORE the mic actually
                // produces samples. The previous order (mic first, then
                // subscribe) dropped the first audio frames on the floor —
                // partial results then started "from the middle" of the
                // user's first utterance.
                _service.StartSession();
                _microphone.SamplesAvailable += HandleMicSamples;

                bool started = await _microphone.StartRecordingAsync();
                if (!started)
                {
                    _microphone.SamplesAvailable -= HandleMicSamples;
                    _service.StopSession();
                    SetStatus("Microphone failed to start.");
                    return;
                }

                _isRecording = true;
                UpdateToggleButton();
                SetStatus("Listening — speak now.");
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
            _nav?.Back();
        }

        // ── Microphone ──

        private void HandleMicSamples(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return;

            UpdateLevel(samples);

            if (_service == null || !_service.IsSessionActive)
                return;

            _service.AcceptSamples(samples, _microphone.SampleRate);
            _service.ProcessAvailableFrames();
        }

        private void UpdateLevel(float[] samples)
        {
            if (_levelLabel == null) return;

            float maxAbs = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = samples[i] < 0f ? -samples[i] : samples[i];
                if (abs > maxAbs) maxAbs = abs;
            }
            _levelLabel.text = $"Mic level: {maxAbs:F3}";
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
                _microphone.StopRecording();
            }

            _service?.StopSession();
            _isRecording = false;

            if (_levelLabel != null)
                _levelLabel.text = "Mic level: —";
            if (_partialLabel != null)
                _partialLabel.text = string.Empty;

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

        private void HandleInitProgressChanged()
        {
            SetStatus(AsrSampleStatusUtil.BuildOnlineCurrent(_service));
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
