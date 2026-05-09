using System.Collections.Generic;
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
    /// Files under <c>{type}-models/{profile}/</c> subfolders are grouped
    /// per profile so the runtime can extract / delete each profile
    /// independently.
    /// </summary>
    internal sealed class StreamingAssetsManifestBuilder : IPreprocessBuildWithReport
    {
        private const string SherpaOnnxDir = "Assets/StreamingAssets/SherpaOnnx";
        private const string ManifestPath = SherpaOnnxDir + "/streaming-assets-manifest.json";
        private const string StreamingAssetsRoot = "Assets/StreamingAssets/";
        private const string SherpaOnnxRel = "SherpaOnnx/";

        // Subfolders under SherpaOnnx/ whose direct children are profile
        // directories. Files under "SherpaOnnx/{prefix}/{profileName}/..."
        // are grouped together; everything else is shared.
        private static readonly string[] ProfileRoots =
        {
            "tts-models",
            "asr-models",
            "vad-models",
        };

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
            var fileEntries = filtered
                .Select(f => new
                {
                    Relative = f
                        .Substring(StreamingAssetsRoot.Length)
                        .Replace('\\', '/'),
                    Length = new FileInfo(f).Length,
                })
                .OrderBy(e => e.Relative)
                .ToList();

            long totalSize = fileEntries.Sum(e => e.Length);
            string version = $"{fileEntries.Count}_{totalSize}";

            // Bucket files into shared vs per-profile groups.
            var shared = new List<string>();
            var groups = new Dictionary<string, StreamingAssetsManifestProfileGroup>();

            foreach (var entry in fileEntries)
            {
                string subdir = TryGetProfileSubdir(entry.Relative);
                if (subdir == null)
                {
                    shared.Add(entry.Relative);
                    continue;
                }

                if (!groups.TryGetValue(subdir, out var group))
                {
                    group = new StreamingAssetsManifestProfileGroup { subdir = subdir };
                    groups[subdir] = group;
                }
                group.files.Add(entry.Relative);
                group.sizeBytes += entry.Length;
            }

            var profileGroups = groups.Values
                .OrderBy(g => g.subdir)
                .ToList();

            var manifest = new StreamingAssetsManifest
            {
                version = version,
                totalSizeBytes = totalSize,
                shared = shared,
                profileGroups = profileGroups,
                // Legacy flat list left empty — runtime falls back to it
                // only when profileGroups is empty (older manifests).
                files = new List<string>(),
            };

            string json = JsonUtility.ToJson(manifest, true);

            Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
            File.WriteAllText(ManifestPath, json);

            AssetDatabase.Refresh();

            Debug.Log(
                $"[SherpaOnnx] Manifest generated: " +
                $"{fileEntries.Count} files ({shared.Count} shared, " +
                $"{profileGroups.Count} profile groups), version={version}");
        }

        /// <summary>
        /// Returns <c>"{prefix}/{profileName}"</c> if the given relative
        /// path lives under one of <see cref="ProfileRoots"/>, otherwise
        /// <c>null</c>. The path is expected to start with
        /// <c>"SherpaOnnx/"</c>.
        /// </summary>
        private static string TryGetProfileSubdir(string relativePath)
        {
            if (!relativePath.StartsWith(SherpaOnnxRel))
                return null;

            string afterSherpa = relativePath.Substring(SherpaOnnxRel.Length);
            int firstSlash = afterSherpa.IndexOf('/');
            if (firstSlash <= 0)
                return null;

            string prefix = afterSherpa.Substring(0, firstSlash);
            bool isProfileRoot = false;
            for (int i = 0; i < ProfileRoots.Length; i++)
            {
                if (ProfileRoots[i] == prefix)
                {
                    isProfileRoot = true;
                    break;
                }
            }
            if (!isProfileRoot)
                return null;

            string afterPrefix = afterSherpa.Substring(firstSlash + 1);
            int secondSlash = afterPrefix.IndexOf('/');
            if (secondSlash <= 0)
                return null;   // file directly under {prefix}/, not in a profile

            string profileName = afterPrefix.Substring(0, secondSlash);
            return $"{prefix}/{profileName}";
        }
    }
}