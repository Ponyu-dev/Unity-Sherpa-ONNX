using PonyuDev.SherpaOnnx.Vad.Data;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Import
{
    /// <summary>
    /// Detects <see cref="VadModelType"/> from archive name using URL heuristics.
    /// Returns null when the type cannot be determined.
    /// </summary>
    internal static class VadModelTypeDetector
    {
        /// <summary>
        /// Tries to detect VAD model type from the archive name (URL stem).
        /// </summary>
        /// <param name="archiveName">
        /// Archive name without extension, e.g. "silero_vad".
        /// </param>
        /// <returns>Detected type or null if unknown.</returns>
        internal static VadModelType? Detect(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            if (lower.Contains("silero"))
                return VadModelType.SileroVad;

            if (lower.Contains("ten"))
                return VadModelType.TenVad;

            return null;
        }
    }
}
