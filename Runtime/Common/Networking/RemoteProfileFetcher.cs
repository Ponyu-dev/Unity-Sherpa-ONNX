using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.IO;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Networking
{
    /// <summary>
    /// Downloads a profile's archive from a URL, decompresses it into
    /// <see cref="Application.persistentDataPath"/> the same way
    /// <see cref="LocalZipExtractor"/> does for bundled archives, and
    /// stamps a <c>.remote-extracted</c> marker so the result survives
    /// across launches. Format auto-detected by extension via
    /// <see cref="ArchiveExtractor"/>: <c>.zip</c>, <c>.tar.gz</c>,
    /// <c>.tgz</c>, <c>.tar.bz2</c>.
    /// <para/>
    /// Built on the existing <see cref="UnityWebRequestFileDownloader"/>
    /// (streams to disk via <c>DownloadHandlerFile</c>, no buffered
    /// <c>byte[]</c>) and the <c>com.unity.sharp-zip-lib</c> package
    /// already declared in <c>package.json</c>, so this works on every
    /// scripting backend (iOS / Android / Standalone / WebGL) without
    /// extra dependencies.
    /// </summary>
    public static class RemoteProfileFetcher
    {
        /// <summary>
        /// Marker file written into the extracted profile directory.
        /// Stores a SHA-1 of the source <c>archiveUrl</c> so a URL change
        /// invalidates the cache and triggers a fresh download.
        /// </summary>
        private const string ExtractedMarker = ".remote-extracted";

        private const int DefaultMaxRetries = 3;
        private const int InitialBackoffMs = 1000;

        /// <summary>
        /// Ensures the profile's archive is downloaded from
        /// <paramref name="archiveUrl"/> and extracted into
        /// <c>persistentDataPath/SherpaOnnx/{modelsSubfolder}/{profileName}/</c>.
        /// Idempotent: skips work when the on-disk marker matches the URL.
        /// Safe to call from any thread; switches to the main thread
        /// internally for Unity APIs and runs decompression on the thread
        /// pool so the UI stays responsive.
        /// <para/>
        /// Reports <see cref="ProfileReadyPhase.Download"/> with 0..100
        /// percent during the network transfer, then
        /// <see cref="ProfileReadyPhase.Extract"/> with 0..100 percent
        /// during decompression. Does <b>not</b> emit
        /// <see cref="ProfileReadyPhase.Ready"/> on its own — that is
        /// the calling service's responsibility, because native engine
        /// init still has to run after this method returns.
        /// </summary>
        /// <param name="onEvent">
        /// Optional named callback for <see cref="ProfileReadyEvent"/>.
        /// Pass a method group from the host (e.g. <c>OnFetcherEvent</c>)
        /// to avoid lambdas.
        /// </param>
        /// <param name="maxRetries">
        /// Number of total download attempts (1 + retries). Each retry
        /// uses exponential backoff (1s, 2s, 4s …) before retrying.
        /// </param>
        public static async UniTask<bool> EnsureDownloadedAsync(
            string modelsSubfolder,
            string profileName,
            string archiveUrl,
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default,
            int maxRetries = DefaultMaxRetries)
        {
            if (string.IsNullOrEmpty(modelsSubfolder) || string.IsNullOrEmpty(profileName))
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] RemoteProfileFetcher: subfolder/profileName required.");
                return false;
            }
            if (string.IsNullOrEmpty(archiveUrl))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] RemoteProfileFetcher: archiveUrl is empty for '{profileName}'.");
                return false;
            }
            if (maxRetries < 1) maxRetries = 1;

            await UniTask.SwitchToMainThread(ct);
            StreamingAssetsCopier.PrimePathCacheOnMainThread();

            string urlHash = ComputeUrlHash(archiveUrl);
            string profileDir = LocalZipExtractor.GetExtractedModelDirectory(modelsSubfolder, profileName);

            if (IsMarkerCurrent(profileDir, urlHash))
                return true;

            string tempDir = GetTempDir();
            string archiveExt = GuessArchiveExtension(archiveUrl);
            string archiveFileName = $"{SanitizeProfileName(profileName)}{archiveExt}";
            string archivePath = Path.Combine(tempDir, archiveFileName);

            // ── Download phase ──

            Emit(onEvent, ProfileReadyPhase.Download, 0, archiveUrl);

            bool downloaded = false;
            Exception lastError = null;
            for (int attempt = 1; attempt <= maxRetries && !downloaded; attempt++)
            {
                if (attempt > 1)
                {
                    EmitRetrying(onEvent, archiveUrl, attempt, lastError);
                    int delayMs = InitialBackoffMs * (1 << (attempt - 2)); // 1s, 2s, 4s, …
                    try { await UniTask.Delay(delayMs, cancellationToken: ct); }
                    catch (OperationCanceledException) { return false; }
                }

                try
                {
                    bool ok = await DownloadAttempt.RunAsync(archiveUrl, tempDir, archiveFileName, onEvent, ct);
                    if (ok)
                        downloaded = true;
                    else
                        lastError = null;
                }
                catch (OperationCanceledException)
                {
                    FileSystemHelper.TryDeleteFile(archivePath);
                    return false;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    FileSystemHelper.TryDeleteFile(archivePath);
                }
            }

            if (!downloaded)
            {
                EmitFailed(onEvent, archiveUrl, lastError);
                FileSystemHelper.TryDeleteFile(archivePath);
                return false;
            }

            Emit(onEvent, ProfileReadyPhase.Download, 100, archiveUrl);

            // ── Extract phase ──

            Emit(onEvent, ProfileReadyPhase.Extract, 0, archiveUrl);

            try
            {
                FileSystemHelper.EnsureCreatedEmpty(profileDir);

                using var extractor = new ArchiveExtractor();
                using var extractProgress = new ExtractProgressForwarder(onEvent, archiveUrl);
                extractProgress.Bind(extractor);

                await UniTask.RunOnThreadPool(new ExtractWork(extractor, archivePath, profileDir, ct).Run, cancellationToken: ct);

                // Sherpa-onnx tar archives almost always nest every
                // file under one top-level folder named after the
                // model. Without this strip step the runtime path
                // resolver would look for "<profileDir>/model.onnx"
                // while the actual file lives at
                // "<profileDir>/<profileName>/model.onnx".
                StripSingleTopLevelFolder(profileDir);

                WriteMarker(profileDir, urlHash);
            }
            catch (OperationCanceledException)
            {
                FileSystemHelper.TryDeleteDirectory(profileDir);
                FileSystemHelper.TryDeleteFile(archivePath);
                return false;
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] RemoteProfileFetcher: extraction failed for '{profileName}': {ex.Message}");
                FileSystemHelper.TryDeleteDirectory(profileDir);
                FileSystemHelper.TryDeleteFile(archivePath);
                EmitFailed(onEvent, archiveUrl, ex);
                return false;
            }
            finally
            {
                FileSystemHelper.TryDeleteFile(archivePath);
            }

            Emit(onEvent, ProfileReadyPhase.Extract, 100, archiveUrl);
            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] RemoteProfileFetcher: '{profileName}' ready at {profileDir}");
            return true;
        }

        // ── Marker ──

        /// <summary>
        /// Returns <c>true</c> if a directory already carries the
        /// <c>.remote-extracted</c> marker for the given URL.
        /// </summary>
        public static bool IsExtracted(
            string modelsSubfolder, string profileName, string archiveUrl)
        {
            if (string.IsNullOrEmpty(profileName) || string.IsNullOrEmpty(archiveUrl))
                return false;
            string profileDir = LocalZipExtractor.GetExtractedModelDirectory(modelsSubfolder, profileName);
            return IsMarkerCurrent(profileDir, ComputeUrlHash(archiveUrl));
        }

        private static bool IsMarkerCurrent(string profileDir, string urlHash)
        {
            string markerPath = Path.Combine(profileDir, ExtractedMarker);
            if (!File.Exists(markerPath))
                return false;

            // The marker on its own is not enough — past versions of
            // the extractor swallowed errors and stamped the marker on
            // an empty / partially-written directory, leaving a stale
            // "ready" state that crashed native LoadProfile when no
            // real model files were on disk. Re-validate by counting
            // non-marker files; treat empty / marker-only directories
            // as not-extracted.
            try
            {
                if (!HasAnyNonMarkerFile(profileDir))
                    return false;

                string existing = File.ReadAllText(markerPath).Trim();
                return existing == urlHash;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasAnyNonMarkerFile(string profileDir)
        {
            if (!Directory.Exists(profileDir))
                return false;
            string[] files = Directory.GetFiles(profileDir, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (name == ExtractedMarker)
                    continue;
                return true;
            }
            return false;
        }

        // If the extracted directory contains exactly one entry — a
        // sub-directory holding everything — promote that sub-directory's
        // children to the top level and delete the now-empty wrapper.
        // Mirrors `tar --strip-components=1`. Safe no-op when the
        // archive already extracts at the top level (e.g. user-built
        // LocalZip uploads).
        private static void StripSingleTopLevelFolder(string profileDir)
        {
            string[] entries = Directory.GetFileSystemEntries(profileDir);
            if (entries.Length != 1)
                return;
            string sole = entries[0];
            if (!Directory.Exists(sole))
                return;

            string[] inner = Directory.GetFileSystemEntries(sole);
            for (int i = 0; i < inner.Length; i++)
            {
                string fileName = Path.GetFileName(inner[i]);
                string newPath = Path.Combine(profileDir, fileName);
                if (Directory.Exists(inner[i]))
                    Directory.Move(inner[i], newPath);
                else
                    File.Move(inner[i], newPath);
            }
            Directory.Delete(sole, recursive: true);
        }

        private static void WriteMarker(string profileDir, string urlHash)
        {
            Directory.CreateDirectory(profileDir);
            File.WriteAllText(Path.Combine(profileDir, ExtractedMarker), urlHash);
        }

        // ── Helpers ──

        private static string GetTempDir()
        {
            // Application.temporaryCachePath needs the main thread; we
            // already switched there before the first call into this.
            string dir = Path.Combine(Application.temporaryCachePath, "sherpaonnx_remote");
            Directory.CreateDirectory(dir);
            return dir;
        }

        // Best-effort double-extension parser that also strips any query
        // string. We don't normalise via Path.GetExtension because
        // ".tar.gz" and ".tar.bz2" carry information across two dots.
        private static string GuessArchiveExtension(string url)
        {
            if (string.IsNullOrEmpty(url)) return ".bin";
            string clean = url;
            int q = clean.IndexOf('?');
            if (q >= 0) clean = clean.Substring(0, q);
            int hash = clean.IndexOf('#');
            if (hash >= 0) clean = clean.Substring(0, hash);

            string lower = clean.ToLowerInvariant();
            if (lower.EndsWith(".tar.bz2")) return ".tar.bz2";
            if (lower.EndsWith(".tar.gz"))  return ".tar.gz";
            if (lower.EndsWith(".tgz"))     return ".tgz";
            if (lower.EndsWith(".zip"))     return ".zip";
            if (lower.EndsWith(".nupkg"))   return ".nupkg";

            // Default to .zip — ArchiveExtractor will surface a clear
            // error if the actual content isn't a recognised format.
            return ".zip";
        }

        private static string SanitizeProfileName(string name)
        {
            // Avoid path-injection / weird characters in the temp file
            // name. Profile names come from settings and should already
            // be safe, but be defensive.
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (invalid[j] == c) { bad = true; break; }
                }
                sb.Append(bad ? '_' : c);
            }
            return sb.ToString();
        }

        private static string ComputeUrlHash(string url)
        {
            using var sha = SHA1.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(url ?? string.Empty));
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        // ── Event emission (named methods — no inline lambdas) ──

        private static void Emit(Action<ProfileReadyEvent> onEvent, ProfileReadyPhase phase, int percent, string url)
        {
            if (onEvent == null)
                return;
            onEvent(new ProfileReadyEvent(phase, percent, url));
        }

        private static void EmitRetrying(Action<ProfileReadyEvent> onEvent, string url, int attempt, Exception error)
        {
            if (onEvent == null)
                return;
            onEvent(new ProfileReadyEvent(ProfileReadyPhase.DownloadRetrying, 0, url, error?.Message, attempt, error));
        }

        private static void EmitFailed(Action<ProfileReadyEvent> onEvent, string url, Exception error)
        {
            if (onEvent == null)
                return;
            onEvent(new ProfileReadyEvent(ProfileReadyPhase.Failed, 0, url, error?.Message, 0, error));
        }

        // ── Helper types ──

        // Keeps the per-attempt progress subscription instance-local so
        // the call site stays free of inline lambdas. Reusable instance
        // bound to a single <see cref="UnityWebRequestFileDownloader"/>
        // for the duration of one DownloadAsync call.
        private sealed class DownloadAttempt
        {
            private readonly Action<ProfileReadyEvent> _onEvent;
            private readonly string _url;

            private DownloadAttempt(Action<ProfileReadyEvent> onEvent, string url)
            {
                _onEvent = onEvent;
                _url = url;
            }

            /// <summary>
            /// Runs one download attempt. Returns <c>true</c> on
            /// success (the file exists with non-zero size on disk).
            /// </summary>
            public static async UniTask<bool> RunAsync(
                string url, string dir, string fileName,
                Action<ProfileReadyEvent> onEvent, CancellationToken ct)
            {
                using var downloader = new UnityWebRequestFileDownloader();
                var attempt = new DownloadAttempt(onEvent, url);

                downloader.OnProgress += attempt.HandleProgress;
                try
                {
                    await downloader.DownloadAsync(url, dir, fileName, ct);
                }
                finally
                {
                    downloader.OnProgress -= attempt.HandleProgress;
                }

                string fullPath = Path.Combine(dir, fileName);
                if (!File.Exists(fullPath))
                    return false;

                long size = new FileInfo(fullPath).Length;
                return size > 0;
            }

            // Forwards UnityWebRequestFileDownloader's 4-argument event
            // into a ProfileReadyEvent of phase Download with byte
            // percent. Named instance method so the callsite stays
            // lambda-free.
            private void HandleProgress(string url, float progress01, ulong downloadedBytes, long totalBytes)
            {
                if (_onEvent == null)
                    return;
                int percent = (int)(progress01 * 100f);
                _onEvent(new ProfileReadyEvent(ProfileReadyPhase.Download, percent, _url));
            }
        }

        // Bridges ArchiveExtractor's per-entry event into the caller's
        // ProfileReadyEvent stream. Tar streams don't know the total
        // entry count up front (total == -1); in that case the bar
        // simply holds at 0% until extraction finishes. zip archives
        // do know — we report done/total scaled to 0..100.
        // Implements IDisposable so the using statement on the call
        // site detaches the event handler when the scope ends.
        private sealed class ExtractProgressForwarder : IDisposable
        {
            private readonly Action<ProfileReadyEvent> _onEvent;
            private readonly string _url;
            private ArchiveExtractor _extractor;

            public ExtractProgressForwarder(Action<ProfileReadyEvent> onEvent, string url)
            {
                _onEvent = onEvent;
                _url = url;
            }

            public void Bind(ArchiveExtractor extractor)
            {
                _extractor = extractor;
                if (_extractor != null)
                    _extractor.OnProgress += HandleProgress;
            }

            public void Dispose()
            {
                if (_extractor != null)
                {
                    _extractor.OnProgress -= HandleProgress;
                    _extractor = null;
                }
            }

            private void HandleProgress(string entryName, int done, int total)
            {
                if (_onEvent == null || total <= 0 || done < 0)
                    return;
                int percent = (int)((long)done * 100L / total);
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                _onEvent(new ProfileReadyEvent(ProfileReadyPhase.Extract, percent, _url));
            }
        }

        // Holds the args for ArchiveExtractor.ExtractAsync so the call
        // can be passed to UniTask.RunOnThreadPool as a method group
        // (no inline lambda capturing locals).
        private sealed class ExtractWork
        {
            private readonly ArchiveExtractor _extractor;
            private readonly string _archivePath;
            private readonly string _destDir;
            private readonly CancellationToken _ct;

            public ExtractWork(ArchiveExtractor extractor, string archivePath, string destDir, CancellationToken ct)
            {
                _extractor = extractor;
                _archivePath = archivePath;
                _destDir = destDir;
                _ct = ct;
            }

            public void Run()
            {
                // ArchiveExtractor.ExtractAsync returns a Task; we are
                // already on a worker thread inside RunOnThreadPool, so
                // blocking it synchronously is the simplest way to wait
                // for extraction. Use GetAwaiter().GetResult() rather
                // than .Wait() so any extractor exception bubbles up
                // unwrapped instead of as AggregateException — the
                // outer catch in EnsureDownloadedAsync logs ex.Message.
                _extractor.ExtractAsync(_archivePath, _destDir, _ct).GetAwaiter().GetResult();
            }
        }
    }
}
