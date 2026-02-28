using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.IO;
using PonyuDev.SherpaOnnx.Common.Networking;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Creates install pipelines and encapsulates platform-specific install flows.
    /// Caller is responsible for disposing the returned pipeline.
    /// </summary>
    internal static class InstallPipelineFactory
    {
        internal static bool IsAndroid(LibraryArch arch)
        {
            return arch.Platform == PlatformType.Android;
        }

        internal static bool IsIOS(LibraryArch arch)
        {
            return arch.Platform == PlatformType.iOS;
        }

        internal static PackageInstallPipeline Create(LibraryArch arch)
        {
            IExtractedContentHandler handler = arch == LibraryPlatforms.ManagedLibrary
                ? new ManagedDllContentHandler()
                : new NativeLibraryContentHandler(arch.Name);

            var downloader = new UnityWebRequestFileDownloader();
            var extractor = new ArchiveExtractor();
            var tempFactory = new TempDirectoryFactory();

            return new PackageInstallPipeline(downloader, extractor, handler, tempFactory);
        }

        /// <summary>
        /// Android-specific install: uses shared cache, does not re-download
        /// if archive is already extracted.
        /// </summary>
        internal static async Task RunAndroidInstallAsync(
            LibraryArch arch,
            string version,
            CancellationToken ct,
            Action<string> onStatus = null,
            Action<float> onProgress = null)
        {
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Android install started: {arch.Name} v{version}");

            IArchiveCache cache = AndroidArchiveCache.Cache;
            SubscribeCache(cache, onStatus, onProgress);
            try
            {
                string url = BuildUrl(arch, version);
                string fileName = BuildFileName(arch);

                await AndroidArchiveCache.EnsureExtractedAsync(url, fileName, ct);

                string jniLibsPath = AndroidArchiveCache.FindJniLibsPath();

                if (string.IsNullOrEmpty(jniLibsPath))
                    throw new InvalidOperationException(
                        "jniLibs directory not found in Android cache.");

                var handler = new AndroidNativeContentHandler(arch.Name);
                await handler.HandleAsync(jniLibsPath, ct);

                SherpaOnnxLog.EditorLog($"[SherpaOnnx] Android install completed: {arch.Name}");
            }
            finally
            {
                UnsubscribeCache(cache, onStatus, onProgress);
            }
        }

        /// <summary>
        /// iOS-specific install: uses shared cache, does not re-download
        /// if archive is already extracted.
        /// </summary>
        internal static async Task RuniOSInstallAsync(
            LibraryArch arch,
            string version,
            CancellationToken ct,
            Action<string> onStatus = null,
            Action<float> onProgress = null)
        {
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] iOS install started: {arch.Name} v{version}");

            IArchiveCache cache = iOSArchiveCache.Cache;
            SubscribeCache(cache, onStatus, onProgress);
            try
            {
                string url = BuildUrl(arch, version);
                string fileName = BuildFileName(arch);

                await iOSArchiveCache.EnsureExtractedAsync(url, fileName, ct);

                string buildIosPath = iOSArchiveCache.FindBuildIosPath();

                if (string.IsNullOrEmpty(buildIosPath))
                    throw new InvalidOperationException(
                        "build-ios directory not found in iOS cache.");

                var handler = new iOSNativeContentHandler(arch.Name);
                await handler.HandleAsync(buildIosPath, ct);

                await DownloadIosManagedDllAsync(version, ct);

                SherpaOnnxLog.EditorLog($"[SherpaOnnx] iOS install completed: {arch.Name}");
            }
            finally
            {
                UnsubscribeCache(cache, onStatus, onProgress);
            }
        }

        private static void SubscribeCache(
            IArchiveCache cache,
            Action<string> onStatus,
            Action<float> onProgress)
        {
            if (onStatus != null) cache.OnStatus += onStatus;
            if (onProgress != null) cache.OnProgress01 += onProgress;
        }

        private static void UnsubscribeCache(
            IArchiveCache cache,
            Action<string> onStatus,
            Action<float> onProgress)
        {
            if (onStatus != null) cache.OnStatus -= onStatus;
            if (onProgress != null) cache.OnProgress01 -= onProgress;
        }

        /// <summary>
        /// Downloads sherpa-onnx.zip from our GitHub releases, extracts
        /// sherpa-onnx.dll and copies it into Assets/Plugins/SherpaOnnx/iOS/.
        /// </summary>
        private static async Task DownloadIosManagedDllAsync(
            string version,
            CancellationToken ct)
        {
            string url = string.Format(LibraryPlatforms.IosManagedDllUrl, version);
            const string fileName = "sherpa-onnx.zip";

            string tempDir = Path.Combine(Application.temporaryCachePath, "SherpaOnnx_iOSDll");

            try
            {
                SherpaOnnxLog.EditorLog("[SherpaOnnx] Downloading iOS managed DLL...");

                Directory.CreateDirectory(tempDir);

                var downloader = new UnityWebRequestFileDownloader();
                await downloader.DownloadAsync(url, tempDir, fileName, ct);

                string zipPath = Path.Combine(tempDir, fileName);

                if (!File.Exists(zipPath))
                    throw new FileNotFoundException(
                        $"Failed to download iOS managed DLL from {url}. " +
                        "Make sure the GitHub release exists.", zipPath);

                string extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                using var extractor = new ArchiveExtractor();
                await extractor.ExtractAsync(zipPath, extractDir, ct);

                string dllSource = Path.Combine(extractDir, ConstantsInstallerPaths.ManagedDllFileName);

                if (!File.Exists(dllSource))
                    throw new FileNotFoundException("sherpa-onnx.dll not found in zip.", dllSource);

                string destDir = Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, "iOS");
                Directory.CreateDirectory(destDir);

                string destPath = Path.Combine(destDir, ConstantsInstallerPaths.ManagedDllFileName);
                File.Copy(dllSource, destPath, overwrite: true);

                SherpaOnnxLog.EditorLog("[SherpaOnnx] iOS managed DLL installed.");
            }
            finally
            {
                FileSystemHelper.TryDeleteDirectory(tempDir);
            }
        }

        internal static string BuildUrl(LibraryArch arch, string version)
        {
            return string.Format(arch.Url, version, version);
        }

        internal static string BuildFileName(LibraryArch arch)
        {
            if (IsAndroid(arch))
                return "sherpa-onnx-android.tar.bz2";

            if (IsIOS(arch))
                return "sherpa-onnx-ios.tar.bz2";

            return arch.Name + ".nupkg";
        }
    }
}
