using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.IO;
using PonyuDev.SherpaOnnx.Common.Networking;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Shared logic for platform-specific archive caches (Android, iOS).
    /// Downloads and extracts once, reuses across multiple architecture installs.
    /// </summary>
    internal abstract class ArchiveCacheBase : IArchiveCache
    {
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action OnCacheChanged;

        protected abstract string CacheFolderName { get; }
        protected abstract string DownloadFolderName { get; }
        protected abstract string PlatformLabel { get; }

        /// <summary>
        /// Directory name or marker searched recursively to determine readiness.
        /// </summary>
        protected abstract string ReadyMarker { get; }

        public string CachePath =>
            Path.Combine(Application.temporaryCachePath, CacheFolderName);

        public bool IsReady
        {
            get
            {
                string cachePath = CachePath;
                if (!Directory.Exists(cachePath))
                    return false;

                string[] dirs = Directory.GetDirectories(
                    cachePath, ReadyMarker, SearchOption.AllDirectories);
                return dirs.Length > 0;
            }
        }

        public void Clean()
        {
            string cachePath = CachePath;

            try
            {
                if (Directory.Exists(cachePath))
                    Directory.Delete(cachePath, recursive: true);

                Debug.Log($"[SherpaOnnx] {PlatformLabel} cache cleaned.");
                OnCacheChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SherpaOnnx] Failed to clean {PlatformLabel} cache: {ex.Message}");
            }
        }

        public async Task EnsureExtractedAsync(
            string url,
            string fileName,
            CancellationToken ct)
        {
            if (IsReady)
            {
                RaiseStatus($"{PlatformLabel} cache ready, skipping download.");
                RaiseProgress(1f);
                return;
            }

            string downloadDir = Path.Combine(
                Application.temporaryCachePath, DownloadFolderName);
            string cachePath = CachePath;

            try
            {
                RaiseStatus($"Downloading {PlatformLabel} archive...");
                RaiseProgress(0f);

                var downloader = new UnityWebRequestFileDownloader();
                downloader.OnProgress += HandleDownloadProgress;

                Directory.CreateDirectory(downloadDir);
                await downloader.DownloadAsync(url, downloadDir, fileName, ct);

                downloader.OnProgress -= HandleDownloadProgress;

                RaiseStatus($"Extracting {PlatformLabel} archive...");
                RaiseProgress(0.5f);

                string archivePath = Path.Combine(downloadDir, fileName);

                using var extractor = new ArchiveExtractor();
                extractor.OnProgress += HandleExtractProgress;
                extractor.OnCompleted += HandleExtractCompleted;

                FileSystemHelper.EnsureCreatedEmpty(cachePath);
                await extractor.ExtractAsync(archivePath, cachePath, ct);

                extractor.OnProgress -= HandleExtractProgress;
                extractor.OnCompleted -= HandleExtractCompleted;

                RaiseStatus($"{PlatformLabel} archive cached.");
                RaiseProgress(1f);
                OnCacheChanged?.Invoke();
            }
            finally
            {
                FileSystemHelper.TryDeleteDirectory(downloadDir);
            }
        }

        /// <summary>
        /// Searches for a specific directory inside the cache recursively.
        /// Returns null if not found.
        /// </summary>
        internal string FindDirectoryInCache(string folderName)
        {
            string cachePath = CachePath;
            if (!Directory.Exists(cachePath))
                return null;

            string[] dirs = Directory.GetDirectories(
                cachePath, folderName, SearchOption.AllDirectories);
            return dirs.Length > 0 ? dirs[0] : null;
        }

        private void HandleDownloadProgress(
            string url, float progress01, ulong downloadedBytes, long totalBytes)
        {
            RaiseProgress(progress01 * 0.5f);
        }

        private void HandleExtractProgress(string entry, int done, int total)
        {
            float extractRatio = total > 0
                ? (float)done / total
                : Math.Min(done / 200f, 0.95f);

            RaiseProgress(0.5f + 0.5f * extractRatio);
        }

        private void HandleExtractCompleted(string dir)
        {
            RaiseProgress(1f);
            RaiseStatus("Extraction completed.");
        }

        private void RaiseStatus(string msg) => OnStatus?.Invoke(msg);
        private void RaiseProgress(float p) => OnProgress01?.Invoke(p);
    }
}
