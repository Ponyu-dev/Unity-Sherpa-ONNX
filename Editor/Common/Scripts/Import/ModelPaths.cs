using System.IO;

namespace PonyuDev.SherpaOnnx.Editor.Common.Import
{
    /// <summary>
    /// Shared constants and helpers for model storage paths
    /// under StreamingAssets.
    /// </summary>
    internal static class ModelPaths
    {
        internal const string StreamingAssetsRoot = "Assets/StreamingAssets";
        private const string SherpaRoot = StreamingAssetsRoot + "/SherpaOnnx";

        internal const string TtsModelsDir = SherpaRoot + "/tts-models";
        internal const string AsrModelsDir = SherpaRoot + "/asr-models";
        internal const string VadModelsDir = SherpaRoot + "/vad-models";

        internal static string GetTtsModelDir(string name)
        {
            return Path.Combine(TtsModelsDir, name);
        }

        internal static string GetAsrModelDir(string name)
        {
            return Path.Combine(AsrModelsDir, name);
        }

        internal static string GetVadModelDir(string name)
        {
            return Path.Combine(VadModelsDir, name);
        }
    }
}
