using System;
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
        /// </summary>
        public static async UniTask<string> EnsureExtractedAsync(
            string modelsSubfolder,
            string profileName,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            string destDir = GetExtractedModelDirectory(modelsSubfolder, profileName);

            if (IsAlreadyExtracted(destDir))
            {
                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] LocalZip '{profileName}' already extracted.");
                progress?.Report(1f);
                return destDir;
            }

            string zipRelativePath = $"SherpaOnnx/{modelsSubfolder}/{profileName}.zip";

            try
            {
                string zipPath = await ResolveZipPathAsync(zipRelativePath, ct);
                if (zipPath == null)
                {
                    SherpaOnnxLog.RuntimeError(
                        $"[SherpaOnnx] LocalZip not found: {zipRelativePath}");
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

                progress?.Report(0.1f);

                FileSystemHelper.EnsureCreatedEmpty(destDir);
                ZipFile.ExtractToDirectory(zipPath, destDir);

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
            string path = Path.Combine(
                Application.persistentDataPath,
                "SherpaOnnx",
                modelsSubfolder,
                profileName);
            return NativePathSanitizer.Sanitize(path);
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
            string zipRelativePath, CancellationToken ct)
        {
            string streamingPath = Path.Combine(
                Application.streamingAssetsPath, zipRelativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            return await CopyFromApkToTempAsync(streamingPath, ct);
#else
            await UniTask.CompletedTask;

            if (File.Exists(streamingPath))
                return streamingPath;

            return null;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static async UniTask<string> CopyFromApkToTempAsync(
            string apkUrl, CancellationToken ct)
        {
            string tempZip = Path.Combine(
                Application.temporaryCachePath, "localzip_temp.zip");

            using var request = UnityWebRequest.Get(apkUrl);
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);

            if (request.result != UnityWebRequest.Result.Success)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Failed to read zip from APK: {request.error}");
                return null;
            }

            File.WriteAllBytes(tempZip, request.downloadHandler.data);
            return tempZip;
        }
#endif
    }
}
