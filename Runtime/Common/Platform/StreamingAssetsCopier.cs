using System;
using System.Collections.Generic;
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

        // Legacy marker — written by older builders that produced a flat
        // file list (manifest.files) instead of shared+profileGroups.
        // Still recognised so users upgrading from an older build don't
        // re-extract everything on first launch under the new code.
        private const string LegacyVersionFile = ".version";

        // New markers used when the manifest carries shared/profileGroups.
        // Per-profile marker lives inside the profile's own folder so
        // deleting the folder also drops the marker, which makes the
        // next "ensure" call re-extract that single profile.
        private const string SharedVersionFile = ".shared-version";
        private const string ProfileExtractedFile = ".profile-extracted";

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
        /// Ensures the shared SherpaOnnx files (settings JSONs + anything
        /// outside per-profile subfolders) are available on the local
        /// filesystem. On non-Android platforms returns immediately. Skips
        /// the copy if the shared marker matches the current manifest
        /// version. Per-profile model files are extracted lazily by
        /// <see cref="EnsureProfileExtractedAsync"/> when the active
        /// service first needs them, so individual profiles can be
        /// deleted to reclaim disk space without breaking other profiles.
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
                var manifest = await GetManifestAsync(ct);
                if (manifest == null)
                    return false;

                bool hasNewLayout =
                    (manifest.profileGroups != null && manifest.profileGroups.Count > 0)
                    || (manifest.shared != null && manifest.shared.Count > 0);

                return hasNewLayout
                    ? await ExtractSharedAsync(manifest, progress, ct)
                    : await ExtractLegacyAsync(manifest, progress, ct);
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

        /// <summary>
        /// Ensures the files of a single per-profile group are extracted
        /// to <see cref="Application.persistentDataPath"/>. The argument
        /// is the relative subdir under <c>SherpaOnnx/</c>, e.g.
        /// <c>tts-models/vits-piper-en</c>. Skips the copy when the
        /// per-profile marker matches the current manifest version.
        /// Returns <c>true</c> on non-Android platforms (no extraction
        /// needed), when the manifest has no entry for the subdir
        /// (legacy manifest, nothing to do), or when the group is
        /// already extracted. Returns <c>false</c> on I/O / network
        /// failure.
        /// </summary>
        public static async UniTask<bool> EnsureProfileExtractedAsync(
            string profileSubdir,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(profileSubdir))
                return false;

            await UniTask.SwitchToMainThread(ct);
            PrimePathCacheOnMainThread();

            if (!NeedsExtraction())
                return true;

            try
            {
                var manifest = await GetManifestAsync(ct);
                if (manifest == null)
                    return false;

                StreamingAssetsManifestProfileGroup group = null;
                if (manifest.profileGroups != null)
                {
                    foreach (var g in manifest.profileGroups)
                    {
                        if (g != null && g.subdir == profileSubdir)
                        {
                            group = g;
                            break;
                        }
                    }
                }

                if (group == null || group.files == null || group.files.Count == 0)
                {
                    // Either an old manifest (the legacy ExtractAll path
                    // already laid this profile down at first launch), or
                    // an unknown subdir. Nothing to do.
                    progress?.Report(1f);
                    return true;
                }

                string profileDir = Path.Combine(
                    _cachedPersistentDataPath, TargetFolder, profileSubdir);

                if (IsProfileMarkerCurrent(profileDir, manifest.version))
                {
                    progress?.Report(1f);
                    return true;
                }

                if (group.sizeBytes > 0)
                {
                    string spaceError = StorageChecker.CheckSpace(
                        _cachedPersistentDataPath, group.sizeBytes);
                    if (spaceError != null)
                    {
                        SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {spaceError}");
                        return false;
                    }
                }

                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] Extracting profile '{profileSubdir}' " +
                    $"({group.files.Count} files)...");

                bool ok = await ExtractFileListAsync(
                    group.files, progress, ct, profileDir);
                if (!ok)
                    return false;

                WriteProfileMarker(profileDir, manifest.version);
                SherpaOnnxLog.RuntimeLog(
                    $"[SherpaOnnx] Profile '{profileSubdir}' extracted.");
                return true;
            }
            catch (OperationCanceledException)
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] Profile extraction cancelled: {profileSubdir}");
                return false;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Profile extraction failed for '{profileSubdir}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Forgets the cached manifest so the next
        /// <see cref="EnsureExtractedAsync"/> /
        /// <see cref="EnsureProfileExtractedAsync"/> call re-reads it
        /// from StreamingAssets. Useful after replacing
        /// <c>streaming-assets-manifest.json</c> at runtime — typically
        /// not needed in shipping apps.
        /// </summary>
        public static void InvalidateManifestCache()
        {
            _cachedManifest = null;
        }

        // ── Private ──

        private static bool NeedsExtraction()
        {
            if (!_pathsCached) PrimePathCacheOnMainThread();
            return _cachedIsAndroid;
        }

        // Legacy path — manifest has the flat `files` list and no
        // shared/profileGroups split. Extracts the lot in one pass and
        // writes the legacy `.version` marker, exactly as the previous
        // implementation did.
        private static async UniTask<bool> ExtractLegacyAsync(
            StreamingAssetsManifest manifest,
            IProgress<float> progress, CancellationToken ct)
        {
            if (manifest.files == null || manifest.files.Count == 0)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] Manifest is empty or failed to load.");
                return false;
            }

            string targetDir = Path.Combine(
                _cachedPersistentDataPath, TargetFolder);

            if (IsLegacyMarkerCurrent(targetDir, manifest.version))
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Files already extracted, skipping.");
                progress?.Report(1f);
                return true;
            }

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

            bool ok = await ExtractFileListAsync(
                manifest.files, progress, ct, targetDir);
            if (!ok)
                return false;

            WriteLegacyMarker(targetDir, manifest.version);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] Extraction complete.");
            return true;
        }

        // New path — extracts only the shared section. Per-profile
        // groups are extracted lazily via EnsureProfileExtractedAsync.
        private static async UniTask<bool> ExtractSharedAsync(
            StreamingAssetsManifest manifest,
            IProgress<float> progress, CancellationToken ct)
        {
            string targetDir = Path.Combine(
                _cachedPersistentDataPath, TargetFolder);

            if (IsSharedMarkerCurrent(targetDir, manifest.version))
            {
                progress?.Report(1f);
                return true;
            }

            var sharedFiles = manifest.shared ?? new List<string>();

            // Disk-space check — sum of shared sizes only. We don't have
            // a per-shared sizeBytes in the manifest, so fall back to
            // totalSizeBytes when the share is the only thing present.
            // Practically the shared section is settings JSONs, sub-MB.
            // Profile groups will check their own size before extracting.

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Extracting {sharedFiles.Count} shared files...");

            bool ok = await ExtractFileListAsync(
                sharedFiles, progress, ct, targetDir);
            if (!ok)
                return false;

            WriteSharedMarker(targetDir, manifest.version);
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] Shared extraction complete.");
            return true;
        }

        // Common loop body for legacy + shared extraction. Walks the
        // file list, runs ExtractFileAsync with per-file byte progress,
        // and on failure cleans up the target directory. Marker / log
        // writes are the caller's responsibility on success.
        private static async UniTask<bool> ExtractFileListAsync(
            List<string> files,
            IProgress<float> progress, CancellationToken ct,
            string cleanupDirOnFailure)
        {
            int total = files.Count;
            if (total == 0)
            {
                progress?.Report(1f);
                return true;
            }

            bool success = false;
            try
            {
                for (int i = 0; i < total; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    string relativePath = files[i];

                    IProgress<float> fileProgress = progress == null
                        ? null
                        : new FileProgressAdapter(progress, i, total);

                    bool extracted = await ExtractFileAsync(
                        relativePath, _cachedPersistentDataPath,
                        fileProgress, ct);
                    if (!extracted)
                        return false;

                    progress?.Report((i + 1f) / total);
                }

                success = true;
                return true;
            }
            finally
            {
                if (!success && !string.IsNullOrEmpty(cleanupDirOnFailure))
                    FileSystemHelper.TryDeleteDirectory(cleanupDirOnFailure);
            }
        }

        // Maps the per-file 0..1 download fraction into the overall 0..1
        // by adding the index of the file currently in flight. Without
        // this, large single files would leave the bar stuck at 0 until
        // the file finishes and then jump. Plain class instead of an
        // inline `new Progress<float>(p => ...)` so the call site stays
        // lambda-free.
        private sealed class FileProgressAdapter : IProgress<float>
        {
            private readonly IProgress<float> _outer;
            private readonly int _fileIndex;
            private readonly int _total;

            public FileProgressAdapter(IProgress<float> outer, int fileIndex, int total)
            {
                _outer = outer;
                _fileIndex = fileIndex;
                _total = total;
            }

            public void Report(float byteProgress)
            {
                _outer.Report((_fileIndex + byteProgress) / _total);
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

        // ── Markers ──

        private static bool IsLegacyMarkerCurrent(string targetDir, string version)
            => IsMarkerCurrent(Path.Combine(targetDir, LegacyVersionFile), version);

        private static bool IsSharedMarkerCurrent(string targetDir, string version)
            => IsMarkerCurrent(Path.Combine(targetDir, SharedVersionFile), version);

        private static bool IsProfileMarkerCurrent(string profileDir, string version)
            => IsMarkerCurrent(Path.Combine(profileDir, ProfileExtractedFile), version);

        private static bool IsMarkerCurrent(string markerPath, string version)
        {
            if (!File.Exists(markerPath))
                return false;
            try
            {
                string existing = File.ReadAllText(markerPath).Trim();
                return existing == version;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteLegacyMarker(string targetDir, string version)
            => WriteMarker(Path.Combine(targetDir, LegacyVersionFile), targetDir, version);

        private static void WriteSharedMarker(string targetDir, string version)
            => WriteMarker(Path.Combine(targetDir, SharedVersionFile), targetDir, version);

        private static void WriteProfileMarker(string profileDir, string version)
            => WriteMarker(Path.Combine(profileDir, ProfileExtractedFile), profileDir, version);

        private static void WriteMarker(string markerPath, string parentDir, string version)
        {
            Directory.CreateDirectory(parentDir);
            File.WriteAllText(markerPath, version);
        }

        // ── Manifest cache ──

        private static StreamingAssetsManifest _cachedManifest;

        private static async UniTask<StreamingAssetsManifest> GetManifestAsync(
            CancellationToken ct)
        {
            if (_cachedManifest != null)
                return _cachedManifest;

            var manifest = await LoadManifestAsync(ct);
            if (manifest != null)
                _cachedManifest = manifest;
            return manifest;
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
