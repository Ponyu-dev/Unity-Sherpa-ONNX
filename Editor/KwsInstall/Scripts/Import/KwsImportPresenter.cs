using System;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Import
{
    /// <summary>
    /// Builds the import-from-URL UI and orchestrates the download → extract
    /// → detect → create profile flow for KWS models.
    /// KWS models are archives with encoder/decoder/joiner/tokens/keywords.
    /// </summary>
    internal sealed class KwsImportPresenter : IDisposable
    {
        private readonly KwsProjectSettings _settings;
        private readonly Action _onImportCompleted;

        private TextField _urlField;
        private Button _importButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        private CancellationTokenSource _cts;
        private PackageInstallPipeline _pipeline;
        private bool _isBusy;

        internal KwsImportPresenter(KwsProjectSettings settings, Action onImportCompleted)
        {
            _settings = settings;
            _onImportCompleted = onImportCompleted;
        }

        internal void Build(VisualElement parent)
        {
            _urlField = parent.Q<TextField>("importUrlField");

            _importButton = parent.Q<Button>("importButton");
            _importButton.clicked += HandleImportClicked;

            _cancelButton = parent.Q<Button>("importCancelButton");
            _cancelButton.clicked += HandleCancelClicked;

            _progressBar = parent.Q<ProgressBar>("importProgressBar");
            _statusLabel = parent.Q<Label>("importStatusLabel");
        }

        public void Dispose()
        {
            CancelIfBusy();

            if (_importButton != null)
                _importButton.clicked -= HandleImportClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancelClicked;

            _importButton = null;
            _cancelButton = null;
            _urlField = null;
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

            if (_isBusy)
                return;

            _cts = new CancellationTokenSource();
            SetBusy(true);
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] KWS import started: {url}");

            try
            {
                await ImportAsync(url, _cts.Token);
                SherpaOnnxLog.EditorLog("[SherpaOnnx] KWS import completed.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Import canceled.");
                SherpaOnnxLog.EditorWarning("[SherpaOnnx] KWS import canceled by user.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
                SherpaOnnxLog.EditorError($"[SherpaOnnx] KWS import failed: {ex}");
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

            var handler = new ModelContentHandler(archiveName, ModelPaths.GetKwsModelDir);
            _pipeline = ImportPipelineFactory.Create(handler);

            _pipeline.OnProgress01 += HandlePipelineProgress;
            _pipeline.OnStatus += HandlePipelineStatus;
            _pipeline.OnError += HandlePipelineError;

            await _pipeline.RunAsync(url, fileName, ct);
            ct.ThrowIfCancellationRequested();

            KwsModelType? detectedType = KwsModelTypeDetector.Detect(archiveName);

            var profile = new KwsProfile
            {
                profileName = archiveName,
                sourceUrl = url,
                modelSource = ModelSource.Local
            };

            if (detectedType.HasValue)
                profile.modelType = detectedType.Value;

            KwsProfileAutoFiller.Fill(profile, handler.DestinationDirectory);

            _settings.data.profiles.Add(profile);
            _settings.SaveSettings();

            AssetDatabase.Refresh();

            string typeLabel = detectedType.HasValue ? detectedType.Value.ToString() : "Unknown";

            SetStatus($"Import complete: {archiveName} ({typeLabel})");

            if (_urlField != null)
                _urlField.value = "";

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
