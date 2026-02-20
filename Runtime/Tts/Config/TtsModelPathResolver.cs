using System.IO;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Config
{
    /// <summary>
    /// Resolves relative model file paths from TtsProfile
    /// into absolute paths. On Android resolves to persistentDataPath
    /// (after extraction), on other platforms to StreamingAssets.
    /// </summary>
    public static class TtsModelPathResolver
    {
        private const string TtsModelsFolder = "SherpaOnnx/tts-models";

        /// <summary>
        /// Returns the absolute directory for a given profile name.
        /// Desktop: {StreamingAssets}/SherpaOnnx/tts-models/{profileName}
        /// Android: {persistentDataPath}/SherpaOnnx/tts-models/{profileName}
        /// </summary>
        public static string GetModelDirectory(string profileName)
        {
            return Path.Combine(
                StreamingAssetsCopier.GetResolvedStreamingAssetsPath(),
                TtsModelsFolder,
                profileName);
        }

        /// <summary>
        /// Resolves a relative file path to an absolute path
        /// within the model directory. Returns empty string if
        /// the relative path is null or empty.
        /// </summary>
        public static string Resolve(string modelDir, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return "";

            return Path.Combine(modelDir, relativePath);
        }
    }
}
