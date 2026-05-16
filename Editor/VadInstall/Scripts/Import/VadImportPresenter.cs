using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Networking;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Import
{
    /// <summary>
    /// Builds the import-from-URL UI and orchestrates the download
    /// → create profile flow for VAD models.
    /// VAD models are single .onnx files (not archives),
    /// so we download directly without extraction.
    /// </summary>
    internal sealed class VadImportPresenter : IDisposable
    {
        private readonly VadProjectSettings _settings;
        private readonly Action _onImportCompleted;

        private TextField _urlField;
        private Button _importButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        private CancellationTokenSource _cts;
        private UnityWebRequestFileDownloader _downloader;
        private bool _isBusy;

        internal VadImportPresenter(VadProjectSettings settings, Action onImportCompleted)
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
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] VAD import started: {url}");

            try
            {
                await ImportAsync(url, _cts.Token);
                SherpaOnnxLog.EditorLog("[SherpaOnnx] VAD import completed.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Import canceled.");
                SherpaOnnxLog.EditorWarning("[SherpaOnnx] VAD import canceled by user.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
                SherpaOnnxLog.EditorError($"[SherpaOnnx] VAD import failed: {ex}");
            }
            finally
            {
                DisposeDownloader();
                SetBusy(false);
            }
        }

        private void HandleCancelClicked()
        {
            CancelIfBusy();
        }

        private void HandleDownloadProgress(string url, float progress01, ulong downloadedBytes, long totalBytes)
        {
            if (_progressBar == null) return;
            _progressBar.value = progress01 * 100f;
        }

        private void HandleDownloadStarted(string url, string fullPath)
        {
            SetStatus($"Downloading {Path.GetFileName(fullPath)}...");
        }

        private void HandleDownloadError(string url, string message)
        {
            SetStatus($"Error: {message}", true);
        }

        // ── Import flow ──

        private async Task ImportAsync(string url, CancellationToken ct)
        {
            string fileName = GetFileNameFromUrl(url);
            string profileName = Path.GetFileNameWithoutExtension(fileName);

            SetStatus($"Downloading {fileName}...");

            string modelDir = ModelPaths.GetVadModelDir(profileName);
            Directory.CreateDirectory(modelDir);

            _downloader = new UnityWebRequestFileDownloader();
            _downloader.OnStarted += HandleDownloadStarted;
            _downloader.OnProgress += HandleDownloadProgress;
            _downloader.OnError += HandleDownloadError;

            await _downloader.DownloadAsync(url, modelDir, fileName, ct);
            ct.ThrowIfCancellationRequested();

            VadModelType? detectedType = VadModelTypeDetector.Detect(profileName);

            var profile = new VadProfile
            {
                profileName = profileName,
                sourceUrl = url,
                modelSource = ModelSource.Local,
                model = fileName
            };

            if (detectedType.HasValue)
            {
                profile.modelType = detectedType.Value;
                AdjustWindowSizeForModelType(profile);
            }

            _settings.data.profiles.Add(profile);
            _settings.SaveSettings();

            AssetDatabase.Refresh();

            string typeLabel = detectedType.HasValue ? detectedType.Value.ToString() : "Unknown";

            SetStatus($"Import complete: {profileName} ({typeLabel})");

            if (_urlField != null)
                _urlField.value = "";

            _onImportCompleted?.Invoke();
        }

        // ── Helpers ──

        private static string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            string path = uri.AbsolutePath;
            int lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        }

        private static void AdjustWindowSizeForModelType(VadProfile profile)
        {
            switch (profile.modelType)
            {
                case VadModelType.SileroVad:
                    profile.windowSize = 512;
                    break;
                case VadModelType.TenVad:
                    profile.windowSize = 256;
                    break;
            }
        }

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

        private void DisposeDownloader()
        {
            if (_downloader == null) return;

            _downloader.OnStarted -= HandleDownloadStarted;
            _downloader.OnProgress -= HandleDownloadProgress;
            _downloader.OnError -= HandleDownloadError;
            _downloader.Dispose();
            _downloader = null;
        }
    }
}
