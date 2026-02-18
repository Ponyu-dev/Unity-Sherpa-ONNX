using System.Collections.Generic;
using System.IO;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.Common.Build
{
    /// <summary>
    /// Excludes disabled module resources from the build.
    /// Pre-build: moves resources to a temp backup.
    /// Post-build: restores everything back.
    /// Runs before <see cref="StreamingAssetsManifestBuilder"/> (50)
    /// so the manifest does not include excluded files.
    /// </summary>
    internal sealed class ModuleBuildFilter
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string SherpaOnnxDir =
            "Assets/StreamingAssets/SherpaOnnx";

        private const string BackupRoot = "ModuleBuildFilterBackup";

        private static readonly List<string> BackedUpPaths = new();

        public int callbackOrder => 40;

        // ── ASR resources ──

        private static readonly string[] AsrPaths =
        {
            SherpaOnnxDir + "/asr-settings.json",
            SherpaOnnxDir + "/online-asr-settings.json",
            SherpaOnnxDir + "/microphone-settings.json",
            SherpaOnnxDir + "/asr-models",
        };

        // ── TTS resources ──

        private static readonly string[] TtsPaths =
        {
            SherpaOnnxDir + "/tts-settings.json",
            SherpaOnnxDir + "/tts-models",
        };

        // ── VAD resources ──

        private static readonly string[] VadPaths =
        {
            SherpaOnnxDir + "/vad-settings.json",
            SherpaOnnxDir + "/vad-models",
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            BackedUpPaths.Clear();

            if (!AsrProjectSettings.instance.asrEnabled)
                BackupPaths(AsrPaths);

            if (!TtsProjectSettings.instance.ttsEnabled)
                BackupPaths(TtsPaths);

            if (!VadProjectSettings.instance.vadEnabled)
                BackupPaths(VadPaths);

            if (BackedUpPaths.Count > 0)
            {
                Debug.Log(
                    "[SherpaOnnx] ModuleBuildFilter: excluded " +
                    BackedUpPaths.Count + " resource(s) from build.");
                AssetDatabase.Refresh();
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RestoreAll();

            if (BackedUpPaths.Count > 0)
            {
                Debug.Log(
                    "[SherpaOnnx] ModuleBuildFilter: restored " +
                    BackedUpPaths.Count + " resource(s) after build.");
                AssetDatabase.Refresh();
            }

            BackedUpPaths.Clear();
        }

        private static void BackupPaths(string[] paths)
        {
            foreach (string path in paths)
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                    continue;

                string backupPath = GetBackupPath(path);
                string backupDir = Path.GetDirectoryName(backupPath);

                if (!string.IsNullOrEmpty(backupDir))
                    Directory.CreateDirectory(backupDir);

                if (Directory.Exists(path))
                    Directory.Move(path, backupPath);
                else
                    File.Move(path, backupPath);

                DeleteMeta(path);

                BackedUpPaths.Add(path);
            }
        }

        private static void RestoreAll()
        {
            foreach (string path in BackedUpPaths)
            {
                string backupPath = GetBackupPath(path);

                if (!File.Exists(backupPath)
                    && !Directory.Exists(backupPath))
                    continue;

                string parentDir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parentDir))
                    Directory.CreateDirectory(parentDir);

                if (Directory.Exists(path))
                    Directory.Delete(path, true);

                if (File.Exists(path))
                    File.Delete(path);

                if (Directory.Exists(backupPath))
                    Directory.Move(backupPath, path);
                else
                    File.Move(backupPath, path);
            }
        }

        private static void DeleteMeta(string path)
        {
            string metaPath = path.TrimEnd('/', '\\') + ".meta";

            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }

        private static string GetBackupPath(string assetPath)
        {
            return Path.Combine(
                Application.temporaryCachePath,
                BackupRoot,
                assetPath);
        }
    }
}
