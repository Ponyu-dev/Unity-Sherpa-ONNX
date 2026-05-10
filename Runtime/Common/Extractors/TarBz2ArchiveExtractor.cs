using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
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

        private const int CopyBufferSize = 64 * 1024;

        // Emit a progress tick at most this often to avoid flooding
        // the bus on tiny entries while still feeling responsive.
        private const int ProgressMinIntervalMs = 250;

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

            long archiveSize = new FileInfo(archivePath).Length;
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TarBz2 extraction started: {archivePath} " +
                $"({archiveSize / (1024 * 1024)} MB compressed)");

            try
            {
                PrepareTempDirectory(tempDirectoryPath);
                OnStarted?.Invoke(archivePath, tempDirectoryPath);

                int doneEntries = 0;
                long lastLoggedRead = 0;

                using (var file = File.OpenRead(archivePath))
                // Wrap in BufferedStream — SharpZipLib's BZip2 reader
                // pulls bytes from the underlying stream in small reads
                // along some paths, which on Android external storage
                // is enough to dominate wall-clock time. A 64 KB
                // buffer collapses those into one syscall per chunk.
                using (var buffered = new BufferedStream(file, CopyBufferSize))
                using (var bz2 = new BZip2InputStream(buffered))
                {
                    var ticker = new ProgressTicker(this, file, archiveSize);
                    byte[] header = new byte[TarUtils.BlockSize];

                    // Initial 0% so the UI knows we started — without
                    // this the first event the bus sees might not
                    // arrive for tens of seconds while BZ2 fills its
                    // first decompression block.
                    ticker.EmitInitial();

                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int read = TarUtils.ReadExact(bz2, header, 0, TarUtils.BlockSize);
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
                        }
                        else
                        {
                            string outDir = Path.GetDirectoryName(outPath);
                            if (!Directory.Exists(outDir))
                                Directory.CreateDirectory(outDir);

                            ExtractFile(bz2, outPath, tarHeader.Size, ticker, tarHeader.Name, cancellationToken);
                            SkipPadding(bz2, tarHeader.Size, cancellationToken);

                            doneEntries++;
                        }

                        ticker.TickIfDue(tarHeader.Name);

                        // Per-32-MB info log so it's visible in adb
                        // logcat that extraction is alive — silent BZ2
                        // decompression on Android can take minutes.
                        if (file.Position - lastLoggedRead >= 32L * 1024 * 1024)
                        {
                            SherpaOnnxLog.RuntimeLog(
                                $"[SherpaOnnx] TarBz2 extracting… " +
                                $"{file.Position / (1024 * 1024)}/" +
                                $"{archiveSize / (1024 * 1024)} MB, " +
                                $"{doneEntries} entries");
                            lastLoggedRead = file.Position;
                        }

                        if (doneEntries % 50 == 0)
                            await Task.Yield();
                    }

                    ticker.EmitFinal();
                }

                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] TarBz2 extraction completed: {doneEntries} entries → {tempDirectoryPath}");
                OnCompleted?.Invoke(tempDirectoryPath);
            }
            catch (OperationCanceledException)
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] TarBz2 extraction canceled.");
                RaiseError("Extraction canceled.");
                throw;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] TarBz2 extraction error: {ex}");
                RaiseError(ex.Message);
                // Bubble the failure up so the caller (e.g.
                // RemoteProfileFetcher) treats this archive as broken
                // and skips the marker / cleans up partial state
                // instead of declaring success on an empty directory.
                throw;
            }
        }

        // OnProgress is (entryName, done, total). For tar streams the
        // entry count is unknown, but the underlying compressed file
        // size IS known — report compressed-bytes progress in KB so
        // listeners (e.g. ProfileReadyEvent's percent calculation)
        // get a meaningful 0..100% bar instead of a frozen 0%.
        // Scaled to KB to fit in int for archives up to ~2 TB.
        private void EmitCompressedProgress(string entryName, long compressedRead, long archiveSize)
        {
            if (archiveSize <= 0)
                return;

            int doneKb = (int)(compressedRead / 1024);
            int totalKb = (int)(archiveSize / 1024);
            if (doneKb < 0) doneKb = 0;
            if (doneKb > totalKb) doneKb = totalKb;

            OnProgress?.Invoke(entryName, doneKb, totalKb);
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

        private static void ExtractFile(
            Stream tarStream,
            string outPath,
            long size,
            ProgressTicker ticker,
            string entryName,
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

                    // Mid-entry pulse so a single large file (a 50+ MB
                    // ONNX weight blob is typical for VITS/Matcha) does
                    // not freeze the progress bar at the percent it
                    // had before this entry started.
                    ticker.TickIfDue(entryName);
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

        // Throttled wrapper around EmitCompressedProgress. Holds the
        // owning extractor + raw FileStream + total size so callers
        // (the main loop and ExtractFile) can call TickIfDue without
        // tracking lastEmitMs manually. One instance per extraction.
        private sealed class ProgressTicker
        {
            private readonly TarBz2ArchiveExtractor _owner;
            private readonly FileStream _rawFile;
            private readonly long _archiveSize;
            private int _lastEmitMs;

            internal ProgressTicker(
                TarBz2ArchiveExtractor owner,
                FileStream rawFile,
                long archiveSize)
            {
                _owner = owner;
                _rawFile = rawFile;
                _archiveSize = archiveSize;
                _lastEmitMs = Environment.TickCount;
            }

            internal void EmitInitial()
            {
                _owner.EmitCompressedProgress("(starting)", 0, _archiveSize);
                _lastEmitMs = Environment.TickCount;
            }

            internal void EmitFinal()
            {
                _owner.EmitCompressedProgress("(done)", _archiveSize, _archiveSize);
            }

            internal void TickIfDue(string entryName)
            {
                int now = Environment.TickCount;
                if (now - _lastEmitMs < ProgressMinIntervalMs)
                    return;
                _lastEmitMs = now;
                _owner.EmitCompressedProgress(entryName, _rawFile.Position, _archiveSize);
            }
        }
    }
}