using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Build
{
    /// <summary>
    /// Zips LocalZip model directories before build and restores them after.
    /// Pre-build: creates .zip in StreamingAssets, moves raw folder to temp backup.
    /// Post-build: deletes .zip, restores raw folder from backup.
    /// </summary>
    internal sealed class TtsLocalZipBuildProcessor
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string BackupRoot = "TtsBuildBackup";

        private static readonly List<string> ZippedProfiles = new();

        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            ZippedProfiles.Clear();

            TtsProjectSettings settings = TtsProjectSettings.instance;

            foreach (TtsProfile profile in settings.data.profiles)
            {
                if (profile.modelSource != TtsModelSource.LocalZip)
                    continue;

                if (string.IsNullOrEmpty(profile.profileName))
                    continue;

                string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
                if (!Directory.Exists(modelDir))
                {
                    Debug.LogWarning(
                        $"[SherpaOnnx] LocalZip profile '{profile.profileName}' " +
                        $"has no model directory at {modelDir}. Skipping.");
                    continue;
                }

                string zipPath = modelDir + ".zip";
                ZipDirectory(modelDir, zipPath);
                BackupDirectory(modelDir, profile.profileName);

                ZippedProfiles.Add(profile.profileName);
            }

            if (ZippedProfiles.Count > 0)
                AssetDatabase.Refresh();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            foreach (string profileName in ZippedProfiles)
            {
                string modelDir = TtsModelPaths.GetModelDir(profileName);
                DeleteZipFromAssets(modelDir + ".zip");
                RestoreDirectory(modelDir, profileName);
            }

            ZippedProfiles.Clear();

            AssetDatabase.Refresh();
        }

        private static void ZipDirectory(string sourceDir, string zipPath)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(
                sourceDir, zipPath, CompressionLevel.Optimal, false);
        }

        private static void BackupDirectory(string modelDir, string profileName)
        {
            string backupDir = GetBackupPath(profileName);

            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);

            Directory.CreateDirectory(Path.GetDirectoryName(backupDir));
            Directory.Move(modelDir, backupDir);
        }

        private static void RestoreDirectory(string modelDir, string profileName)
        {
            string backupDir = GetBackupPath(profileName);

            if (!Directory.Exists(backupDir))
                return;

            if (Directory.Exists(modelDir))
                Directory.Delete(modelDir, true);

            Directory.Move(backupDir, modelDir);
        }

        private static void DeleteZipFromAssets(string zipPath)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            string metaPath = zipPath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }

        private static string GetBackupPath(string profileName)
        {
            return Path.Combine(
                Application.temporaryCachePath, BackupRoot, profileName);
        }
    }
}
