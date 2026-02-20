using System;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Advanced config sample — uses
    /// <see cref="ITtsService.GenerateWithConfigAsync"/>
    /// to demonstrate <see cref="TtsGenerationConfig"/>
    /// with silence scale, num steps and progress bar.
    /// </summary>
    public sealed class TtsConfigPanel : ISamplePanel
    {
        private ITtsService _service;
        private AudioSource _audio;
        private Action _onBack;
        private VisualElement _root;

        private TextField _textField;
        private FloatField _speedField;
        private IntegerField _speakerIdField;
        private FloatField _silenceScaleField;
        private IntegerField _numStepsField;
        private Button _generateButton;
        private Button _backButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private Label _infoLabel;

        private bool _isGenerating;
        private volatile float _lastProgress;

        // ── ISamplePanel ──

        public void Bind(
            VisualElement root,
            ITtsService service,
            AudioSource audio,
            Action onBack)
        {
            _root = root;
            _service = service;
            _audio = audio;
            _onBack = onBack;

            _textField = root.Q<TextField>("textField");
            _speedField = root.Q<FloatField>("speedField");
            _speakerIdField = root.Q<IntegerField>("speakerIdField");
            _silenceScaleField = root.Q<FloatField>("silenceScaleField");
            _numStepsField = root.Q<IntegerField>("numStepsField");
            _generateButton = root.Q<Button>("generateButton");
            _backButton = root.Q<Button>("backButton");
            _progressBar = root.Q<ProgressBar>("progressBar");
            _statusLabel = root.Q<Label>("statusLabel");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_generateButton != null)
                _generateButton.clicked += HandleGenerate;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            UpdateInfo();
        }

        public void Unbind()
        {
            if (_generateButton != null)
                _generateButton.clicked -= HandleGenerate;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;

            _textField = null;
            _speedField = null;
            _speakerIdField = null;
            _silenceScaleField = null;
            _numStepsField = null;
            _generateButton = null;
            _backButton = null;
            _progressBar = null;
            _statusLabel = null;
            _infoLabel = null;
            _service = null;
            _audio = null;
            _onBack = null;
            _root = null;
        }

        // ── Handlers ──

        private async void HandleGenerate()
        {
            if (_isGenerating || _service == null || !_service.IsReady)
                return;

            string text = _textField?.value ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("Enter text first.");
                return;
            }

            var config = new TtsGenerationConfig
            {
                Speed = _speedField?.value ?? 1f,
                SpeakerId = _speakerIdField?.value ?? 0,
                SilenceScale = _silenceScaleField?.value ?? 0.2f,
                NumSteps = _numStepsField?.value ?? 5,
            };

            _isGenerating = true;
            _lastProgress = 0f;
            _generateButton?.SetEnabled(false);
            ResetProgress();
            SetStatus("Generating with config...");

            var scheduled = _root?.schedule.Execute(UpdateProgressUi)
                .Every(50);

            try
            {
                TtsCallbackProgress callback = (samples, count, progress) =>
                {
                    _lastProgress = progress;
                    return 1; // continue
                };

                var result = await _service.GenerateWithConfigAsync(
                    text, config, callback);

                UpdateProgressUi();

                if (result == null || !result.IsValid)
                {
                    SetStatus("Generation returned no audio.");
                    return;
                }

                var clip = result.ToAudioClip("tts-config");
                _audio.clip = clip;
                _audio.Play();

                SetStatus(
                    $"Playing — {result.DurationSeconds:F2}s, " +
                    $"{result.SampleRate}Hz, {result.NumSamples} samples");

                result.Dispose();
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsConfigPanel error: {ex}");
            }
            finally
            {
                scheduled?.Pause();
                _isGenerating = false;
                _generateButton?.SetEnabled(true);
            }
        }

        private void HandleBack() => _onBack?.Invoke();

        // ── Helpers ──

        private void UpdateProgressUi()
        {
            float pct = _lastProgress * 100f;

            if (_progressBar != null)
                _progressBar.value = pct;
        }

        private void ResetProgress()
        {
            if (_progressBar != null)
                _progressBar.value = 0;
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
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
