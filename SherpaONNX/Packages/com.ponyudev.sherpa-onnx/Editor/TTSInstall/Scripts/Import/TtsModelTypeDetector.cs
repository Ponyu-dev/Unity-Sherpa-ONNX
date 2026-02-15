using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Detects <see cref="TtsModelType"/> from archive name using URL heuristics.
    /// Returns null when the type cannot be determined.
    /// </summary>
    internal static class TtsModelTypeDetector
    {
        /// <summary>
        /// Tries to detect model type from the archive name (URL stem).
        /// </summary>
        /// <param name="archiveName">
        /// Archive name without extension, e.g. "vits-piper-en_US-amy-low".
        /// </param>
        /// <returns>Detected type or null if unknown.</returns>
        internal static TtsModelType? Detect(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            if (lower.StartsWith("vits-"))
                return TtsModelType.Vits;

            if (lower.StartsWith("matcha-"))
                return TtsModelType.Matcha;

            if (lower.StartsWith("kokoro-"))
                return TtsModelType.Kokoro;

            if (lower.Contains("kitten"))
                return TtsModelType.Kitten;

            if (lower.Contains("zipformer") ||
                lower.Contains("zip-voice") ||
                lower.Contains("zipvoice"))
                return TtsModelType.ZipVoice;

            if (lower.Contains("pocket"))
                return TtsModelType.Pocket;

            return null;
        }
    }
}
