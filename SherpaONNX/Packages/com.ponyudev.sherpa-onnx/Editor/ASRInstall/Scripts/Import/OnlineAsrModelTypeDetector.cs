using PonyuDev.SherpaOnnx.Asr.Online.Data;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Detects <see cref="OnlineAsrModelType"/> from archive name
    /// using URL heuristics. Returns null when unknown.
    /// </summary>
    internal static class OnlineAsrModelTypeDetector
    {
        internal static OnlineAsrModelType? Detect(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            if (lower.Contains("paraformer"))
                return OnlineAsrModelType.Paraformer;

            if (lower.Contains("nemo") && lower.Contains("ctc"))
                return OnlineAsrModelType.NemoCtc;

            if (lower.Contains("tone") && lower.Contains("ctc"))
                return OnlineAsrModelType.ToneCtc;

            if (lower.Contains("zipformer2") && lower.Contains("ctc"))
                return OnlineAsrModelType.Zipformer2Ctc;

            if (lower.Contains("transducer")
                || lower.Contains("zipformer"))
                return OnlineAsrModelType.Transducer;

            return null;
        }

        internal static OnlineAsrModelType? DetectFromFiles(
            string modelDir)
        {
            string[] onnxFiles = Common.ModelFileScanner
                .GetOnnxFileNames(modelDir);
            if (onnxFiles.Length == 0) return null;

            string joined = string.Join(" ",
                onnxFiles).ToLowerInvariant();

            if (joined.Contains("joiner"))
                return OnlineAsrModelType.Transducer;

            if (joined.Contains("paraformer"))
                return OnlineAsrModelType.Paraformer;

            return null;
        }
    }
}
