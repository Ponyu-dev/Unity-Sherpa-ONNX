using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.SharpZipLib.BZip2;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
    /// <summary>
    /// Extracts .tar.bz2 archives. Uses SharpZipLib for BZip2 decompression
    /// and the same tar parsing logic as TarGzArchiveExtractor.
    /// </summary>
    public sealed class TarBz2ArchiveExtractor : IArchiveExtractor
    {
        public event Action<string, string> OnStarted;
        public event Action<string, int, int> OnProgress;
        public event Action<string> OnCompleted;
        public event Action<string> OnError;

        private const int TarBlockSize = 512;
        private const int CopyBufferSize = 64 * 1024;

        private bool _disposed;

        public async Task ExtractAsync(
            string archivePath,
            string tempDirectoryPath,
            CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TarBz2ArchiveExtractor));

            if (!File.Exists(archivePath))
                throw new FileNotFoundException("Archive not found.", archivePath);

            try
            {
                PrepareTempDirectory(tempDirectoryPath);
                OnStarted?.Invoke(archivePath, tempDirectoryPath);

                int doneEntries = 0;

                using (var file = File.OpenRead(archivePath))
                using (var bz2 = new BZip2InputStream(file))
                {
                    byte[] header = new byte[TarBlockSize];

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int read = ReadExact(bz2, header, 0, TarBlockSize);
                        if (read == 0)
                            break;

                        if (read < TarBlockSize)
                            throw new InvalidDataException("Invalid tar header.");

                        if (IsAllZeroBlock(header))
                            break;

                        TarHeader tarHeader = TarHeader.Parse(header);

                        if (string.IsNullOrEmpty(tarHeader.Name))
                            throw new InvalidDataException("Tar entry has empty name.");

                        string safeName = NormalizeEntryPath(tarHeader.Name);
                        string outPath = Path.Combine(tempDirectoryPath, safeName);

                        if (tarHeader.IsDirectory)
                        {
                            Directory.CreateDirectory(outPath);

                            doneEntries++;
                            OnProgress?.Invoke(tarHeader.Name, doneEntries, -1);
                            continue;
                        }

                        string outDir = Path.GetDirectoryName(outPath);
                        if (!Directory.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        ExtractFile(bz2, outPath, tarHeader.Size, cancellationToken);
                        SkipPadding(bz2, tarHeader.Size, cancellationToken);

                        doneEntries++;
                        OnProgress?.Invoke(tarHeader.Name, doneEntries, -1);

                        // Yield every 50 entries to keep UI responsive
                        if (doneEntries % 50 == 0)
                            await Task.Yield();
                    }
                }

                Debug.Log($"[SherpaOnnx] TarBz2 extraction completed: {doneEntries} entries → {tempDirectoryPath}");
                OnCompleted?.Invoke(tempDirectoryPath);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[SherpaOnnx] TarBz2 extraction canceled.");
                RaiseError("Extraction canceled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SherpaOnnx] TarBz2 extraction error: {ex}");
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

        private static int ReadExact(Stream s, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int r = s.Read(buffer, offset + total, count - total);
                if (r <= 0)
                    return total;
                total += r;
            }
            return total;
        }

        private static bool IsAllZeroBlock(byte[] block)
        {
            for (int i = 0; i < block.Length; i++)
            {
                if (block[i] != 0)
                    return false;
            }
            return true;
        }

        private static string NormalizeEntryPath(string name)
        {
            name = name.Replace('\\', '/');
            while (name.StartsWith("/", StringComparison.Ordinal))
                name = name.Substring(1);

            if (name.Contains(".."))
                throw new InvalidDataException("Tar entry contains invalid path: " + name);

            return name;
        }

        private static void ExtractFile(
            Stream tarStream,
            string outPath,
            long size,
            CancellationToken ct)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);

            try
            {
                using var outFile = new FileStream(
                    outPath, FileMode.Create, FileAccess.Write, FileShare.None);
                long remaining = size;

                while (remaining > 0)
                {
                    ct.ThrowIfCancellationRequested();

                    int toRead = remaining > buffer.Length
                        ? buffer.Length
                        : (int)remaining;
                    int read = tarStream.Read(buffer, 0, toRead);
                    if (read <= 0)
                        throw new EndOfStreamException("Unexpected end of tar stream.");

                    outFile.Write(buffer, 0, read);
                    remaining -= read;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void SkipPadding(Stream tarStream, long fileSize, CancellationToken ct)
        {
            long pad = fileSize % TarBlockSize;
            if (pad == 0)
                return;

            long toSkip = TarBlockSize - pad;
            byte[] skip = ArrayPool<byte>.Shared.Rent(TarBlockSize);

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

        private readonly struct TarHeader
        {
            public readonly string Name;
            public readonly long Size;
            public readonly byte TypeFlag;

            public bool IsDirectory =>
                TypeFlag == (byte)'5' || Name.EndsWith("/", StringComparison.Ordinal);

            private TarHeader(string name, long size, byte typeFlag)
            {
                Name = name;
                Size = size;
                TypeFlag = typeFlag;
            }

            public static TarHeader Parse(byte[] header)
            {
                string name = ReadNullTerminatedString(header, 0, 100);
                string sizeOctal = ReadNullTerminatedString(header, 124, 12);
                byte typeFlag = header[156];

                string prefix = ReadNullTerminatedString(header, 345, 155);
                if (!string.IsNullOrEmpty(prefix))
                    name = prefix + "/" + name;

                long size = ParseOctalLong(sizeOctal);

                if (typeFlag == 0)
                    typeFlag = (byte)'0';

                return new TarHeader(name, size, typeFlag);
            }

            private static string ReadNullTerminatedString(byte[] bytes, int offset, int length)
            {
                int end = offset;
                int max = offset + length;

                while (end < max && bytes[end] != 0)
                    end++;

                return Encoding.ASCII.GetString(bytes, offset, end - offset).Trim();
            }

            private static long ParseOctalLong(string octal)
            {
                if (string.IsNullOrEmpty(octal))
                    return 0;

                octal = octal.Trim();

                long value = 0;
                for (int i = 0; i < octal.Length; i++)
                {
                    char c = octal[i];
                    if (c < '0' || c > '7')
                        break;

                    value = (value << 3) + (c - '0');
                }
                return value;
            }
        }
    }
}