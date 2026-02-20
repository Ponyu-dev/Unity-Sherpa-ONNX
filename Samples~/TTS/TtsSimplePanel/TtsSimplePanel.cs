using System;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Simple TTS sample — uses <see cref="ITtsService.GenerateAsync"/>.
    /// Enter text, pick speed and speaker, play the result.
    /// </summary>
    public sealed class TtsSimplePanel : ISamplePanel
    {
        private ITtsService _service;
        private AudioSource _audio;
        private Action _onBack;

        private TextField _textField;
        private FloatField _speedField;
        private IntegerField _speakerIdField;
        private Button _generateButton;
        private Button _backButton;
        private Label _statusLabel;
        private Label _infoLabel;

        private bool _isGenerating;

        // ── ISamplePanel ──

        public void Bind(
            VisualElement root,
            ITtsService service,
            AudioSource audio,
            Action onBack)
        {
            _service = service;
            _audio = audio;
            _onBack = onBack;

            _textField = root.Q<TextField>("textField");
            _speedField = root.Q<FloatField>("speedField");
            _speakerIdField = root.Q<IntegerField>("speakerIdField");
            _generateButton = root.Q<Button>("generateButton");
            _backButton = root.Q<Button>("backButton");
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
            _generateButton = null;
            _backButton = null;
            _statusLabel = null;
            _infoLabel = null;
            _service = null;
            _audio = null;
            _onBack = null;
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

            float speed = _speedField?.value ?? 1f;
            int speakerId = _speakerIdField?.value ?? 0;

            _isGenerating = true;
            _generateButton?.SetEnabled(false);
            SetStatus("Generating...");

            try
            {
                var result = await _service.GenerateAsync(
                    text, speed, speakerId);

                if (result == null || !result.IsValid)
                {
                    SetStatus("Generation returned no audio.");
                    return;
                }

                var clip = result.ToAudioClip("tts-simple");
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
                    $"[SherpaOnnx] TtsSimplePanel error: {ex}");
            }
            finally
            {
                _isGenerating = false;
                _generateButton?.SetEnabled(true);
            }
        }

        private void HandleBack() => _onBack?.Invoke();

        // ── Helpers ──

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
