using System;
using System.Collections.Generic;
using System.IO;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.Common.Build
{
    /// <summary>
    /// Honours the per-service "Only active profile in build" toggle.
    /// On preprocess (callback order 105 — after every
    /// <c>LocalZipBuildProcessor</c> at 100..102, before
    /// <c>StreamingAssetsManifestBuilder</c> at 110), every non-active
    /// profile's model directory and any LocalZip archive produced by
    /// <see cref="LocalZipBuildHelper"/> is moved out of StreamingAssets
    /// into a per-build staging directory under <c>Library/</c>. After
    /// the build runs, postprocess restores folders back; transient zip
    /// archives are discarded because <see cref="LocalZipBuildHelper"/>
    /// has already restored the original folder from its own temp-cache
    /// backup.
    ///
    /// A defensive <c>InitializeOnLoad</c> recovery handles the case
    /// where the build was cancelled or the Editor crashed between
    /// preprocess and postprocess: when stale staging is detected on
    /// Editor startup every staged item is moved back into
    /// StreamingAssets so the project never permanently loses model
    /// files.
    /// </summary>
    internal sealed class OnlyActiveProfileBuildProcessor
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string StagingRoot =
            "Library/SherpaOnnx-build-staging";
        private const string ManifestFileName = "staging-manifest.json";
        private const string KindFolder = "folder";
        private const string KindZip = "zip";

        // Run after Local-zip build processors (100..102) so we can move
        // the just-zipped non-active archives, and before the manifest
        // builder (110) so the manifest reflects only the active profile.
        public int callbackOrder => 105;

        public void OnPreprocessBuild(BuildReport report)
        {
            var items = new List<StagingItem>();

            CollectTtsExcludes(items);
            CollectAsrOfflineExcludes(items);
            CollectAsrOnlineExcludes(items);
            CollectVadExcludes(items);

            if (items.Count == 0)
                return;

            Directory.CreateDirectory(StagingRoot);

            foreach (StagingItem item in items)
                MoveToStaging(item);

            SaveManifest(items);

            AssetDatabase.Refresh();

            Debug.Log(
                $"[SherpaOnnx] Moved {items.Count} non-active profile " +
                $"item(s) to {StagingRoot}. Will restore after build.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanupAfterBuild();
        }

        [InitializeOnLoadMethod]
        private static void RestoreOnEditorLoad()
        {
            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
                return;

            Debug.LogWarning(
                "[SherpaOnnx] Stale build-staging manifest detected at " +
                $"{manifestPath}. The previous build did not run its " +
                "postprocess hook (build cancelled, Editor crash, or " +
                "domain reload during build). Restoring profile files now.");

            RestoreAll();
        }

        // ── Per-service collection ──

        private static void CollectTtsExcludes(List<StagingItem> output)
        {
            var data = TtsProjectSettings.instance.data;
            if (!data.buildOnlyActiveProfile)
                return;

            int active = data.activeProfileIndex;
            int count = data.profiles.Count;
            if (count == 0)
                return;

            if (!TryGetActiveName(
                "TTS", active, count,
                i => data.profiles[i]?.profileName,
                out string activeName))
                return;

            for (int i = 0; i < count; i++)
            {
                string name = data.profiles[i]?.profileName;
                if (string.IsNullOrEmpty(name) || name == activeName)
                    continue;

                AppendExcludedItems(
                    name, ModelPaths.GetTtsModelDir, "tts-models", output);
            }
        }

        private static void CollectAsrOfflineExcludes(List<StagingItem> output)
        {
            var data = AsrProjectSettings.instance.offlineData;
            if (!data.buildOnlyActiveProfile)
                return;

            int active = data.activeProfileIndex;
            int count = data.profiles.Count;
            if (count == 0)
                return;

            if (!TryGetActiveName(
                "ASR (offline)", active, count,
                i => data.profiles[i]?.profileName,
                out string activeName))
                return;

            for (int i = 0; i < count; i++)
            {
                string name = data.profiles[i]?.profileName;
                if (string.IsNullOrEmpty(name) || name == activeName)
                    continue;

                AppendExcludedItems(
                    name, ModelPaths.GetAsrModelDir, "asr-models", output);
            }
        }

        private static void CollectAsrOnlineExcludes(List<StagingItem> output)
        {
            var data = AsrProjectSettings.instance.onlineData;
            if (!data.buildOnlyActiveProfile)
                return;

            int active = data.activeProfileIndex;
            int count = data.profiles.Count;
            if (count == 0)
                return;

            if (!TryGetActiveName(
                "ASR (online)", active, count,
                i => data.profiles[i]?.profileName,
                out string activeName))
                return;

            for (int i = 0; i < count; i++)
            {
                string name = data.profiles[i]?.profileName;
                if (string.IsNullOrEmpty(name) || name == activeName)
                    continue;

                AppendExcludedItems(
                    name, ModelPaths.GetAsrModelDir, "asr-models", output);
            }
        }

        private static void CollectVadExcludes(List<StagingItem> output)
        {
            var data = VadProjectSettings.instance.data;
            if (!data.buildOnlyActiveProfile)
                return;

            int active = data.activeProfileIndex;
            int count = data.profiles.Count;
            if (count == 0)
                return;

            if (!TryGetActiveName(
                "VAD", active, count,
                i => data.profiles[i]?.profileName,
                out string activeName))
                return;

            for (int i = 0; i < count; i++)
            {
                string name = data.profiles[i]?.profileName;
                if (string.IsNullOrEmpty(name) || name == activeName)
                    continue;

                AppendExcludedItems(
                    name, ModelPaths.GetVadModelDir, "vad-models", output);
            }
        }

        private static bool TryGetActiveName(
            string serviceName,
            int activeIndex,
            int profileCount,
            Func<int, string> getName,
            out string activeName)
        {
            activeName = null;

            if (activeIndex < 0 || activeIndex >= profileCount)
            {
                Debug.LogWarning(
                    $"[SherpaOnnx] {serviceName}: 'Only active profile in " +
                    "build' is ON but no active profile is set. Skipping " +
                    "the filter for this service — every profile will ship.");
                return false;
            }

            string name = getName(activeIndex);
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning(
                    $"[SherpaOnnx] {serviceName}: 'Only active profile in " +
                    "build' is ON but the active profile has no name. " +
                    "Skipping the filter for this service.");
                return false;
            }

            activeName = name;
            return true;
        }

        private static void AppendExcludedItems(
            string profileName,
            Func<string, string> getModelDir,
            string subdirPrefix,
            List<StagingItem> output)
        {
            string modelDir = getModelDir(profileName);
            string zipPath = modelDir + ".zip";

            if (Directory.Exists(modelDir))
            {
                output.Add(new StagingItem
                {
                    kind = KindFolder,
                    originalPath = modelDir,
                    stagingPath = Path.Combine(
                        StagingRoot, subdirPrefix, profileName),
                });
            }

            if (File.Exists(zipPath))
            {
                output.Add(new StagingItem
                {
                    kind = KindZip,
                    originalPath = zipPath,
                    stagingPath = Path.Combine(
                        StagingRoot, subdirPrefix, profileName + ".zip"),
                });
            }
        }

        // ── Move / Restore ──

        private static void MoveToStaging(StagingItem item)
        {
            string original = item.originalPath;
            string staging = item.stagingPath;
            string originalMeta = original + ".meta";
            string stagingMeta = staging + ".meta";

            string parent = Path.GetDirectoryName(staging);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            if (item.kind == KindFolder)
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, true);
                Directory.Move(original, staging);
            }
            else
            {
                if (File.Exists(staging))
                    File.Delete(staging);
                File.Move(original, staging);
            }

            // Move .meta alongside so AssetDatabase keeps the GUID stable
            // when the item is moved back after the build.
            if (File.Exists(originalMeta))
            {
                if (File.Exists(stagingMeta))
                    File.Delete(stagingMeta);
                File.Move(originalMeta, stagingMeta);
            }
        }

        private static void RestoreItem(StagingItem item)
        {
            string original = item.originalPath;
            string staging = item.stagingPath;
            string originalMeta = original + ".meta";
            string stagingMeta = staging + ".meta";

            string parent = Path.GetDirectoryName(original);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            if (item.kind == KindFolder)
            {
                if (Directory.Exists(staging))
                {
                    if (Directory.Exists(original))
                        Directory.Delete(original, true);
                    Directory.Move(staging, original);
                }
            }
            else
            {
                if (File.Exists(staging))
                {
                    if (File.Exists(original))
                        File.Delete(original);
                    File.Move(staging, original);
                }
            }

            if (File.Exists(stagingMeta))
            {
                if (File.Exists(originalMeta))
                    File.Delete(originalMeta);
                File.Move(stagingMeta, originalMeta);
            }
        }

        private static void DiscardItem(StagingItem item)
        {
            string staging = item.stagingPath;
            string stagingMeta = staging + ".meta";

            if (Directory.Exists(staging))
                Directory.Delete(staging, true);
            else if (File.Exists(staging))
                File.Delete(staging);

            if (File.Exists(stagingMeta))
                File.Delete(stagingMeta);
        }

        // Postprocess after a successful build:
        // - Folders go back so the user keeps every profile in Editor.
        // - Zips are discarded — LocalZipBuildHelper.Postprocess has
        //   already restored the original folder from its temp-cache
        //   backup, so re-introducing the transient zip would just
        //   leave a junk archive next to the restored folder.
        private static void CleanupAfterBuild()
        {
            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
                return;

            StagingManifest manifest = LoadManifest(manifestPath);
            if (manifest?.items != null)
            {
                foreach (StagingItem item in manifest.items)
                {
                    if (item.kind == KindFolder)
                        RestoreItem(item);
                    else
                        DiscardItem(item);
                }
            }

            File.Delete(manifestPath);
            DeleteIfEmpty(StagingRoot);

            AssetDatabase.Refresh();
        }

        // Editor reload after a crash: LocalZipBuildHelper's static
        // tracking is gone, so we cannot tell what would have been
        // restored from temp cache. Move every staged item back; a
        // junk zip alongside a restored folder is harmless and trivial
        // to delete by hand.
        private static void RestoreAll()
        {
            string manifestPath = GetManifestPath();
            if (!File.Exists(manifestPath))
                return;

            StagingManifest manifest = LoadManifest(manifestPath);
            if (manifest?.items != null)
            {
                foreach (StagingItem item in manifest.items)
                    RestoreItem(item);
            }

            File.Delete(manifestPath);
            DeleteIfEmpty(StagingRoot);

            AssetDatabase.Refresh();
        }

        // ── Manifest IO ──

        private static string GetManifestPath()
        {
            return Path.Combine(StagingRoot, ManifestFileName);
        }

        private static void SaveManifest(List<StagingItem> items)
        {
            var manifest = new StagingManifest { items = items };
            string json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(GetManifestPath(), json);
        }

        private static StagingManifest LoadManifest(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<StagingManifest>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[SherpaOnnx] Failed to read staging manifest at " +
                    $"{path}: {ex.Message}");
                return null;
            }
        }

        private static void DeleteIfEmpty(string dir)
        {
            if (!Directory.Exists(dir))
                return;
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // Best-effort — leave the folder; next build overwrites it.
            }
        }

        // ── Manifest types ──

        [Serializable]
        private sealed class StagingManifest
        {
            public List<StagingItem> items = new();
        }

        [Serializable]
        private sealed class StagingItem
        {
            public string kind;
            public string originalPath;
            public string stagingPath;
        }
    }
}
