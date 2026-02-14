using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
     public sealed class ZipArchiveExtractor : IArchiveExtractor
    {
        public event Action<string, string> OnStarted;
        public event Action<string, int, int> OnProgress;
        public event Action<string> OnCompleted;
        public event Action<string> OnError;

        private bool _disposed;

        public async Task ExtractAsync(string archivePath, string tempDirectoryPath, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ZipArchiveExtractor));

            if (!File.Exists(archivePath))
                throw new FileNotFoundException("Archive not found.", archivePath);

            try
            {
                PrepareTempDirectory(tempDirectoryPath);
                OnStarted?.Invoke(archivePath, tempDirectoryPath);

                using (var archive = ZipFile.OpenRead(archivePath))
                {
                    int total = archive.Entries.Count;
                    int done = 0;

                    for (int i = 0; i < total; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        ZipArchiveEntry entry = archive.Entries[i];
                        string entryName = entry.FullName;

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // Directory
                            Directory.CreateDirectory(Path.Combine(tempDirectoryPath, entry.FullName));
                            done++;
                            OnProgress?.Invoke(entryName, done, total);
                            await Task.Yield();
                            continue;
                        }

                        string fullPath = Path.Combine(tempDirectoryPath, entry.FullName);
                        string dir = Path.GetDirectoryName(fullPath);

                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        entry.ExtractToFile(fullPath, overwrite: true);

                        done++;
                        OnProgress?.Invoke(entryName, done, total);

                        // Give control back between files.
                        await Task.Yield();
                    }
                }

                OnCompleted?.Invoke(tempDirectoryPath);
            }
            catch (OperationCanceledException)
            {
                RaiseError("Extraction canceled.");
            }
            catch (Exception ex)
            {
                RaiseError(ex.Message);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            OnStarted = null;
            OnProgress = null;
            OnCompleted = null;
            OnError = null;
        }

        private static void PrepareTempDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);

            Directory.CreateDirectory(path);
        }

        private void RaiseError(string msg) => OnError?.Invoke(msg);
    }
}