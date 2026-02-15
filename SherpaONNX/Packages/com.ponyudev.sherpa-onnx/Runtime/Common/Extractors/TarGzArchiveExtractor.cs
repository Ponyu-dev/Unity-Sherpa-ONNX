using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
    public sealed class TarGzArchiveExtractor : IArchiveExtractor
    {
        public event Action<string, string> OnStarted;
        public event Action<string, int, int> OnProgress;
        public event Action<string> OnCompleted;
        public event Action<string> OnError;

        private const int CopyBufferSize = 64 * 1024;

        private bool _disposed;

        public async Task ExtractAsync(string archivePath, string tempDirectoryPath, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TarGzArchiveExtractor));

            if (!File.Exists(archivePath))
                throw new FileNotFoundException("Archive not found.", archivePath);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] TarGz extraction started: {archivePath}");

            try
            {
                PrepareTempDirectory(tempDirectoryPath);
                OnStarted?.Invoke(archivePath, tempDirectoryPath);

                int doneEntries = 0;

                await using (var file = File.OpenRead(archivePath))
                await using (var gz = new GZipStream(file, CompressionMode.Decompress))
                {
                    byte[] header = new byte[TarUtils.BlockSize];

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int read = TarUtils.ReadExact(gz, header, 0, TarUtils.BlockSize);
                        if (read == 0)
                            break;

                        if (read < TarUtils.BlockSize)
                            throw new InvalidDataException("Invalid tar header.");

                        if (TarUtils.IsAllZeroBlock(header))
                            break;

                        TarHeader tarHeader = TarHeader.Parse(header);

                        if (string.IsNullOrEmpty(tarHeader.Name))
                            throw new InvalidDataException("Tar entry has empty name.");

                        string safeName = TarUtils.NormalizeEntryPath(tarHeader.Name);
                        string outPath = Path.Combine(tempDirectoryPath, safeName);

                        if (tarHeader.IsDirectory)
                        {
                            Directory.CreateDirectory(outPath);

                            doneEntries++;
                            OnProgress?.Invoke(tarHeader.Name, doneEntries, -1);
                            await Task.Yield();
                            continue;
                        }

                        string outDir = Path.GetDirectoryName(outPath);
                        if (!Directory.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        await ExtractFileAsync(gz, outPath, tarHeader.Size, cancellationToken);

                        // Align to 512 blocks.
                        SkipPadding(gz, tarHeader.Size, cancellationToken);

                        doneEntries++;
                        OnProgress?.Invoke(tarHeader.Name, doneEntries, -1);
                        await Task.Yield();
                    }
                }

                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] TarGz extraction completed: {doneEntries} entries → {tempDirectoryPath}");
                OnCompleted?.Invoke(tempDirectoryPath);
            }
            catch (OperationCanceledException)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] TarGz extraction canceled.");
                RaiseError("Extraction canceled.");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] TarGz extraction error: {ex}");
                RaiseError(ex.Message);
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

        private static void PrepareTempDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);

            Directory.CreateDirectory(path);
        }

        private static async Task ExtractFileAsync(Stream tarStream, string outPath, long size, CancellationToken ct)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);

            try
            {
                await using var outFile = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
                long remaining = size;

                while (remaining > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    int toRead = remaining > buffer.Length ? buffer.Length : (int)remaining;
                    int read = tarStream.Read(buffer, 0, toRead);
                    if (read <= 0)
                        throw new EndOfStreamException("Unexpected end of tar stream.");

                    outFile.Write(buffer, 0, read);
                    remaining -= read;

                    // Yield occasionally to avoid long frame stalls.
                    await Task.Yield();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void SkipPadding(Stream tarStream, long fileSize, CancellationToken ct)
        {
            long pad = fileSize % TarUtils.BlockSize;
            if (pad == 0)
                return;

            long toSkip = TarUtils.BlockSize - pad;
            byte[] skip = ArrayPool<byte>.Shared.Rent(TarUtils.BlockSize);

            try
            {
                while (toSkip > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    int chunk = toSkip > skip.Length ? skip.Length : (int)toSkip;
                    int read = tarStream.Read(skip, 0, chunk);
                    if (read <= 0)
                        throw new EndOfStreamException("Unexpected end of tar stream (padding).");

                    toSkip -= read;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(skip);
            }
        }

        private void RaiseError(string msg) => OnError?.Invoke(msg);
    }
}