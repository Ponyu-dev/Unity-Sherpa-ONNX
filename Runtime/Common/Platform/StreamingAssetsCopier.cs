using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.IO;
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

        // ── Path cache ──
        //
        // Application.persistentDataPath / streamingAssetsPath / platform
        // are Unity APIs that throw "can only be called from the main
        // thread" when accessed from the thread pool. Several plugin
        // paths (model-dir resolution, native engine load) need these
        // values from background threads — we capture them once on the
        // main thread (during EnsureExtractedAsync below, or on first
        // explicit access via PrimePathCacheOnMainThread) and serve them
        // out of fields afterwards.

        private static string _cachedPersistentDataPath;
        private static string _cachedStreamingAssetsPath;
        private static bool _cachedIsAndroid;
        private static volatile bool _pathsCached;

        /// <summary>
        /// Captures <see cref="Application.persistentDataPath"/>,
        /// <see cref="Application.streamingAssetsPath"/>, and
        /// <see cref="Application.platform"/> for later use from the
        /// thread pool. Idempotent. <b>Must be called from the main
        /// thread</b> on its very first invocation; subsequent calls
        /// from any thread are free.
        /// </summary>
        public static void PrimePathCacheOnMainThread()
        {
            if (_pathsCached) return;
            _cachedPersistentDataPath = Application.persistentDataPath;
            _cachedStreamingAssetsPath = Application.streamingAssetsPath;
            _cachedIsAndroid = Application.platform == RuntimePlatform.Android;
            _pathsCached = true;
        }

        /// <summary>
        /// Cached <see cref="Application.persistentDataPath"/> — safe to
        /// read from any thread once <see cref="PrimePathCacheOnMainThread"/>
        /// has run (it does so automatically inside
        /// <see cref="EnsureExtractedAsync"/>).
        /// </summary>
        public static string PersistentDataPath
        {
            get
            {
                if (!_pathsCached) PrimePathCacheOnMainThread();
                return _cachedPersistentDataPath;
            }
        }

        /// <summary>
        /// Cached <see cref="Application.streamingAssetsPath"/> — safe
        /// to read from any thread once the cache is primed.
        /// </summary>
        public static string StreamingAssetsPath
        {
            get
            {
                if (!_pathsCached) PrimePathCacheOnMainThread();
                return _cachedStreamingAssetsPath;
            }
        }

        /// <summary>
        /// Returns the root path where SherpaOnnx files are readable.
        /// Android: <see cref="Application.persistentDataPath"/>
        /// (files copied there from APK).
        /// Other platforms: <see cref="Application.streamingAssetsPath"/>
        /// (direct filesystem access).
        /// Safe to call from any thread once the path cache is primed.
        /// </summary>
        public static string GetResolvedStreamingAssetsPath()
        {
            if (!_pathsCached) PrimePathCacheOnMainThread();
            return _cachedIsAndroid
                ? _cachedPersistentDataPath
                : _cachedStreamingAssetsPath;
        }

        /// <summary>
        /// Ensures all SherpaOnnx files from the manifest are available
        /// on the local filesystem. On non-Android platforms returns
        /// immediately. Skips copy if the version marker matches.
        /// Safe to call from any thread — the method enters the main
        /// thread internally because it touches Unity APIs
        /// (<see cref="Application"/>, <see cref="UnityWebRequest"/>)
        /// that throw when invoked from the thread pool.
        /// </summary>
        public static async UniTask<bool> EnsureExtractedAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            await UniTask.SwitchToMainThread(ct);
            PrimePathCacheOnMainThread();

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
            if (!_pathsCached) PrimePathCacheOnMainThread();
            return _cachedIsAndroid;
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
                _cachedPersistentDataPath, TargetFolder);

            // Check version marker.
            if (IsAlreadyExtracted(targetDir, manifest.version))
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Files already extracted, skipping.");
                progress?.Report(1f);
                return true;
            }

            // Check available disk space before extraction.
            if (manifest.totalSizeBytes > 0)
            {
                string spaceError = StorageChecker.CheckSpace(
                    _cachedPersistentDataPath, manifest.totalSizeBytes);
                if (spaceError != null)
                {
                    SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {spaceError}");
                    return false;
                }
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Extracting {manifest.files.Count} files...");

            int total = manifest.files.Count;
            bool success = false;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    string relativePath = manifest.files[i];
                    int fileIndex = i;

                    // Per-file byte-progress adapter: maps the request's
                    // 0..1 download progress into the overall 0..1 by
                    // adding the index of the file currently in flight.
                    // Without this, large single files (~hundreds of MB)
                    // would leave the bar stuck at 0 until the file
                    // finishes and then jump.
                    IProgress<float> fileProgress = progress == null
                        ? null
                        : new Progress<float>(byteProgress =>
                            progress.Report((fileIndex + byteProgress) / total));

                    bool extracted = await ExtractFileAsync(
                        relativePath, _cachedPersistentDataPath,
                        fileProgress, ct);

                    if (!extracted)
                        return false;

                    progress?.Report((fileIndex + 1f) / total);
                }

                WriteVersionMarker(targetDir, manifest.version);
                success = true;

                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Extraction complete.");
                return true;
            }
            finally
            {
                if (!success)
                    FileSystemHelper.TryDeleteDirectory(targetDir);
            }
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

        /// <summary>
        /// Streams a single asset directly into a file under
        /// <paramref name="rootDir"/>, without buffering its bytes in memory.
        /// Uses <see cref="DownloadHandlerFile"/> so the main thread never
        /// blocks on a large <c>File.WriteAllBytes</c>: bytes flow into the
        /// destination as they arrive over the player loop's async tick.
        /// <paramref name="progress"/> receives the request's byte progress
        /// (0..1 over this single file) — caller is expected to scale it
        /// into an overall fraction.
        /// </summary>
        private static async UniTask<bool> ExtractFileAsync(
            string relativePath, string rootDir,
            IProgress<float> progress, CancellationToken ct)
        {
            string targetPath = Path.Combine(rootDir, relativePath);
            string targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            string url = CombineStreamingUrl(relativePath);

            using var request = UnityWebRequest.Get(url);
            request.downloadHandler =
                new DownloadHandlerFile(targetPath) { removeFileOnAbort = true };

            try
            {
                await request.SendWebRequest()
                    .ToUniTask(progress: progress, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                // DownloadHandlerFile.removeFileOnAbort takes care of the
                // partial file when SendWebRequest is aborted via CT.
                throw;
            }

            if (HasError(request))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Download error: {url} — {request.error}");
                // HTTP / network failures don't trigger removeFileOnAbort,
                // so clean up any zero-byte / partial file ourselves.
                FileSystemHelper.TryDeleteFile(targetPath);
                return false;
            }

            return true;
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
            return _cachedStreamingAssetsPath + "/" + relativePath;
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
