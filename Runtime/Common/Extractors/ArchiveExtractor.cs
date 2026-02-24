using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
    public sealed class ArchiveExtractor : IArchiveExtractor
    {
        public event Action<string, string> OnStarted;
        public event Action<string, int, int> OnProgress;
        public event Action<string> OnCompleted;
        public event Action<string> OnError;

        private bool _disposed;

        public async Task ExtractAsync(string archivePath, string tempDirectoryPath, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ArchiveExtractor));

            IArchiveExtractor extractor = null;

            try
            {
                extractor = CreateExtractor(archivePath);

                extractor.OnStarted += HandleStarted;
                extractor.OnProgress += HandleProgress;
                extractor.OnCompleted += HandleCompleted;
                extractor.OnError += HandleError;

                await extractor.ExtractAsync(archivePath, tempDirectoryPath, cancellationToken);
            }
            finally
            {
                if (extractor != null)
                {
                    extractor.OnStarted -= HandleStarted;
                    extractor.OnProgress -= HandleProgress;
                    extractor.OnCompleted -= HandleCompleted;
                    extractor.OnError -= HandleError;

                    extractor.Dispose();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            OnStarted = null;
            OnProgress = null;
            OnCompleted = null;
            OnError = null;
        }

        private static IArchiveExtractor CreateExtractor(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath))
                throw new ArgumentException("Archive path is null or empty.", nameof(archivePath));

            string lower = archivePath.ToLowerInvariant();

            if (lower.EndsWith(".tar.bz2"))
                return new TarBz2ArchiveExtractor();

            if (lower.EndsWith(".tar.gz"))
                return new TarGzArchiveExtractor();

            if (lower.EndsWith(".tgz"))
                return new TarGzArchiveExtractor();

            if (lower.EndsWith(".zip") || lower.EndsWith(".nupkg"))
                return new ZipArchiveExtractor();

            throw new NotSupportedException(
                $"Unsupported archive format: '{Path.GetFileName(archivePath)}'. " +
                "Supported formats: .tar.bz2, .tar.gz, .tgz, .zip");
        }

        private void HandleStarted(string archivePath, string dir) => OnStarted?.Invoke(archivePath, dir);
        private void HandleProgress(string entry, int done, int total) => OnProgress?.Invoke(entry, done, total);
        private void HandleCompleted(string dir) => OnCompleted?.Invoke(dir);
        private void HandleError(string msg) => OnError?.Invoke(msg);
    }
}