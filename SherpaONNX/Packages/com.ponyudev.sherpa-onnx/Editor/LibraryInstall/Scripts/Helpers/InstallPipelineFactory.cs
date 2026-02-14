using System;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers;

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
            return arch.Url != null && arch.Url.Contains("android.tar.bz2");
        }

        internal static bool IsIOS(LibraryArch arch)
        {
            return arch.Url != null && arch.Url.Contains("ios.tar.bz2");
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
            CancellationToken ct)
        {
            string url = BuildUrl(arch, version);
            string fileName = BuildFileName(arch);

            await AndroidArchiveCache.EnsureExtractedAsync(url, fileName, ct);

            string jniLibsPath = AndroidArchiveCache.FindJniLibsPath();

            if (string.IsNullOrEmpty(jniLibsPath))
                throw new InvalidOperationException("jniLibs directory not found in Android cache.");

            var handler = new AndroidNativeContentHandler(arch.Name);
            await handler.HandleAsync(jniLibsPath, ct);
        }

        /// <summary>
        /// iOS-specific install: uses shared cache, does not re-download
        /// if archive is already extracted.
        /// </summary>
        internal static async Task RuniOSInstallAsync(
            LibraryArch arch,
            string version,
            CancellationToken ct)
        {
            string url = BuildUrl(arch, version);
            string fileName = BuildFileName(arch);

            await iOSArchiveCache.EnsureExtractedAsync(url, fileName, ct);

            string buildIosPath = iOSArchiveCache.FindBuildIosPath();

            if (string.IsNullOrEmpty(buildIosPath))
                throw new InvalidOperationException("build-ios directory not found in iOS cache.");

            var handler = new iOSNativeContentHandler(arch.Name);
            await handler.HandleAsync(buildIosPath, ct);
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
