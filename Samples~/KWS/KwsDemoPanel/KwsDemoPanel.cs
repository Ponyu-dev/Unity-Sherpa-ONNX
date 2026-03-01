using System;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Kws;
using PonyuDev.SherpaOnnx.Kws.Engine;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Demo panel: toggles microphone recording, feeds audio to KWS,
    /// displays detected keywords in a scrollable log.
    /// </summary>
    public sealed class KwsDemoPanel : IKwsSamplePanel
    {
        private IKwsService _kws;
        private MicrophoneSource _mic;
        private Action _onBack;

        private Button _backButton;
        private Button _toggleButton;
        private Button _clearButton;
        private Label _keywordStateLabel;
        private Label _detectionCountLabel;
        private Label _statusLabel;
        private Label _infoLabel;
        private ScrollView _detectionScroll;

        private bool _recording;
        private int _detectionCount;

        // ── IKwsSamplePanel ──

        public void Bind(VisualElement root, IKwsService kwsService, MicrophoneSource microphone, Action onBack)
        {
            _kws = kwsService;
            _mic = microphone;
            _onBack = onBack;

            _backButton = root.Q<Button>("backButton");
            _toggleButton = root.Q<Button>("toggleButton");
            _clearButton = root.Q<Button>("clearButton");
            _keywordStateLabel = root.Q<Label>("keywordStateLabel");
            _detectionCountLabel = root.Q<Label>("detectionCountLabel");
            _statusLabel = root.Q<Label>("statusLabel");
            _infoLabel = root.Q<Label>("infoLabel");
            _detectionScroll = root.Q<ScrollView>("detectionScroll");

            if (_backButton != null) _backButton.clicked += HandleBack;
            if (_toggleButton != null) _toggleButton.clicked += HandleToggle;
            if (_clearButton != null) _clearButton.clicked += HandleClear;

            _kws.OnKeywordDetected += HandleKeyword;

            UpdateInfo();
        }

        public void Unbind()
        {
            if (_backButton != null) _backButton.clicked -= HandleBack;
            if (_toggleButton != null) _toggleButton.clicked -= HandleToggle;
            if (_clearButton != null) _clearButton.clicked -= HandleClear;

            if (_kws != null) _kws.OnKeywordDetected -= HandleKeyword;

            _kws = null;
            _mic = null;
            _onBack = null;
        }

        // ── Handlers ──

        private void HandleBack()
        {
            _onBack?.Invoke();
        }

        private async void HandleToggle()
        {
            if (_mic == null)
                return;

            if (_recording)
            {
                _mic.StopRecording();
                _recording = false;
                _toggleButton.text = "Start Recording";
                SetState("Idle", "kws-state-idle");
            }
            else
            {
                await _mic.StartRecordingAsync();
                _recording = true;
                _toggleButton.text = "Stop Recording";
                SetState("Listening...", "kws-state-listening");
            }
        }

        private void HandleClear()
        {
            _detectionScroll?.Clear();
            _detectionCount = 0;
            UpdateDetectionCount();
        }

        private void HandleKeyword(KwsResult result)
        {
            if (!result.IsValid)
                return;

            _detectionCount++;
            UpdateDetectionCount();

            string time = DateTime.Now.ToString("HH:mm:ss");
            var entry = new Label($"[{time}] {result.Keyword}");
            entry.AddToClassList("kws-detection-entry");
            _detectionScroll?.Add(entry);

            SetState($"Detected: {result.Keyword}", "kws-state-detected");
            _statusLabel.text = $"Last: {result.Keyword}";

            // Reset visual state after short delay.
            _keywordStateLabel?.schedule.Execute(() =>
            {
                if (_recording)
                    SetState("Listening...", "kws-state-listening");
            }).StartingIn(1500);
        }

        // ── Helpers ──

        private void SetState(string text, string className)
        {
            if (_keywordStateLabel == null)
                return;

            _keywordStateLabel.text = text;
            _keywordStateLabel.RemoveFromClassList("kws-state-idle");
            _keywordStateLabel.RemoveFromClassList("kws-state-listening");
            _keywordStateLabel.RemoveFromClassList("kws-state-detected");
            _keywordStateLabel.AddToClassList(className);
        }

        private void UpdateDetectionCount()
        {
            if (_detectionCountLabel != null)
                _detectionCountLabel.text = $"Detections: {_detectionCount}";
        }

        private void UpdateInfo()
        {
            if (_infoLabel == null || _kws == null)
                return;

            var profile = _kws.ActiveProfile;
            if (profile == null)
            {
                _infoLabel.text = "KWS: not loaded";
                return;
            }

            _infoLabel.text = $"KWS: {profile.profileName} | {profile.modelType}";
        }
    }
}
