using PonyuDev.SherpaOnnx.Common.Extractors;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Common.Networking;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Assembles a <see cref="PackageInstallPipeline"/>
    /// configured for downloading and extracting TTS model archives.
    /// </summary>
    internal static class TtsImportPipelineFactory
    {
        internal static PackageInstallPipeline Create(TtsModelContentHandler handler)
        {
            var downloader = new UnityWebRequestFileDownloader();
            var extractor = new ArchiveExtractor();
            var tempFactory = new TempDirectoryFactory();

            return new PackageInstallPipeline(
                downloader, extractor, handler, tempFactory);
        }
    }
}
