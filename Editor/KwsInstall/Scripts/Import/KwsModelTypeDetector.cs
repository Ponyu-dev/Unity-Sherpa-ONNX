using PonyuDev.SherpaOnnx.Kws.Data;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Import
{
    /// <summary>
    /// Detects <see cref="KwsModelType"/> from archive name using URL heuristics.
    /// Currently all pretrained KWS models are Zipformer Transducers.
    /// Returns null when the type cannot be determined.
    /// </summary>
    internal static class KwsModelTypeDetector
    {
        internal static KwsModelType? Detect(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                return null;

            string lower = archiveName.ToLowerInvariant();

            if (lower.Contains("kws"))
                return KwsModelType.Transducer;

            if (lower.Contains("zipformer"))
                return KwsModelType.Transducer;

            return null;
        }
    }
}
