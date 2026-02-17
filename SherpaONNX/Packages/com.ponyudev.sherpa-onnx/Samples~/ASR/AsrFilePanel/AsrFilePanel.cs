using System;
using System.Diagnostics;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// File recognition sample — uses <see cref="IAsrService.RecognizeAsync"/>.
    /// Extracts PCM from a pre-loaded AudioClip and shows the text result.
    /// </summary>
    public sealed class AsrFilePanel : IAsrSamplePanel
    {
        private IAsrService _service;
        private AudioClip _clip;
        private Action _onBack;

        private Button _recognizeButton;
        private Button _backButton;
        private Label _resultLabel;
        private Label _statusLabel;
        private Label _infoLabel;
        private Label _timingLabel;

        private bool _isRecognizing;

        // ── IAsrSamplePanel ──

        public void Bind(
            VisualElement root,
            IAsrService offlineService,
            IOnlineAsrService onlineService,
            MicrophoneSource microphone,
            AudioClip sampleClip,
            Action onBack)
        {
            _service = offlineService;
            _clip = sampleClip;
            _onBack = onBack;

            _recognizeButton = root.Q<Button>("recognizeButton");
            _backButton = root.Q<Button>("backButton");
            _resultLabel = root.Q<Label>("resultLabel");
            _statusLabel = root.Q<Label>("statusLabel");
            _infoLabel = root.Q<Label>("infoLabel");
            _timingLabel = root.Q<Label>("timingLabel");

            if (_recognizeButton != null)
                _recognizeButton.clicked += HandleRecognize;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            UpdateInfo();
        }

        public void Unbind()
        {
            if (_recognizeButton != null)
                _recognizeButton.clicked -= HandleRecognize;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;

            _recognizeButton = null;
            _backButton = null;
            _resultLabel = null;
            _statusLabel = null;
            _infoLabel = null;
            _timingLabel = null;
            _service = null;
            _clip = null;
            _onBack = null;
        }

        // ── Handlers ──

        private async void HandleRecognize()
        {
            if (_isRecognizing)
                return;

            if (_service == null || !_service.IsReady)
            {
                SetStatus("Engine not loaded.");
                return;
            }

            if (_clip == null)
            {
                SetStatus("No AudioClip assigned.");
                return;
            }

            _isRecognizing = true;
            _recognizeButton?.SetEnabled(false);
            SetStatus("Recognizing...");
            SetTiming("");
            SetResult("");

            try
            {
                float[] samples = new float[_clip.samples * _clip.channels];
                _clip.GetData(samples, 0);

                var sw = Stopwatch.StartNew();
                AsrResult result = await _service.RecognizeAsync(
                    samples, _clip.frequency);
                sw.Stop();

                if (result == null || !result.IsValid)
                {
                    SetStatus("Recognition returned no text.");
                    return;
                }

                SetResult(result.Text);
                SetTiming($"Time: {sw.ElapsedMilliseconds} ms");
                SetStatus("Done.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrFilePanel error: {ex}");
            }
            finally
            {
                _isRecognizing = false;
                _recognizeButton?.SetEnabled(true);
            }
        }

        private void HandleBack()
        {
            _onBack?.Invoke();
        }

        // ── Helpers ──

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void SetResult(string text)
        {
            if (_resultLabel != null)
                _resultLabel.text = text;
        }

        private void SetTiming(string text)
        {
            if (_timingLabel != null)
                _timingLabel.text = text;
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
            string clipInfo = _clip != null
                ? $"{_clip.length:F1}s, {_clip.frequency}Hz"
                : "no clip";

            _infoLabel.text =
                $"Profile: {profile?.profileName ?? "—"} | " +
                $"Clip: {clipInfo}";
        }
    }
}
