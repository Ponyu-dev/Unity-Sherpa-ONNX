using PonyuDev.SherpaOnnx.Asr.Offline.Data;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Detects <see cref="AsrModelType"/> from archive name using
    /// URL heuristics. Returns null when the type cannot be determined.
    /// </summary>
    internal static class AsrModelTypeDetector
    {
        /// <summary>
        /// Tries to detect offline ASR model type from archive name.
        /// </summary>
        internal static AsrModelType? Detect(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            if (lower.Contains("whisper"))
                return AsrModelType.Whisper;

            if (lower.Contains("paraformer"))
                return AsrModelType.Paraformer;

            if (lower.Contains("sense-voice") || lower.Contains("sensevoice"))
                return AsrModelType.SenseVoice;

            if (lower.Contains("moonshine"))
                return AsrModelType.Moonshine;

            if (lower.Contains("fire-red") || lower.Contains("firered"))
                return AsrModelType.FireRedAsr;

            if (lower.Contains("dolphin"))
                return AsrModelType.Dolphin;

            if (lower.Contains("canary"))
                return AsrModelType.Canary;

            if (lower.Contains("wenet"))
                return AsrModelType.WenetCtc;

            if (lower.Contains("omnilingual"))
                return AsrModelType.Omnilingual;

            if (lower.Contains("med-asr") || lower.Contains("medasr"))
                return AsrModelType.MedAsr;

            if (lower.Contains("fun-asr") || lower.Contains("funasrnano"))
                return AsrModelType.FunAsrNano;

            if (lower.Contains("tdnn"))
                return AsrModelType.Tdnn;

            if (lower.Contains("nemo") && lower.Contains("ctc"))
                return AsrModelType.NemoCtc;

            if (lower.Contains("zipformer") && lower.Contains("ctc"))
                return AsrModelType.ZipformerCtc;

            if (lower.Contains("transducer") || lower.Contains("zipformer"))
                return AsrModelType.Transducer;

            return null;
        }

        /// <summary>
        /// Fallback: detect from file names inside the model directory.
        /// </summary>
        internal static AsrModelType? DetectFromFiles(string modelDir)
        {
            string[] onnxFiles = Common.ModelFileScanner
                .GetOnnxFileNames(modelDir);
            if (onnxFiles.Length == 0) return null;

            string joined = string.Join(" ",
                onnxFiles).ToLowerInvariant();

            if (joined.Contains("whisper"))
                return AsrModelType.Whisper;

            if (joined.Contains("paraformer"))
                return AsrModelType.Paraformer;

            if (joined.Contains("sense"))
                return AsrModelType.SenseVoice;

            if (joined.Contains("moonshine"))
                return AsrModelType.Moonshine;

            if (joined.Contains("joiner"))
                return AsrModelType.Transducer;

            return null;
        }
    }
}
