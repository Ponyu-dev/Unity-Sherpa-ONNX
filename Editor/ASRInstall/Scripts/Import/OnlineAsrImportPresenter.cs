using System;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Builds the online ASR import-from-URL UI and orchestrates
    /// the download → extract → detect → create profile flow.
    /// </summary>
    internal sealed class OnlineAsrImportPresenter : IDisposable
    {
        private readonly AsrProjectSettings _settings;
        private readonly Action _onImportCompleted;

        private TextField _urlField;
        private VisualElement _optionsRow;
        private Toggle _int8Toggle;
        private Button _importButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        private CancellationTokenSource _cts;
        private PackageInstallPipeline _pipeline;
        private bool _isBusy;

        internal OnlineAsrImportPresenter(
            AsrProjectSettings settings, Action onImportCompleted)
        {
            _settings = settings;
            _onImportCompleted = onImportCompleted;
        }

        internal void Build(VisualElement parent)
        {
            _urlField = parent.Q<TextField>("onlineImportUrlField");
            _urlField.RegisterValueChangedCallback(HandleUrlChanged);

            _optionsRow = parent.Q<VisualElement>("onlineImportOptionsRow");
            _int8Toggle = parent.Q<Toggle>("onlineImportInt8Toggle");

            _importButton = parent.Q<Button>("onlineImportButton");
            _importButton.clicked += HandleImportClicked;

            _cancelButton = parent.Q<Button>("onlineImportCancelButton");
            _cancelButton.clicked += HandleCancelClicked;

            _progressBar = parent.Q<ProgressBar>("onlineImportProgressBar");
            _statusLabel = parent.Q<Label>("onlineImportStatusLabel");
        }

        public void Dispose()
        {
            CancelIfBusy();

            if (_importButton != null)
                _importButton.clicked -= HandleImportClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancelClicked;
            _urlField?.UnregisterValueChangedCallback(HandleUrlChanged);

            _importButton = null;
            _cancelButton = null;
            _urlField = null;
            _optionsRow = null;
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

            string urlError = UrlValidator.Validate(url);
            if (urlError != null)
            {
                SetStatus(urlError, true);
                return;
            }

            if (_isBusy) return;

            _cts = new CancellationTokenSource();
            SetBusy(true);
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Online ASR import started: {url}");

            try
            {
                await ImportAsync(url, _cts.Token);
                SherpaOnnxLog.EditorLog("[SherpaOnnx] Online ASR import completed.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Import canceled.");
                SherpaOnnxLog.EditorWarning("[SherpaOnnx] Online ASR import canceled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
                SherpaOnnxLog.EditorError($"[SherpaOnnx] Online ASR import failed: {ex}");
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
            string url = evt.newValue?.Trim() ?? "";
            bool hasUrl = !string.IsNullOrEmpty(url);

            if (_optionsRow != null)
                _optionsRow.style.display = hasUrl
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
            SetStatus($"Error: {error}", true);
        }

        // ── Import flow ──

        private async Task ImportAsync(string url, CancellationToken ct)
        {
            string archiveName = ArchiveNameParser.GetArchiveName(url);
            string fileName = ArchiveNameParser.GetFileName(url);

            SetStatus($"Starting import of {archiveName}...");

            var handler = new ModelContentHandler(archiveName, AsrModelPaths.GetModelDir);
            _pipeline = ImportPipelineFactory.Create(handler);

            _pipeline.OnProgress01 += HandlePipelineProgress;
            _pipeline.OnStatus += HandlePipelineStatus;
            _pipeline.OnError += HandlePipelineError;

            await _pipeline.RunAsync(url, fileName, ct);
            ct.ThrowIfCancellationRequested();

            OnlineAsrModelType? detected = OnlineAsrModelTypeDetector.Detect(archiveName) ?? OnlineAsrModelTypeDetector.DetectFromFiles(handler.DestinationDirectory);

            var profile = new OnlineAsrProfile
            {
                profileName = archiveName
            };

            if (detected.HasValue)
                profile.modelType = detected.Value;

            bool useInt8 = _int8Toggle != null && _int8Toggle.value;
            OnlineAsrProfileAutoFiller.Fill(profile, handler.DestinationDirectory, useInt8);

            _settings.onlineData.profiles.Add(profile);
            _settings.SaveSettings();

            AssetDatabase.Refresh();

            string typeLabel = detected.HasValue
                ? detected.Value.ToString() : "Unknown";
            SetStatus($"Import complete: {archiveName} ({typeLabel})");

            if (_urlField != null) _urlField.value = "";
            _onImportCompleted?.Invoke();
        }

        // ── Helpers ──

        private const string ErrorClass = "model-import-status--error";

        private void SetStatus(string text, bool isError = false)
        {
            if (_statusLabel == null) return;
            _statusLabel.text = text;
            _statusLabel.style.display = DisplayStyle.Flex;

            if (isError)
                _statusLabel.AddToClassList(ErrorClass);
            else
                _statusLabel.RemoveFromClassList(ErrorClass);
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            _importButton?.SetEnabled(!busy);
            _urlField?.SetEnabled(!busy);
            if (_cancelButton != null)
                _cancelButton.style.display = busy
                    ? DisplayStyle.Flex : DisplayStyle.None;
            if (_progressBar == null) return;
            _progressBar.style.display = busy
                ? DisplayStyle.Flex : DisplayStyle.None;
            _progressBar.value = 0f;
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
