using System.IO;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Config
{
    /// <summary>
    /// Resolves relative model file paths from TtsProfile
    /// into absolute paths under StreamingAssets.
    /// </summary>
    public static class TtsModelPathResolver
    {
        private const string TtsModelsFolder = "SherpaOnnx/tts-models";

        /// <summary>
        /// Returns the absolute directory for a given profile name.
        /// e.g. {StreamingAssets}/SherpaOnnx/tts-models/{profileName}
        /// </summary>
        public static string GetModelDirectory(string profileName)
        {
            return Path.Combine(
                Application.streamingAssetsPath,
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
