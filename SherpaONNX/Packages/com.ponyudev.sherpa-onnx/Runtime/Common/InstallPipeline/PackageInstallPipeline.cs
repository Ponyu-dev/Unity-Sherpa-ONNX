using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.Networking;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    public sealed class PackageInstallPipeline : IPackageInstallPipeline
    {
        public event Action<PipelineStage> OnStageChanged;
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action<string> OnError;
        public event Action OnCompleted;

        private readonly IFileDownloader _downloader;
        private readonly IArchiveExtractor _extractor;
        private readonly IExtractedContentHandler _contentHandler;
        private readonly ITempDirectoryFactory _tempFactory;

        private ITempDirectory _downloadTemp;
        private ITempDirectory _extractTemp;

        private bool _disposed;

        private PipelineStage _stage;
        private float _downloadProgress01;
        private float _extractProgress01;
        private float _handleProgress01;

        // weights for overall progress
        private const float DownloadWeight = 0.45f;
        private const float ExtractWeight = 0.35f;
        private const float HandleWeight = 0.20f;

        public PackageInstallPipeline(
            IFileDownloader downloader,
            IArchiveExtractor extractor,
            IExtractedContentHandler contentHandler,
            ITempDirectoryFactory tempFactory)
        {
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            _contentHandler = contentHandler ?? throw new ArgumentNullException(nameof(contentHandler));
            _tempFactory = tempFactory ?? throw new ArgumentNullException(nameof(tempFactory));
        }

        public async Task RunAsync(string url, string fileName, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PackageInstallPipeline));

            try
            {
                ChangeStage(PipelineStage.Preparing);
                ResetProgress();

                // Base temp root: works in Editor and Runtime
                string tempRoot = Application.temporaryCachePath;
                //TODO Думаю надо передавать _downloadTemp и _extractTemp. Что бы потом была возможность удалить временные папки.
                _downloadTemp = _tempFactory.Create(tempRoot, "Pkg_Download");
                _extractTemp = _tempFactory.Create(tempRoot, "Pkg_Extract");

                SubscribeAll();

                ChangeStage(PipelineStage.Downloading);

                string downloadedPath = Path.Combine(_downloadTemp.Path, fileName);
                await _downloader.DownloadAsync(url, _downloadTemp.Path, fileName, cancellationToken);

                ChangeStage(PipelineStage.Extracting);
                await _extractor.ExtractAsync(downloadedPath, _extractTemp.Path, cancellationToken);

                ChangeStage(PipelineStage.HandlingContent);
                await _contentHandler.HandleAsync(_extractTemp.Path, cancellationToken);

                ChangeStage(PipelineStage.CleaningUp);
                CleanupTemps();

                ChangeStage(PipelineStage.Completed);
                OnCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Fail("Pipeline canceled.");
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
            finally
            {
                UnsubscribeAll();
                DisposeTemps();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            UnsubscribeAll();
            DisposeTemps();

            OnStageChanged = null;
            OnStatus = null;
            OnProgress01 = null;
            OnError = null;
            OnCompleted = null;
        }

        private void SubscribeAll()
        {
            _downloader.OnStarted += HandleDownloadStarted;
            _downloader.OnProgress += HandleDownloadProgress;
            _downloader.OnCompleted += HandleDownloadCompleted;
            _downloader.OnError += HandleDownloadError;

            _extractor.OnStarted += HandleExtractStarted;
            _extractor.OnProgress += HandleExtractProgress;
            _extractor.OnCompleted += HandleExtractCompleted;
            _extractor.OnError += HandleExtractError;

            _contentHandler.OnStatus += HandleContentStatus;
            _contentHandler.OnProgress01 += HandleContentProgress;
            _contentHandler.OnError += HandleContentError;
        }

        private void UnsubscribeAll()
        {
            _downloader.OnStarted -= HandleDownloadStarted;
            _downloader.OnProgress -= HandleDownloadProgress;
            _downloader.OnCompleted -= HandleDownloadCompleted;
            _downloader.OnError -= HandleDownloadError;

            _extractor.OnStarted -= HandleExtractStarted;
            _extractor.OnProgress -= HandleExtractProgress;
            _extractor.OnCompleted -= HandleExtractCompleted;
            _extractor.OnError -= HandleExtractError;

            _contentHandler.OnStatus -= HandleContentStatus;
            _contentHandler.OnProgress01 -= HandleContentProgress;
            _contentHandler.OnError -= HandleContentError;
        }

        private void CleanupTemps()
        {
            _downloadTemp?.Clean();
            _extractTemp?.Clean();
        }

        private void DisposeTemps()
        {
            _downloadTemp?.Dispose();
            _downloadTemp = null;
            
            _extractTemp?.Dispose();
            _extractTemp = null;
        }

        private void ChangeStage(PipelineStage stage)
        {
            _stage = stage;
            OnStageChanged?.Invoke(stage);
        }

        private void ResetProgress()
        {
            _downloadProgress01 = 0f;
            _extractProgress01 = 0f;
            _handleProgress01 = 0f;
            PublishOverallProgress();
        }

        private void PublishOverallProgress()
        {
            float overall =
                (_downloadProgress01 * DownloadWeight) +
                (_extractProgress01 * ExtractWeight) +
                (_handleProgress01 * HandleWeight);

            OnProgress01?.Invoke(overall);
        }

        private void Fail(string message)
        {
            ChangeStage(PipelineStage.Failed);
            OnError?.Invoke(message);

            // best effort cleanup
            try { CleanupTemps(); } catch { }
        }

        // -----------------------
        // Downloader events
        // -----------------------

        private void HandleDownloadStarted(string url, string fullPath)
        {
            OnStatus?.Invoke("Downloading: " + url);
        }

        private void HandleDownloadProgress(string url, float progress01, ulong downloadedBytes, long totalBytes)
        {
            _downloadProgress01 = Clamp01(progress01);
            PublishOverallProgress();
        }

        private void HandleDownloadCompleted(string url, string fullPath)
        {
            _downloadProgress01 = 1f;
            PublishOverallProgress();
            OnStatus?.Invoke("Downloaded: " + fullPath);
        }

        private void HandleDownloadError(string url, string message)
        {
            Fail("Download error: " + message);
        }

        // -----------------------
        // Extractor events
        // -----------------------

        private void HandleExtractStarted(string archivePath, string extractDirectory)
        {
            OnStatus?.Invoke("Extracting: " + archivePath);
        }

        private void HandleExtractProgress(string entryName, int extractedEntries, int totalEntriesOrMinus1)
        {
            // If total is known (zip), compute; otherwise just pulse a bit.
            if (totalEntriesOrMinus1 > 0)
                _extractProgress01 = Clamp01((float)extractedEntries / totalEntriesOrMinus1);
            else
                _extractProgress01 = 0.5f; // tar.gz unknown total - keep mid until completed

            PublishOverallProgress();
        }

        private void HandleExtractCompleted(string extractDirectory)
        {
            _extractProgress01 = 1f;
            PublishOverallProgress();
            OnStatus?.Invoke("Extracted: " + extractDirectory);
        }

        private void HandleExtractError(string message)
        {
            Fail("Extract error: " + message);
        }

        // -----------------------
        // Content handler events
        // -----------------------

        private void HandleContentStatus(string status)
        {
            OnStatus?.Invoke(status);
        }

        private void HandleContentProgress(float p01)
        {
            _handleProgress01 = Clamp01(p01);
            PublishOverallProgress();
        }

        private void HandleContentError(string message)
        {
            Fail("Handle content error: " + message);
        }

        private static float Clamp01(float v) => Math.Clamp(v, 0, 1);
    }
}