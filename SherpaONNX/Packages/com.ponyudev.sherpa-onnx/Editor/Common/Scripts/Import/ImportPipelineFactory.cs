using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;

namespace PonyuDev.SherpaOnnx.Editor.Common.Import
{
    /// <summary>
    /// Assembles a <see cref="PackageInstallPipeline"/>
    /// for downloading and extracting model archives.
    /// </summary>
    internal static class ImportPipelineFactory
    {
        internal static PackageInstallPipeline Create(
            IExtractedContentHandler handler)
        {
            var downloader = new UnityWebRequestFileDownloader();
            var extractor = new ArchiveExtractor();
            var tempFactory = new TempDirectoryFactory();

            return new PackageInstallPipeline(
                downloader, extractor, handler, tempFactory);
        }
    }
}
