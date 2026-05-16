using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.IO
{
    /// <summary>
    /// Cross-platform utility for checking available disk space
    /// and estimating archive extracted sizes.
    /// </summary>
    public static class StorageChecker
    {
        private const long SafetyMarginBytes = 50L * 1024 * 1024; // 50 MB

        /// <summary>
        /// Returns available bytes on the drive containing <paramref name="path"/>,
        /// or -1 if the value cannot be determined.
        /// </summary>
        public static long GetAvailableBytes(string path)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return GetAvailableBytesAndroid(path);
#else
                return GetAvailableBytesDesktop(path);
#endif
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning($"[SherpaOnnx] StorageChecker.GetAvailableBytes failed: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Returns null if enough space is available, or an error message otherwise.
        /// Adds a safety margin of 50 MB on top of <paramref name="requiredBytes"/>.
        /// Returns null (OK) when available space cannot be determined.
        /// </summary>
        public static string CheckSpace(string path, long requiredBytes)
        {
            long available = GetAvailableBytes(path);
            if (available < 0)
                return null; // unknown — skip check

            long needed = requiredBytes + SafetyMarginBytes;
            if (available >= needed)
                return null;

            long availMb = available / (1024 * 1024);
            long needMb = needed / (1024 * 1024);
            return $"Not enough disk space. Available: {availMb} MB, required: {needMb} MB (including 50 MB safety margin).";
        }

        /// <summary>
        /// Returns the sum of uncompressed entry sizes for a zip archive.
        /// Returns -1 if the file does not exist or cannot be read.
        /// </summary>
        public static long EstimateZipExtractedSize(string zipPath)
        {
            try
            {
                if (!File.Exists(zipPath))
                    return -1;

                long total = 0;
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (ZipArchiveEntry entry in archive.Entries)
                    total += entry.Length;

                return total;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning($"[SherpaOnnx] EstimateZipExtractedSize failed: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Estimates extracted size for an archive.
        /// Zip: exact sum of entry sizes.
        /// Tar.gz/tar.bz2: compressed file size * 3 heuristic.
        /// Returns -1 if the file does not exist or cannot be read.
        /// </summary>
        public static long EstimateArchiveExtractedSize(string archivePath)
        {
            if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
                return -1;

            string lower = archivePath.ToLowerInvariant();

            if (lower.EndsWith(".zip"))
                return EstimateZipExtractedSize(archivePath);

            // tar.gz, tar.bz2 — use heuristic
            long compressedSize = new FileInfo(archivePath).Length;
            return compressedSize * 3;
        }

        // ── Platform-specific ──

        private static long GetAvailableBytesDesktop(string path)
        {
            string root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
                return -1;

            var drive = new DriveInfo(root);
            return drive.IsReady ? drive.AvailableFreeSpace : -1;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static long GetAvailableBytesAndroid(string path)
        {
            using var statFs = new AndroidJavaObject("android.os.StatFs", path);
            return statFs.Call<long>("getAvailableBytes");
        }
#endif
    }
}
