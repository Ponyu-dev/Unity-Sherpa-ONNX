using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Copies SherpaOnnx files from StreamingAssets to persistentDataPath
    /// on Android. On other platforms this is a no-op.
    /// </summary>
    public static class StreamingAssetsCopier
    {
        private const string ManifestRelativePath =
            "SherpaOnnx/streaming-assets-manifest.json";

        private const string TargetFolder = "SherpaOnnx";
        private const string VersionFile = ".version";

        // ── Public API ──

        /// <summary>
        /// Returns the root path where SherpaOnnx files are readable.
        /// Android: <see cref="Application.persistentDataPath"/>
        /// (files copied there from APK).
        /// Other platforms: <see cref="Application.streamingAssetsPath"/>
        /// (direct filesystem access).
        /// </summary>
        public static string GetResolvedStreamingAssetsPath()
        {
            if (NeedsExtraction())
                return Application.persistentDataPath;

            return Application.streamingAssetsPath;
        }

        /// <summary>
        /// Ensures all SherpaOnnx files from the manifest are available
        /// on the local filesystem. On non-Android platforms returns
        /// immediately. Skips copy if the version marker matches.
        /// </summary>
        public static async UniTask<bool> EnsureExtractedAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            if (!NeedsExtraction())
                return true;

            try
            {
                return await ExtractAllAsync(progress, ct);
            }
            catch (OperationCanceledException)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] StreamingAssets extraction cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] StreamingAssets extraction failed: {ex.Message}");
                return false;
            }
        }

        // ── Private ──

        private static bool NeedsExtraction()
        {
            return Application.platform == RuntimePlatform.Android;
        }

        private static async UniTask<bool> ExtractAllAsync(
            IProgress<float> progress, CancellationToken ct)
        {
            // Load manifest from APK via UnityWebRequest.
            var manifest = await LoadManifestAsync(ct);
            if (manifest == null || manifest.files.Count == 0)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] Manifest is empty or failed to load.");
                return false;
            }

            string targetDir = Path.Combine(
                Application.persistentDataPath, TargetFolder);

            // Check version marker.
            if (IsAlreadyExtracted(targetDir, manifest.version))
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Files already extracted, skipping.");
                progress?.Report(1f);
                return true;
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Extracting {manifest.files.Count} files...");

            int total = manifest.files.Count;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();

                string relativePath = manifest.files[i];
                byte[] data = await ReadStreamingAssetAsync(relativePath, ct);

                if (data == null)
                {
                    SherpaOnnxLog.RuntimeError(
                        $"[SherpaOnnx] Failed to read: {relativePath}");
                    return false;
                }

                WriteFile(Application.persistentDataPath, relativePath, data);
                progress?.Report((i + 1f) / total);
            }

            WriteVersionMarker(targetDir, manifest.version);

            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] Extraction complete.");
            return true;
        }

        private static async UniTask<StreamingAssetsManifest> LoadManifestAsync(
            CancellationToken ct)
        {
            string url = CombineStreamingUrl(ManifestRelativePath);
            byte[] data = await DownloadBytesAsync(url, ct);

            if (data == null)
                return null;

            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<StreamingAssetsManifest>(json);
        }

        private static async UniTask<byte[]> ReadStreamingAssetAsync(
            string relativePath, CancellationToken ct)
        {
            string url = CombineStreamingUrl(relativePath);
            return await DownloadBytesAsync(url, ct);
        }

        private static async UniTask<byte[]> DownloadBytesAsync(
            string url, CancellationToken ct)
        {
            using var request = UnityWebRequest.Get(url);

            await request.SendWebRequest()
                .ToUniTask(cancellationToken: ct);

            if (HasError(request))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Download error: {url} — {request.error}");
                return null;
            }

            return request.downloadHandler.data;
        }

        private static string CombineStreamingUrl(string relativePath)
        {
            // On Android streamingAssetsPath is already a jar: URL.
            return Application.streamingAssetsPath + "/" + relativePath;
        }

        private static bool IsAlreadyExtracted(
            string targetDir, string version)
        {
            string markerPath = Path.Combine(targetDir, VersionFile);

            if (!File.Exists(markerPath))
                return false;

            string existing = File.ReadAllText(markerPath).Trim();
            return existing == version;
        }

        private static void WriteVersionMarker(
            string targetDir, string version)
        {
            Directory.CreateDirectory(targetDir);
            string markerPath = Path.Combine(targetDir, VersionFile);
            File.WriteAllText(markerPath, version);
        }

        private static void WriteFile(
            string rootDir, string relativePath, byte[] data)
        {
            string fullPath = Path.Combine(rootDir, relativePath);
            string dir = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(fullPath, data);
        }

        private static bool HasError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }
    }
}
