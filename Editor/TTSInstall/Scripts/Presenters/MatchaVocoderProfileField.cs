using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Builds a vocoder selector (dropdown + download button)
    /// for the Matcha profile detail panel.
    /// </summary>
    internal sealed class MatchaVocoderProfileField
    {
        private readonly TtsProfile _profile;
        private readonly TtsProjectSettings _settings;
        private readonly Action _onDownloaded;

        private PopupField<MatchaVocoderOption> _dropdown;
        private Button _downloadButton;
        private Label _statusLabel;
        private CancellationTokenSource _cts;

        internal MatchaVocoderProfileField(
            TtsProfile profile,
            TtsProjectSettings settings,
            Action onDownloaded)
        {
            _profile = profile;
            _settings = settings;
            _onDownloaded = onDownloaded;
        }

        internal VisualElement Build()
        {
            var container = new VisualElement();
            container.AddToClassList("tts-vocoder-container");

            var row = new VisualElement();
            row.AddToClassList("tts-vocoder-row");

            var choices = new List<MatchaVocoderOption>
            {
                MatchaVocoderOption.Vocos22khz,
                MatchaVocoderOption.HifiganV1,
                MatchaVocoderOption.HifiganV2,
                MatchaVocoderOption.HifiganV3
            };

            MatchaVocoderOption current = DetectCurrent();

            _dropdown = new PopupField<MatchaVocoderOption>(
                "Change vocoder", choices, current,
                MatchaVocoderOptionExtensions.GetDisplayName,
                MatchaVocoderOptionExtensions.GetDisplayName);

            _dropdown.AddToClassList("tts-vocoder-dropdown");
            row.Add(_dropdown);

            _downloadButton = new Button { text = "Download" };
            _downloadButton.AddToClassList("btn");
            _downloadButton.AddToClassList("btn-secondary");
            _downloadButton.AddToClassList("tts-vocoder-download-btn");
            _downloadButton.clicked += HandleDownloadClicked;
            row.Add(_downloadButton);

            container.Add(row);

            _statusLabel = new Label();
            _statusLabel.AddToClassList("hidden");
            container.Add(_statusLabel);

            return container;
        }

        // ── Handlers ──

        private async void HandleDownloadClicked()
        {
            if (_cts != null) return;

            string modelDir = ModelPaths.GetTtsModelDir(_profile.profileName);
            _cts = new CancellationTokenSource();
            _downloadButton.SetEnabled(false);
            SherpaOnnxLog.EditorLog("[SherpaOnnx] Vocoder download started.");

            try
            {
                SetStatus("Downloading...");

                using var downloader = new MatchaVocoderDownloader();
                downloader.OnProgress += HandleProgress;
                downloader.OnStatus += SetStatus;

                try
                {
                    string oldPath = Path.Combine(modelDir, _profile.matchaVocoder ?? "");
                    ModelFileService.DeleteFile(oldPath);

                    MatchaVocoderOption option = _dropdown.value;
                    string fileName = await downloader.DownloadAsync(
                        option, modelDir, _cts.Token);

                    _profile.matchaVocoder = fileName;
                    _settings.SaveSettings();

                    SherpaOnnxLog.EditorLog($"[SherpaOnnx] Vocoder download completed: {option.GetDisplayName()}");
                    SetStatus($"Vocoder changed to {option.GetDisplayName()}");
                    _onDownloaded?.Invoke();
                }
                finally
                {
                    downloader.OnProgress -= HandleProgress;
                    downloader.OnStatus -= SetStatus;
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Download canceled.");
                SherpaOnnxLog.EditorWarning("[SherpaOnnx] Vocoder download canceled by user.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.EditorError($"[SherpaOnnx] Vocoder download failed: {ex}");
            }
            finally
            {
                _downloadButton?.SetEnabled(true);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void HandleProgress(float progress01)
        {
            SetStatus($"Downloading... {progress01 * 100f:F0}%");
        }

        // ── Helpers ──

        private MatchaVocoderOption DetectCurrent()
        {
            string current = _profile.matchaVocoder ?? "";

            if (current.Contains("hifigan_v1")) return MatchaVocoderOption.HifiganV1;
            if (current.Contains("hifigan_v2")) return MatchaVocoderOption.HifiganV2;
            if (current.Contains("hifigan_v3")) return MatchaVocoderOption.HifiganV3;

            return MatchaVocoderOption.Vocos22khz;
        }

        private void SetStatus(string text)
        {
            if (_statusLabel == null) return;
            _statusLabel.text = text;
            _statusLabel.style.display = DisplayStyle.Flex;
        }
    }
}
