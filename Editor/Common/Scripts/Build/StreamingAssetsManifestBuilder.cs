using System.IO;
using System.Linq;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.Common.Build
{
    /// <summary>
    /// Generates streaming-assets-manifest.json before each build.
    /// The manifest lists every file under StreamingAssets/SherpaOnnx/
    /// so <see cref="StreamingAssetsCopier"/> can copy them on Android.
    /// </summary>
    internal sealed class StreamingAssetsManifestBuilder : IPreprocessBuildWithReport
    {
        private const string SherpaOnnxDir = "Assets/StreamingAssets/SherpaOnnx";
        private const string ManifestPath = SherpaOnnxDir + "/streaming-assets-manifest.json";
        private const string StreamingAssetsRoot = "Assets/StreamingAssets/";

        // Run after all LocalZipBuildProcessors (100–102)
        // so the manifest reflects zipped directories.
        public int callbackOrder => 110;

        public void OnPreprocessBuild(BuildReport report)
        {
            RebuildManifest();
        }

        [MenuItem("Tools/SherpaOnnx/Rebuild StreamingAssets Manifest")]
        internal static void RebuildManifest()
        {
            if (!Directory.Exists(SherpaOnnxDir))
            {
                Debug.LogWarning($"[SherpaOnnx] Directory not found: {SherpaOnnxDir}. Manifest not generated.");
                return;
            }

            string[] allFiles = Directory.GetFiles(SherpaOnnxDir, "*", SearchOption.AllDirectories);

            // Exclude .meta, .DS_Store, and the manifest itself.
            var filtered = allFiles
                .Where(f => !f.EndsWith(".meta"))
                .Where(f => !f.EndsWith(".DS_Store"))
                .Where(f => !f.EndsWith("streaming-assets-manifest.json"))
                .ToList();

            // Convert to relative paths from StreamingAssets root.
            // Use forward slashes for cross-platform compatibility.
            var relativePaths = filtered
                .Select(f => f
                    .Substring(StreamingAssetsRoot.Length)
                    .Replace('\\', '/'))
                .OrderBy(p => p)
                .ToList();

            // Fingerprint: fileCount_totalSizeBytes.
            long totalSize = filtered.Sum(f => new FileInfo(f).Length);
            string version = $"{relativePaths.Count}_{totalSize}";

            var manifest = new StreamingAssetsManifest
            {
                version = version,
                totalSizeBytes = totalSize,
                files = relativePaths,
            };

            string json = JsonUtility.ToJson(manifest, true);

            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
            File.WriteAllText(ManifestPath, json);

            AssetDatabase.Refresh();

            Debug.Log($"[SherpaOnnx] Manifest generated: {relativePaths.Count} files, version={version}");
        }
    }
}