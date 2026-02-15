using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Builds the Matcha vocoder dropdown for the import UI
    /// and handles vocoder download after archive extraction.
    /// </summary>
    internal sealed class MatchaVocoderImportField
    {
        private PopupField<MatchaVocoderOption> _field;

        internal VisualElement Build()
        {
            var choices = new List<MatchaVocoderOption>
            {
                MatchaVocoderOption.Vocos22khz,
                MatchaVocoderOption.HifiganV1,
                MatchaVocoderOption.HifiganV2,
                MatchaVocoderOption.HifiganV3
            };

            _field = new PopupField<MatchaVocoderOption>(
                "Vocoder", choices, MatchaVocoderOption.Vocos22khz,
                MatchaVocoderOptionExtensions.GetDisplayName,
                MatchaVocoderOptionExtensions.GetDisplayName);

            _field.style.display = DisplayStyle.None;
            return _field;
        }

        internal void SetVisible(bool visible)
        {
            if (_field == null) return;
            _field.style.display = visible
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Downloads the selected vocoder into <paramref name="modelDir"/>
        /// and writes the file name into <paramref name="profile"/>.
        /// </summary>
        internal async Task DownloadAsync(
            TtsProfile profile,
            string modelDir,
            Action<float> onProgress,
            Action<string> onStatus,
            CancellationToken ct)
        {
            MatchaVocoderOption option = _field?.value
                ?? MatchaVocoderOption.Vocos22khz;

            using var downloader = new MatchaVocoderDownloader();
            downloader.OnProgress += onProgress;
            downloader.OnStatus += onStatus;

            try
            {
                string fileName = await downloader.DownloadAsync(option, modelDir, ct);
                profile.matchaVocoder = fileName;
            }
            finally
            {
                downloader.OnProgress -= onProgress;
                downloader.OnStatus -= onStatus;
            }
        }
    }
}
