using System.IO;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Asr.Config
{
    /// <summary>
    /// Resolves relative model file paths from AsrProfile
    /// into absolute paths. On Android resolves to persistentDataPath
    /// (after extraction), on other platforms to StreamingAssets.
    /// </summary>
    public static class AsrModelPathResolver
    {
        internal const string ModelsSubfolder = "asr-models";
        private const string AsrModelsFolder = "SherpaOnnx/" + ModelsSubfolder;

        /// <summary>
        /// Returns the absolute directory for a given profile name.
        /// Desktop: {StreamingAssets}/SherpaOnnx/asr-models/{profileName}
        /// Android: {persistentDataPath}/SherpaOnnx/asr-models/{profileName}
        /// </summary>
        public static string GetModelDirectory(string profileName)
        {
            string path = Path.Combine(
                StreamingAssetsCopier.GetResolvedStreamingAssetsPath(),
                AsrModelsFolder,
                profileName);
            return NativePathSanitizer.Sanitize(path);
        }

        /// <summary>
        /// Returns the model directory based on <paramref name="source"/>.
        /// For <see cref="ModelSource.LocalZip"/> returns the extracted
        /// directory under persistentDataPath.
        ///
        /// In Editor every source resolves to the StreamingAssets path,
        /// matching <see cref="ProfileSourceResolver"/>'s "non-Local =
        /// Local in Editor" rule — no LocalZip extraction in Editor,
        /// so the persistentDataPath copy never exists.
        /// </summary>
        public static string GetModelDirectory(string profileName, ModelSource source)
        {
            if (Application.isEditor)
                return GetModelDirectory(profileName);

            if (source == ModelSource.LocalZip)
                return LocalZipExtractor.GetExtractedModelDirectory(ModelsSubfolder, profileName);

            return GetModelDirectory(profileName);
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
