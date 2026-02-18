using System.IO;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Import
{
    /// <summary>
    /// Constants for VAD model storage paths under StreamingAssets.
    /// </summary>
    internal static class VadModelPaths
    {
        internal const string StreamingAssetsRoot = "Assets/StreamingAssets";
        internal const string VadModelsRelative = "SherpaOnnx/vad-models";
        internal const string VadModelsDir = StreamingAssetsRoot + "/" + VadModelsRelative;

        internal static string GetModelDir(string archiveName)
        {
            return Path.Combine(VadModelsDir, archiveName);
        }
    }
}
