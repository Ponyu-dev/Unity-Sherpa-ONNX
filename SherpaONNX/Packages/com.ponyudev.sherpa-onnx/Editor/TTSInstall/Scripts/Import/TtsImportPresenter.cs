using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Builds the import-from-URL UI and orchestrates the download → extract
    /// → detect → create profile flow.
    /// </summary>
    internal sealed class TtsImportPresenter : IDisposable
    {
        private readonly TtsProjectSettings _settings;
        private readonly Action _onImportCompleted;

        private TextField _urlField;
        private PopupField<MatchaVocoderOption> _vocoderField;
        private Toggle _int8Toggle;
        private Button _importButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        private CancellationTokenSource _cts;
        private PackageInstallPipeline _pipeline;
        private bool _isBusy;

        internal TtsImportPresenter(
            TtsProjectSettings settings,
            Action onImportCompleted)
        {
            _settings = settings;
            _onImportCompleted = onImportCompleted;
        }

        internal void Build(VisualElement parent)
        {
            var row = new VisualElement();
            row.AddToClassList("tts-import-row");

            _urlField = new TextField("Model archive URL");
            _urlField.AddToClassList("tts-import-url");
            _urlField.RegisterValueChangedCallback(HandleUrlChanged);
            row.Add(_urlField);

            _vocoderField = BuildVocoderField();
            _vocoderField.style.display = DisplayStyle.None;
            row.Add(_vocoderField);

            _int8Toggle = new Toggle("Use int8 models");
            _int8Toggle.style.display = DisplayStyle.None;
            _int8Toggle.style.flexShrink = 0;
            row.Add(_int8Toggle);

            _importButton = new Button { text = "Import" };
            _importButton.AddToClassList("btn");
            _importButton.AddToClassList("btn-primary");
            _importButton.style.flexShrink = 0;
            _importButton.clicked += HandleImportClicked;
            row.Add(_importButton);

            _cancelButton = new Button { text = "Cancel" };
            _cancelButton.AddToClassList("btn");
            _cancelButton.AddToClassList("btn-secondary");
            _cancelButton.style.flexShrink = 0;
            _cancelButton.clicked += HandleCancelClicked;
            _cancelButton.style.display = DisplayStyle.None;
            row.Add(_cancelButton);

            parent.Add(row);

            _progressBar = new ProgressBar { title = "" };
            _progressBar.AddToClassList("tts-import-progress");
            _progressBar.style.display = DisplayStyle.None;
            parent.Add(_progressBar);

            _statusLabel = new Label();
            _statusLabel.AddToClassList("tts-import-status");
            _statusLabel.style.display = DisplayStyle.None;
            parent.Add(_statusLabel);
        }

        public void Dispose()
        {
            CancelIfBusy();

            if (_importButton != null)
                _importButton.clicked -= HandleImportClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancelClicked;
            if (_urlField != null)
                _urlField.UnregisterValueChangedCallback(HandleUrlChanged);

            _importButton = null;
            _cancelButton = null;
            _urlField = null;
            _vocoderField = null;
            _int8Toggle = null;
            _progressBar = null;
            _statusLabel = null;
        }

        // ── Handlers ──

        private async void HandleImportClicked()
        {
            string url = _urlField?.value?.Trim();

            if (string.IsNullOrEmpty(url))
            {
                SetStatus("Please enter a URL.");
                return;
            }

            if (_isBusy)
                return;

            _cts = new CancellationTokenSource();
            SetBusy(true);

            try
            {
                await ImportAsync(url, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Import canceled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                Debug.LogError($"[SherpaOnnx] TTS import failed: {ex}");
            }
            finally
            {
                DisposePipeline();
                SetBusy(false);
            }
        }

        private void HandleCancelClicked()
        {
            CancelIfBusy();
        }

        private void HandleUrlChanged(ChangeEvent<string> evt)
        {
            string archiveName = ArchiveNameParser.GetArchiveName(evt.newValue ?? "");
            TtsModelType? detected = TtsModelTypeDetector.Detect(archiveName);

            if (_vocoderField != null)
                _vocoderField.style.display = detected == TtsModelType.Matcha
                    ? DisplayStyle.Flex : DisplayStyle.None;

            if (_int8Toggle != null)
                _int8Toggle.style.display = detected == TtsModelType.ZipVoice
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandlePipelineProgress(float progress01)
        {
            if (_progressBar == null) return;
            _progressBar.value = progress01 * 100f;
        }

        private void HandlePipelineStatus(string status)
        {
            SetStatus(status);
        }

        private void HandlePipelineError(string error)
        {
            SetStatus($"Error: {error}");
        }

        // ── Import flow ──

        private async Task ImportAsync(string url, CancellationToken ct)
        {
            string archiveName = ArchiveNameParser.GetArchiveName(url);
            string fileName = ArchiveNameParser.GetFileName(url);

            SetStatus($"Starting import of {archiveName}...");

            var handler = new TtsModelContentHandler(archiveName);
            _pipeline = TtsImportPipelineFactory.Create(handler);

            _pipeline.OnProgress01 += HandlePipelineProgress;
            _pipeline.OnStatus += HandlePipelineStatus;
            _pipeline.OnError += HandlePipelineError;

            await _pipeline.RunAsync(url, fileName, ct);
            ct.ThrowIfCancellationRequested();

            TtsModelType? detectedType = TtsModelTypeDetector.Detect(archiveName);

            var profile = new TtsProfile
            {
                profileName = archiveName,
                modelSource = TtsModelSource.Local
            };

            if (detectedType.HasValue)
                profile.modelType = detectedType.Value;

            bool useInt8 = _int8Toggle != null && _int8Toggle.value;
            TtsProfileAutoFiller.Fill(profile, handler.DestinationDirectory, useInt8);

            if (detectedType == TtsModelType.Matcha)
                await DownloadMatchaVocoderAsync(profile, handler.DestinationDirectory, ct);

            _settings.data.profiles.Add(profile);
            _settings.SaveSettings();

            AssetDatabase.Refresh();

            string typeLabel = detectedType.HasValue
                ? detectedType.Value.ToString()
                : "Unknown";

            SetStatus($"Import complete: {archiveName} ({typeLabel})");

            if (_urlField != null)
                _urlField.value = "";

            _onImportCompleted?.Invoke();
        }

        // ── Matcha vocoder ──

        private async Task DownloadMatchaVocoderAsync(
            TtsProfile profile, string modelDir, CancellationToken ct)
        {
            MatchaVocoderOption option = _vocoderField != null
                ? _vocoderField.value
                : MatchaVocoderOption.Vocos22khz;

            using (var downloader = new MatchaVocoderDownloader())
            {
                downloader.OnProgress += HandlePipelineProgress;
                downloader.OnStatus += HandlePipelineStatus;

                string fileName = await downloader.DownloadAsync(option, modelDir, ct);
                profile.matchaVocoder = fileName;
            }
        }

        private static PopupField<MatchaVocoderOption> BuildVocoderField()
        {
            var choices = new List<MatchaVocoderOption>
            {
                MatchaVocoderOption.Vocos22khz,
                MatchaVocoderOption.HifiganV1,
                MatchaVocoderOption.HifiganV2,
                MatchaVocoderOption.HifiganV3
            };

            return new PopupField<MatchaVocoderOption>(
                "Vocoder", choices, MatchaVocoderOption.Vocos22khz,
                MatchaVocoderOptionExtensions.GetDisplayName,
                MatchaVocoderOptionExtensions.GetDisplayName);
        }

        // ── Helpers ──

        private void SetStatus(string text)
        {
            if (_statusLabel == null) return;
            _statusLabel.text = text;
            _statusLabel.style.display = DisplayStyle.Flex;
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;

            if (_importButton != null)
                _importButton.SetEnabled(!busy);
            if (_urlField != null)
                _urlField.SetEnabled(!busy);
            if (_cancelButton != null)
                _cancelButton.style.display = busy
                    ? DisplayStyle.Flex : DisplayStyle.None;
            if (_progressBar != null)
            {
                _progressBar.style.display = busy
                    ? DisplayStyle.Flex : DisplayStyle.None;
                _progressBar.value = 0f;
            }
        }

        private void CancelIfBusy()
        {
            if (_cts == null) return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void DisposePipeline()
        {
            if (_pipeline == null) return;

            _pipeline.OnProgress01 -= HandlePipelineProgress;
            _pipeline.OnStatus -= HandlePipelineStatus;
            _pipeline.OnError -= HandlePipelineError;
            _pipeline.Dispose();
            _pipeline = null;
        }
    }
}
