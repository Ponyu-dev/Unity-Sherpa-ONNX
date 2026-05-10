using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Extracts LocalZip model archives from StreamingAssets
    /// to persistentDataPath at runtime.
    /// On Desktop: direct <see cref="ZipFile.ExtractToDirectory"/>.
    /// On Android: copies zip via <see cref="UnityWebRequest"/>
    /// to a temp file, extracts, then deletes temp.
    /// Skips extraction if a marker file exists.
    /// </summary>
    public static class LocalZipExtractor
    {
        private const string ExtractedMarker = ".zip-extracted";

        /// <summary>
        /// Ensures the zip archive for the given profile is extracted.
        /// Returns the extracted directory path, or null on failure.
        /// Safe to call from any thread — the method enters the main
        /// thread internally because it touches Unity APIs
        /// (<see cref="Application"/>, <see cref="UnityWebRequest"/>)
        /// that throw when invoked from the thread pool.
        /// </summary>
        public static async UniTask<string> EnsureExtractedAsync(
            string modelsSubfolder,
            string profileName,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            await UniTask.SwitchToMainThread(ct);

            string destDir = GetExtractedModelDirectory(modelsSubfolder, profileName);

            if (IsAlreadyExtracted(destDir))
            {
                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] LocalZip '{profileName}' already extracted.");
                progress?.Report(1f);
                return destDir;
            }

            string zipRelativePath = $"SherpaOnnx/{modelsSubfolder}/{profileName}.zip";

            try
            {
                // Copy phase reports 0..0.5; unzip phase jumps to 1.0 once
                // it finishes (ZipFile.ExtractToDirectory has no per-entry
                // progress without manually walking ZipArchive entries).
                IProgress<float> copyProgress = progress == null
                    ? null
                    : new Progress<float>(p => progress.Report(p * 0.5f));

                string zipPath = await ResolveZipPathAsync(
                    zipRelativePath, copyProgress, ct);
                if (zipPath == null)
                {
                    SherpaOnnxLog.RuntimeError($"[SherpaOnnx] LocalZip not found: {zipRelativePath}");
                    return null;
                }

                long estimatedSize = StorageChecker.EstimateZipExtractedSize(zipPath);
                if (estimatedSize > 0)
                {
                    string spaceError = StorageChecker.CheckSpace(destDir, estimatedSize);
                    if (spaceError != null)
                    {
                        SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {spaceError}");
                        return null;
                    }
                }

                progress?.Report(0.5f);

                FileSystemHelper.EnsureCreatedEmpty(destDir);

                // ZipFile.ExtractToDirectory is pure System.IO and can be
                // hundreds of MB of decompression — keep it off the main
                // thread so the UI does not freeze during install.
                await UniTask.RunOnThreadPool(
                    () => ZipFile.ExtractToDirectory(zipPath, destDir),
                    cancellationToken: ct);

                WriteMarker(destDir);
                progress?.Report(1f);

                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] LocalZip '{profileName}' extracted to {destDir}");
                return destDir;
            }
            catch (OperationCanceledException)
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] LocalZip extraction cancelled: {profileName}");
                FileSystemHelper.TryDeleteDirectory(destDir);
                return null;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] LocalZip extraction failed for '{profileName}': {ex.Message}");
                FileSystemHelper.TryDeleteDirectory(destDir);
                return null;
            }
        }

        /// <summary>
        /// Returns the expected directory for an extracted LocalZip profile.
        /// Always under persistentDataPath.
        /// </summary>
        public static string GetExtractedModelDirectory(
            string modelsSubfolder, string profileName)
        {
            // Read persistentDataPath through StreamingAssetsCopier's
            // path cache so this method stays callable from the thread
            // pool (Unity Application.* APIs are main-thread-only).
            string path = Path.Combine(
                StreamingAssetsCopier.PersistentDataPath,
                "SherpaOnnx",
                modelsSubfolder,
                profileName);
            return NativePathSanitizer.Sanitize(path);
        }

        // ── Disk-usage management ──

        /// <summary>
        /// Returns <c>true</c> if a LocalZip profile is currently extracted
        /// to <see cref="Application.persistentDataPath"/> (the marker file
        /// produced by a successful extraction is present).
        /// </summary>
        public static bool IsExtracted(string modelsSubfolder, string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;
            string destDir = GetExtractedModelDirectory(modelsSubfolder, profileName);
            return HasAnyExtractionMarker(destDir);
        }

        /// <summary>
        /// Returns the size on disk (sum of all files) of an extracted
        /// LocalZip profile, or <c>0</c> if the profile is not extracted.
        /// </summary>
        public static long GetExtractedSizeBytes(string modelsSubfolder, string profileName)
        {
            string destDir = GetExtractedModelDirectory(modelsSubfolder, profileName);
            if (!Directory.Exists(destDir))
                return 0L;

            long total = 0L;
            try
            {
                foreach (var file in Directory.EnumerateFiles(destDir, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(file).Length; }
                    catch { /* missing/locked file — skip */ }
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] GetExtractedSizeBytes('{profileName}'): {ex.Message}");
            }
            return total;
        }

        /// <summary>
        /// Lists every profile name that has an extracted directory under
        /// <c>persistentDataPath/SherpaOnnx/{modelsSubfolder}</c>. Picks
        /// up both LocalZip extractions (<c>.zip-extracted</c> marker)
        /// and Local/Remote per-profile extractions
        /// (<c>.profile-extracted</c> marker written by
        /// <see cref="StreamingAssetsCopier.EnsureProfileExtractedAsync"/>).
        /// Includes stale folders left over from profiles that were
        /// renamed or removed from settings — useful as input for
        /// <see cref="CleanupUnusedProfiles"/>.
        /// </summary>
        public static IReadOnlyList<string> ListExtractedProfiles(string modelsSubfolder)
        {
            string root = Path.Combine(
                StreamingAssetsCopier.PersistentDataPath, "SherpaOnnx", modelsSubfolder);
            if (!Directory.Exists(root))
                return Array.Empty<string>();

            var results = new List<string>();
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    if (HasAnyExtractionMarker(dir))
                        results.Add(Path.GetFileName(dir));
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] ListExtractedProfiles('{modelsSubfolder}'): {ex.Message}");
            }
            return results;
        }

        // True when the directory carries any of the three known
        // extraction markers, written by:
        //   .zip-extracted     → LocalZipExtractor (StreamingAssets zip)
        //   .profile-extracted → StreamingAssetsCopier
        //                        .EnsureProfileExtractedAsync (Local /
        //                        bundled Remote)
        //   .remote-extracted  → RemoteProfileFetcher.EnsureDownloadedAsync
        //                        (runtime download from URL)
        // Listing / cleanup APIs treat all three the same.
        private const string ProfileExtractedMarker = ".profile-extracted";
        private const string RemoteExtractedMarker = ".remote-extracted";
        private static bool HasAnyExtractionMarker(string dir)
        {
            return File.Exists(Path.Combine(dir, ExtractedMarker))
                || File.Exists(Path.Combine(dir, ProfileExtractedMarker))
                || File.Exists(Path.Combine(dir, RemoteExtractedMarker));
        }

        /// <summary>
        /// Deletes the extracted directory for a single profile,
        /// regardless of how it got there (bundled <c>Local</c>,
        /// runtime-downloaded <c>Remote</c>, or unzipped
        /// <c>LocalZip</c> — all three sources land in the same
        /// per-profile path under persistentDataPath). Returns
        /// <c>true</c> when the directory existed and was removed
        /// (or was already absent). Logs the freed-bytes count on
        /// success and the I/O exception on failure.
        /// </summary>
        public static bool TryDeleteExtractedModel(
            string modelsSubfolder, string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return false;

            string destDir = GetExtractedModelDirectory(modelsSubfolder, profileName);
            if (!Directory.Exists(destDir))
            {
                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] {modelsSubfolder} '{profileName}': nothing to delete (no extracted dir).");
                return true;
            }

            long freedBytes = SafeMeasureDirectorySize(destDir);

            try
            {
                Directory.Delete(destDir, recursive: true);
                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] {modelsSubfolder} '{profileName}': " +
                    $"removed extracted dir {destDir} " +
                    $"({FormatBytes(freedBytes)} freed).");
                return true;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] {modelsSubfolder} '{profileName}': " +
                    $"failed to remove {destDir} — {ex.Message}");
                return false;
            }
        }

        // Best-effort sum of file sizes under destDir. Errors (a file
        // disappearing mid-walk, permission denied) collapse to 0 so
        // a logging helper never explodes the caller's flow.
        private static long SafeMeasureDirectorySize(string dir)
        {
            try
            {
                long total = 0;
                string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    try { total += new FileInfo(files[i]).Length; }
                    catch { /* skip vanished / unreadable */ }
                }
                return total;
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            const long mb = 1024L * 1024L;
            const long kb = 1024L;
            if (bytes >= mb) return $"{bytes / (double)mb:F1} MB";
            if (bytes >= kb) return $"{bytes / (double)kb:F1} KB";
            return $"{bytes} B";
        }

        /// <summary>
        /// Removes every extracted LocalZip profile under
        /// <paramref name="modelsSubfolder"/> that is not in
        /// <paramref name="keepProfileNames"/>. Returns the number of
        /// profiles deleted. Use with the names of profiles currently
        /// referenced from <c>*-settings.json</c> to free disk space
        /// taken by orphan/renamed/unused models.
        /// </summary>
        public static int CleanupUnusedProfiles(
            string modelsSubfolder,
            IReadOnlyCollection<string> keepProfileNames)
        {
            var keep = keepProfileNames != null
                ? new HashSet<string>(keepProfileNames, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            int deleted = 0;
            foreach (var name in ListExtractedProfiles(modelsSubfolder))
            {
                if (keep.Contains(name))
                    continue;
                if (TryDeleteExtractedModel(modelsSubfolder, name))
                    deleted++;
            }
            return deleted;
        }

        // ── Private ──

        private static bool IsAlreadyExtracted(string destDir)
        {
            string marker = Path.Combine(destDir, ExtractedMarker);
            return File.Exists(marker);
        }

        private static void WriteMarker(string destDir)
        {
            string marker = Path.Combine(destDir, ExtractedMarker);
            File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// On Desktop: returns StreamingAssets path directly.
        /// On Android: copies zip from APK to temp file via UnityWebRequest.
        /// </summary>
        private static async UniTask<string> ResolveZipPathAsync(
            string zipRelativePath, IProgress<float> progress, CancellationToken ct)
        {
            string streamingPath = Path.Combine(StreamingAssetsCopier.StreamingAssetsPath, zipRelativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            return await CopyFromApkToTempAsync(streamingPath, progress, ct);
#else
            await UniTask.CompletedTask;

            if (File.Exists(streamingPath))
            {
                progress?.Report(1f);
                return streamingPath;
            }

            return null;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static async UniTask<string> CopyFromApkToTempAsync(
            string apkUrl, IProgress<float> progress, CancellationToken ct)
        {
            string tempZip = Path.Combine(Application.temporaryCachePath, "localzip_temp.zip");

            // Stream the APK asset directly to disk via DownloadHandlerFile —
            // model archives are tens to hundreds of megabytes and a buffered
            // request.downloadHandler.data + File.WriteAllBytes pair would
            // freeze the main thread (and can OOM on low-memory devices).
            using var request = UnityWebRequest.Get(apkUrl);
            request.downloadHandler =
                new DownloadHandlerFile(tempZip) { removeFileOnAbort = true };

            try
            {
                await request.SendWebRequest()
                    .ToUniTask(progress: progress, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] Failed to read zip from APK: {request.error}");
                FileSystemHelper.TryDeleteFile(tempZip);
                return null;
            }

            return tempZip;
        }
#endif
    }
}
