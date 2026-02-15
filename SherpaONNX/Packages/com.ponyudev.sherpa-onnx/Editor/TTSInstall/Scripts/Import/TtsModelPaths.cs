using System.IO;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Constants for TTS model storage paths under StreamingAssets.
    /// </summary>
    internal static class TtsModelPaths
    {
        internal const string StreamingAssetsRoot = "Assets/StreamingAssets";
        internal const string TtsModelsRelative = "SherpaOnnx/tts-models";
        internal const string TtsModelsDir = StreamingAssetsRoot + "/" + TtsModelsRelative;

        internal static string GetModelDir(string archiveName)
        {
            return Path.Combine(TtsModelsDir, archiveName);
        }
    }
}
