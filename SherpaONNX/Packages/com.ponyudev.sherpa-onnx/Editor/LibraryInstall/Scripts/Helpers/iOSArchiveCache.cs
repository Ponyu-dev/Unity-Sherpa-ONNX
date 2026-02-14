using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.Networking;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Manages a shared cache for the extracted iOS .tar.bz2 archive.
    /// Downloads and extracts once, reuses across arm64 and simulator installs.
    /// </summary>
    internal static class iOSArchiveCache
    {
        private const string CacheFolderName = "SherpaOnnx_iOSCache";
        private const string DownloadFolderName = "SherpaOnnx_iOSDownload";
        private const string XcframeworkMarker = "sherpa-onnx.xcframework";
        private const string BuildIosFolder = "build-ios";

        internal static event Action<string> OnStatus;
        internal static event Action<float> OnProgress01;
        internal static event Action<string> OnError;
        internal static event Action OnCacheChanged;

        internal static string CachePath =>
            Path.Combine(Application.temporaryCachePath, CacheFolderName);

        internal static bool IsReady
        {
            get
            {
                string cachePath = CachePath;
                if (!Directory.Exists(cachePath))
                    return false;

                string[] dirs = Directory.GetDirectories(
                    cachePath, XcframeworkMarker, SearchOption.AllDirectories);
                return dirs.Length > 0;
            }
        }

        internal static void Clean()
        {
            string cachePath = CachePath;

            try
            {
                if (Directory.Exists(cachePath))
                    Directory.Delete(cachePath, recursive: true);

                Debug.Log("[SherpaOnnx] iOS cache cleaned.");
                OnCacheChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SherpaOnnx] Failed to clean iOS cache: {ex.Message}");
            }
        }

        internal static async Task EnsureExtractedAsync(
            string url,
            string fileName,
            CancellationToken ct)
        {
            if (IsReady)
            {
                OnStatus?.Invoke("iOS cache ready, skipping download.");
                OnProgress01?.Invoke(1f);
                return;
            }

            string downloadDir = Path.Combine(Application.temporaryCachePath, DownloadFolderName);
            string cachePath = CachePath;

            try
            {
                // Download
                OnStatus?.Invoke("Downloading iOS archive...");
                OnProgress01?.Invoke(0f);

                var downloader = new UnityWebRequestFileDownloader();
                downloader.OnProgress += HandleDownloadProgress;

                Directory.CreateDirectory(downloadDir);
                await downloader.DownloadAsync(url, downloadDir, fileName, ct);

                downloader.OnProgress -= HandleDownloadProgress;

                // Extract
                OnStatus?.Invoke("Extracting iOS archive...");
                OnProgress01?.Invoke(0.5f);

                string archivePath = Path.Combine(downloadDir, fileName);

                using var extractor = new ArchiveExtractor();
                extractor.OnProgress += HandleExtractProgress;
                extractor.OnCompleted += HandleExtractCompleted;

                if (Directory.Exists(cachePath))
                    Directory.Delete(cachePath, recursive: true);

                Directory.CreateDirectory(cachePath);
                await extractor.ExtractAsync(archivePath, cachePath, ct);

                extractor.OnProgress -= HandleExtractProgress;
                extractor.OnCompleted -= HandleExtractCompleted;

                OnStatus?.Invoke("iOS archive cached.");
                OnProgress01?.Invoke(1f);
                OnCacheChanged?.Invoke();
            }
            finally
            {
                TryDeleteDirectory(downloadDir);
            }
        }

        /// <summary>
        /// Returns the path to build-ios/ inside the cache.
        /// Searches recursively because the archive has a root folder.
        /// </summary>
        internal static string FindBuildIosPath()
        {
            string cachePath = CachePath;
            if (!Directory.Exists(cachePath))
                return null;

            string[] dirs = Directory.GetDirectories(
                cachePath, BuildIosFolder, SearchOption.AllDirectories);
            return dirs.Length > 0 ? dirs[0] : null;
        }

        private static void HandleDownloadProgress(
            string url, float progress01, ulong downloadedBytes, long totalBytes)
        {
            OnProgress01?.Invoke(progress01 * 0.5f);
        }

        private static void HandleExtractProgress(string entry, int done, int total)
        {
            float extractRatio = total > 0
                ? (float)done / total
                : Math.Min(done / 200f, 0.95f);

            OnProgress01?.Invoke(0.5f + 0.5f * extractRatio);
        }

        private static void HandleExtractCompleted(string dir)
        {
            OnProgress01?.Invoke(1f);
            OnStatus?.Invoke("Extraction completed.");
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
