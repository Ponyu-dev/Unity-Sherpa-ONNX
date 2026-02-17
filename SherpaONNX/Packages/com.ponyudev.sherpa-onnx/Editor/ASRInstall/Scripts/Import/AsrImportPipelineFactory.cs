using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Assembles a <see cref="PackageInstallPipeline"/> configured
    /// for downloading and extracting ASR model archives.
    /// </summary>
    internal static class AsrImportPipelineFactory
    {
        internal static PackageInstallPipeline Create(
            AsrModelContentHandler handler)
        {
            var downloader = new UnityWebRequestFileDownloader();
            var extractor = new ArchiveExtractor();
            var tempFactory = new TempDirectoryFactory();

            return new PackageInstallPipeline(
                downloader, extractor, handler, tempFactory);
        }
    }
}
