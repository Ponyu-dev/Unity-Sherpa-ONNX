namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Available vocoder models for Matcha TTS.
    /// Each option maps to a downloadable .onnx file.
    /// </summary>
    internal enum MatchaVocoderOption
    {
        Vocos22khz,
        HifiganV1,
        HifiganV2,
        HifiganV3
    }

    /// <summary>
    /// Maps <see cref="MatchaVocoderOption"/> to download URLs and file names.
    /// </summary>
    internal static class MatchaVocoderOptionExtensions
    {
        private const string BaseUrl =
            "https://github.com/k2-fsa/sherpa-onnx/releases/download/vocoder-models/";

        internal static string GetUrl(this MatchaVocoderOption option)
        {
            return BaseUrl + GetFileName(option);
        }

        internal static string GetFileName(this MatchaVocoderOption option)
        {
            switch (option)
            {
                case MatchaVocoderOption.Vocos22khz: return "vocos-22khz-univ.onnx";
                case MatchaVocoderOption.HifiganV1:  return "hifigan_v1.onnx";
                case MatchaVocoderOption.HifiganV2:  return "hifigan_v2.onnx";
                case MatchaVocoderOption.HifiganV3:  return "hifigan_v3.onnx";
                default:                             return "vocos-22khz-univ.onnx";
            }
        }

        internal static string GetDisplayName(this MatchaVocoderOption option)
        {
            switch (option)
            {
                case MatchaVocoderOption.Vocos22khz: return "Vocos 22kHz (recommended)";
                case MatchaVocoderOption.HifiganV1:  return "HiFi-GAN v1";
                case MatchaVocoderOption.HifiganV2:  return "HiFi-GAN v2";
                case MatchaVocoderOption.HifiganV3:  return "HiFi-GAN v3";
                default:                             return option.ToString();
            }
        }
    }
}
