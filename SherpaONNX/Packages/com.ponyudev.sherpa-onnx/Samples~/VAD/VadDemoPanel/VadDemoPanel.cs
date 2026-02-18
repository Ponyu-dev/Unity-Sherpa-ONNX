using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// VAD + ASR demo panel — microphone capture with voice activity
    /// detection. Speech segments are sent to offline ASR. Shows
    /// speech state indicator, segment count, and recognition results.
    /// </summary>
    public sealed class VadDemoPanel : IVadSamplePanel
    {
        private VadAsrPipeline _pipeline;
        private MicrophoneSource _microphone;
        private Action _onBack;

        private Button _toggleButton;
        private Button _clearButton;
        private Button _backButton;
        private Label _speechStateLabel;
        private Label _segmentCountLabel;
        private ScrollView _transcriptScroll;
        private Label _statusLabel;
        private Label _infoLabel;

        private bool _isRecording;
        private int _segmentCount;

        // ── IVadSamplePanel ──

        public void Bind(
            VisualElement root,
            IVadService vadService,
            IAsrService asrService,
            VadAsrPipeline pipeline,
            MicrophoneSource microphone,
            Action onBack)
        {
            _pipeline = pipeline;
            _microphone = microphone;
            _onBack = onBack;

            _toggleButton = root.Q<Button>("toggleButton");
            _clearButton = root.Q<Button>("clearButton");
            _backButton = root.Q<Button>("backButton");
            _speechStateLabel = root.Q<Label>("speechStateLabel");
            _segmentCountLabel = root.Q<Label>("segmentCountLabel");
            _transcriptScroll = root.Q<ScrollView>("transcriptScroll");
            _statusLabel = root.Q<Label>("statusLabel");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_toggleButton != null)
                _toggleButton.clicked += HandleToggle;
            if (_clearButton != null)
                _clearButton.clicked += HandleClear;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            SubscribePipeline();
            UpdateInfo(vadService);
        }

        public void Unbind()
        {
            StopRecordingIfActive();
            UnsubscribePipeline();

            if (_toggleButton != null)
                _toggleButton.clicked -= HandleToggle;
            if (_clearButton != null)
                _clearButton.clicked -= HandleClear;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;

            _toggleButton = null;
            _clearButton = null;
            _backButton = null;
            _speechStateLabel = null;
            _segmentCountLabel = null;
            _transcriptScroll = null;
            _statusLabel = null;
            _infoLabel = null;
            _pipeline = null;
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

            if (_pipeline == null || !_pipeline.IsReady)
            {
                SetStatus("Pipeline not ready. " +
                    "Check VAD and ASR profiles.");
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
                bool started = await _microphone
                    .StartRecordingAsync();
                if (!started)
                {
                    SetStatus("Microphone failed to start.");
                    return;
                }

                _microphone.SamplesAvailable += HandleMicSamples;
                _microphone.SilenceDetected +=
                    HandleSilenceDetected;
                _isRecording = true;
                _segmentCount = 0;

                UpdateToggleButton();
                UpdateSpeechState(false);
                UpdateSegmentCount();
                SetStatus("Recording — speak into the mic.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] VadDemoPanel start error: " +
                    ex);
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

            _segmentCount = 0;
            UpdateSegmentCount();
        }

        private void HandleBack()
        {
            StopRecordingIfActive();
            _onBack?.Invoke();
        }

        // ── Microphone ──

        private void HandleMicSamples(float[] samples)
        {
            _pipeline?.AcceptSamples(samples);
        }

        private void HandleSilenceDetected(string diagnosis)
        {
            StopRecordingIfActive();

            SetStatus(
                "Microphone silence detected \u2014 " +
                "voice capture is not available on " +
                "this device.");

            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] VadDemoPanel: " +
                "silence detected, recording stopped. " +
                "Diag: " + diagnosis);
        }

        // ── Pipeline events ──

        private void HandleSpeechStart()
        {
            UpdateSpeechState(true);
        }

        private void HandleSpeechEnd()
        {
            UpdateSpeechState(false);
        }

        private void HandleResult(AsrResult result)
        {
            _segmentCount++;
            UpdateSegmentCount();
            AppendTranscriptLine(result.Text);
        }

        // ── Helpers ──

        private void StopRecordingIfActive()
        {
            if (!_isRecording)
                return;

            if (_microphone != null)
            {
                _microphone.SamplesAvailable -= HandleMicSamples;
                _microphone.SilenceDetected -=
                    HandleSilenceDetected;
                _microphone.StopRecording();
            }

            _pipeline?.Flush();
            _isRecording = false;

            UpdateToggleButton();
            UpdateSpeechState(false);
            SetStatus("Stopped.");
        }

        private void SubscribePipeline()
        {
            if (_pipeline == null)
                return;

            _pipeline.OnSpeechStart += HandleSpeechStart;
            _pipeline.OnSpeechEnd += HandleSpeechEnd;
            _pipeline.OnResult += HandleResult;
        }

        private void UnsubscribePipeline()
        {
            if (_pipeline == null)
                return;

            _pipeline.OnSpeechStart -= HandleSpeechStart;
            _pipeline.OnSpeechEnd -= HandleSpeechEnd;
            _pipeline.OnResult -= HandleResult;
        }

        private void UpdateToggleButton()
        {
            if (_toggleButton == null)
                return;

            _toggleButton.text = _isRecording
                ? "Stop Recording"
                : "Start Recording";
        }

        private void UpdateSpeechState(bool isSpeaking)
        {
            if (_speechStateLabel == null)
                return;

            _speechStateLabel.text = isSpeaking
                ? "Speaking..."
                : "Silence";

            _speechStateLabel.RemoveFromClassList(
                "vad-speech-active");
            _speechStateLabel.RemoveFromClassList(
                "vad-speech-inactive");
            _speechStateLabel.AddToClassList(
                isSpeaking
                    ? "vad-speech-active"
                    : "vad-speech-inactive");
        }

        private void UpdateSegmentCount()
        {
            if (_segmentCountLabel != null)
                _segmentCountLabel.text =
                    $"Segments: {_segmentCount}";
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
            line.AddToClassList("vad-transcript-line");
            _transcriptScroll.contentContainer.Add(line);
        }

        private void UpdateInfo(IVadService vadService)
        {
            if (_infoLabel == null)
                return;

            if (vadService == null || !vadService.IsReady)
            {
                _infoLabel.text = "VAD engine not loaded.";
                return;
            }

            var profile = vadService.ActiveProfile;
            _infoLabel.text =
                $"VAD: {profile?.profileName ?? "\u2014"} | " +
                $"Window: {vadService.WindowSize} samples";
        }
    }
}
