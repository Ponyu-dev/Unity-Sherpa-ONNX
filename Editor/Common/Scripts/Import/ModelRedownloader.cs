using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;
using UnityEditor;

namespace PonyuDev.SherpaOnnx.Editor.Common.Import
{
    /// <summary>
    /// Re-downloads a model from its saved <c>sourceUrl</c>.
    /// Used by detail presenters when model files are missing.
    /// Shows progress via <see cref="EditorUtility.DisplayProgressBar"/>.
    /// </summary>
    internal sealed class ModelRedownloader : IDisposable
    {
        private PackageInstallPipeline _pipeline;
        private UnityWebRequestFileDownloader _downloader;
        private string _currentStatus = "";

        /// <summary>
        /// Re-downloads an archive model (TTS / ASR).
        /// Returns the destination directory for auto-filling.
        /// </summary>
        internal async Task<string> RedownloadArchiveAsync(string url, Func<string, string> getModelDir, CancellationToken ct)
        {
            string archiveName = ArchiveNameParser.GetArchiveName(url);
            string fileName = ArchiveNameParser.GetFileName(url);

            var handler = new ModelContentHandler(archiveName, getModelDir);
            _pipeline = ImportPipelineFactory.Create(handler);

            _pipeline.OnProgress01 += HandlePipelineProgress;
            _pipeline.OnStatus += HandlePipelineStatus;

            EditorUtility.DisplayProgressBar("Re-downloading model...", $"Starting {archiveName}...", 0f);

            await _pipeline.RunAsync(url, fileName, ct);
            ct.ThrowIfCancellationRequested();

            return handler.DestinationDirectory;
        }

        /// <summary>
        /// Re-downloads a single-file model (VAD).
        /// </summary>
        internal async Task RedownloadFileAsync(string url, string modelDir, CancellationToken ct)
        {
            string fileName = GetFileNameFromUrl(url);

            Directory.CreateDirectory(modelDir);

            _downloader = new UnityWebRequestFileDownloader();
            _downloader.OnProgress += HandleDownloadProgress;
            _downloader.OnStarted += HandleDownloadStarted;

            EditorUtility.DisplayProgressBar("Re-downloading model...", $"Starting {fileName}...", 0f);

            await _downloader.DownloadAsync(url, modelDir, fileName, ct);
            ct.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();

            if (_pipeline != null)
            {
                _pipeline.OnProgress01 -= HandlePipelineProgress;
                _pipeline.OnStatus -= HandlePipelineStatus;
                _pipeline.Dispose();
                _pipeline = null;
            }

            if (_downloader != null)
            {
                _downloader.OnProgress -= HandleDownloadProgress;
                _downloader.OnStarted -= HandleDownloadStarted;
                _downloader.Dispose();
                _downloader = null;
            }
        }

        // ── Pipeline handlers ──

        private void HandlePipelineProgress(float progress01)
        {
            EditorUtility.DisplayProgressBar("Re-downloading model...", _currentStatus, progress01);
        }

        private void HandlePipelineStatus(string status)
        {
            _currentStatus = status;
        }

        // ── File download handlers ──

        private void HandleDownloadProgress(string url, float progress01, ulong downloadedBytes, long totalBytes)
        {
            EditorUtility.DisplayProgressBar("Re-downloading model...", _currentStatus, progress01);
        }

        private void HandleDownloadStarted(string url, string fullPath)
        {
            _currentStatus = $"Downloading {Path.GetFileName(fullPath)}...";
        }

        // ── Helpers ──

        private static string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            string path = uri.AbsolutePath;
            int lastSlash = path.LastIndexOf('/');
            return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        }
    }
}
