using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers;
using UnityEditor;

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
                    throw new InvalidOperationException("jniLibs directory not found in Android cache.");

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
        /// iOS-specific install: downloads our unified sherpa-onnx-ios.zip
        /// (DLL + xcframeworks) and installs from cache.
        /// </summary>
        internal static async Task RuniOSInstallAsync(
            LibraryArch arch,
            string version,
            CancellationToken ct,
            Action<string> onStatus = null,
            Action<float> onProgress = null)
        {
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] iOS install started: v{version}");

            IArchiveCache cache = iOSArchiveCache.Cache;
            SubscribeCache(cache, onStatus, onProgress);
            try
            {
                string url = BuildUrl(arch, version);
                string fileName = BuildFileName(arch);

                await iOSArchiveCache.EnsureExtractedAsync(url, fileName, ct);

                InstallIosFromCache(ct);

                SherpaOnnxLog.EditorLog("[SherpaOnnx] iOS install completed.");
            }
            finally
            {
                UnsubscribeCache(cache, onStatus, onProgress);
            }
        }

        /// <summary>
        /// Re-installs iOS xcframeworks + DLL from cache using current settings.
        /// Called when simulator/macOS toggles change. No download needed.
        /// </summary>
        internal static void ReinstallIosFromCache()
        {
            if (!iOSArchiveCache.IsReady)
                return;

            InstallIosFromCache(CancellationToken.None);

            AssetDatabase.Refresh();

            foreach (var platform in LibraryPlatforms.Platforms)
                foreach (var arch in platform.Arches)
                    if (arch.Platform == PlatformType.iOS)
                        PluginImportConfigurator.Configure(arch);
        }

        private static void InstallIosFromCache(CancellationToken ct)
        {
            string cachePath = iOSArchiveCache.CachePath;
            var s = SherpaOnnxProjectSettings.instance;

            var handler = new iOSNativeContentHandler(s.iosIncludeSimulator, s.iosIncludeMac);
            handler.HandleAsync(cachePath, ct).GetAwaiter().GetResult();

            CopyIosManagedDll(cachePath);
        }

        private static void CopyIosManagedDll(string cachePath)
        {
            string[] files = Directory.GetFiles(
                cachePath, ConstantsInstallerPaths.ManagedDllFileName,
                SearchOption.AllDirectories);

            if (files.Length == 0)
                throw new FileNotFoundException("sherpa-onnx.dll not found in iOS cache.");

            string destDir = Path.Combine(ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, "iOS");
            Directory.CreateDirectory(destDir);

            string destPath = Path.Combine(destDir, ConstantsInstallerPaths.ManagedDllFileName);
            File.Copy(files[0], destPath, overwrite: true);

            SherpaOnnxLog.EditorLog("[SherpaOnnx] iOS managed DLL installed from cache.");
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

        internal static string BuildUrl(LibraryArch arch, string version)
        {
            return string.Format(arch.Url, version, version);
        }

        internal static string BuildFileName(LibraryArch arch)
        {
            if (IsAndroid(arch))
                return "sherpa-onnx-android.tar.bz2";

            if (IsIOS(arch))
                return "sherpa-onnx-ios.zip";

            return arch.Name + ".nupkg";
        }
    }
}
