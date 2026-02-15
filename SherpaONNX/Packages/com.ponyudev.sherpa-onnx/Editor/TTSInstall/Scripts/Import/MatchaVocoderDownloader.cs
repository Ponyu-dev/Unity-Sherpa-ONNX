using System;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Networking;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Downloads a selected Matcha vocoder .onnx file into the model directory.
    /// </summary>
    internal sealed class MatchaVocoderDownloader : IDisposable
    {
        private readonly UnityWebRequestFileDownloader _downloader;

        internal event Action<float> OnProgress;
        internal event Action<string> OnStatus;

        internal MatchaVocoderDownloader()
        {
            _downloader = new UnityWebRequestFileDownloader();
            _downloader.OnProgress += HandleProgress;
        }

        /// <summary>
        /// Downloads the vocoder file into <paramref name="modelDir"/>.
        /// Returns the vocoder file name (e.g. "vocos-22khz-univ.onnx").
        /// </summary>
        internal async Task<string> DownloadAsync(
            MatchaVocoderOption option,
            string modelDir,
            CancellationToken ct)
        {
            string url = option.GetUrl();
            string fileName = option.GetFileName();

            OnStatus?.Invoke($"Downloading vocoder: {fileName}...");

            await _downloader.DownloadAsync(url, modelDir, fileName, ct);
            ct.ThrowIfCancellationRequested();

            OnStatus?.Invoke($"Vocoder downloaded: {fileName}");
            return fileName;
        }

        public void Dispose()
        {
            _downloader.OnProgress -= HandleProgress;
            _downloader.Dispose();
        }

        private void HandleProgress(string url, float progress, ulong downloaded, long total)
        {
            OnProgress?.Invoke(progress);
        }
    }
}
