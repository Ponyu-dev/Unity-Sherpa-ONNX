using System;
using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Creates a configured PackageInstallPipeline for the given LibraryArch.
    /// Caller is responsible for disposing the returned pipeline.
    /// </summary>
    internal static class InstallPipelineFactory
    {
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

        internal static string BuildUrl(LibraryArch arch, string version)
        {
            return string.Format(arch.Url, version, version);
        }

        internal static string BuildFileName(LibraryArch arch)
        {
            return arch.Name + ".nupkg";
        }
    }
}
