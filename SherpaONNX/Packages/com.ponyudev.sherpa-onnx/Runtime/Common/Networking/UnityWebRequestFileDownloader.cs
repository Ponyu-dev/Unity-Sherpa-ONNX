using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace PonyuDev.SherpaOnnx.Common.Networking
{
    public interface IFileDownloader : IDisposable
    {
        event Action<string, string> OnStarted; // url, fullPath
        event Action<string, float, ulong, long> OnProgress; // url, progress01, downloadedBytes, totalBytes
        event Action<string, string> OnCompleted; // url, fullPath
        event Action<string, string> OnError; // url, message

        Task DownloadAsync(string url, string directoryPath, string fileName, CancellationToken cancellationToken);
    }
    
     /// <summary>
    /// Downloads any file by URL into a target directory using UnityWebRequest.
    /// Works in Runtime and Editor.
    /// </summary>
    public sealed class UnityWebRequestFileDownloader : IFileDownloader
    {
        public event Action<string, string> OnStarted;
        public event Action<string, float, ulong, long> OnProgress;
        public event Action<string, string> OnCompleted;
        public event Action<string, string> OnError;

        private const int ProgressReportDelayMs = 50;

        private bool _disposed;

        public async Task DownloadAsync(
            string url,
            string directoryPath,
            string fileName,
            CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnityWebRequestFileDownloader));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is null or empty.", nameof(url));

            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path is null or empty.", nameof(directoryPath));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is null or empty.", nameof(fileName));

            string fullPath = BuildFullPath(directoryPath, fileName);

            try
            {
                EnsureDirectoryExists(directoryPath);

                // If file exists, overwrite safely.
                DeleteIfExists(fullPath);

                OnStarted?.Invoke(url, fullPath);

                using var request = CreateRequest(url, fullPath);
                var operation = request.SendWebRequest();

                // Report progress until done.
                await PollProgressAsync(request, operation, url, cancellationToken);

                if (HasRequestError(request))
                {
                    TryDeletePartial(fullPath);
                    RaiseError(url, BuildErrorMessage(request));
                    return;
                }

                OnProgress?.Invoke(url, 1f, GetDownloadedBytes(request), GetTotalBytes(request));
                OnCompleted?.Invoke(url, fullPath);
            }
            catch (OperationCanceledException)
            {
                TryDeletePartial(fullPath);
                RaiseError(url, "Download canceled.");
            }
            catch (Exception ex)
            {
                TryDeletePartial(fullPath);
                RaiseError(url, ex.Message);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // Ensure no dangling subscribers.
            OnStarted = null;
            OnProgress = null;
            OnCompleted = null;
            OnError = null;
        }

        private static UnityWebRequest CreateRequest(string url, string fullPath)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);

            // Stream to file.
            var downloadHandler = new DownloadHandlerFile(fullPath)
            {
                removeFileOnAbort = true
            };

            request.downloadHandler = downloadHandler;
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;

            return request;
        }

        private async Task PollProgressAsync(
            UnityWebRequest request,
            UnityWebRequestAsyncOperation operation,
            string url,
            CancellationToken cancellationToken)
        {
            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // progress in [0..1]
                float progress01 = request.downloadProgress;
                ulong downloaded = GetDownloadedBytes(request);
                long total = GetTotalBytes(request);

                OnProgress?.Invoke(url, progress01, downloaded, total);

                await Task.Delay(ProgressReportDelayMs, cancellationToken: cancellationToken);
            }
        }

        private static bool HasRequestError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        private static string BuildErrorMessage(UnityWebRequest request)
        {
            // request.error is typically enough; include response code if HTTP.
            long code = request.responseCode;
            if (code > 0)
                return $"{request.error} (HTTP {code})";

            return request.error ?? "Unknown download error.";
        }

        private void RaiseError(string url, string message)
        {
            OnError?.Invoke(url, message);
        }

        private static string BuildFullPath(string directoryPath, string fileName)
        {
            // Normalize directory path separators.
            directoryPath = directoryPath.Replace('\\', '/');
            return Path.Combine(directoryPath, fileName);
        }

        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        private static void DeleteIfExists(string fullPath)
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        private static void TryDeletePartial(string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }

        private static ulong GetDownloadedBytes(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.downloadedBytes;
#else
            // Older versions: downloadedBytes exists too, but keep method for compatibility.
            return request.downloadedBytes;
#endif
        }

        private static long GetTotalBytes(UnityWebRequest request)
        {
            // If server provides Content-Length, Unity may expose it via GetResponseHeader.
            // Note: downloadHandler.data is not used (streaming).
            string header = request.GetResponseHeader("Content-Length");
            if (string.IsNullOrEmpty(header))
                return -1;

            if (long.TryParse(header, out long total))
                return total;

            return -1;
        }
    }
}