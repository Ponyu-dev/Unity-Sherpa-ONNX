using System.IO;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Constants for ASR model storage paths under StreamingAssets.
    /// </summary>
    internal static class AsrModelPaths
    {
        internal const string StreamingAssetsRoot = "Assets/StreamingAssets";
        internal const string AsrModelsRelative = "SherpaOnnx/asr-models";
        internal const string AsrModelsDir =
            StreamingAssetsRoot + "/" + AsrModelsRelative;

        internal static string GetModelDir(string archiveName)
        {
            return Path.Combine(AsrModelsDir, archiveName);
        }
    }
}
