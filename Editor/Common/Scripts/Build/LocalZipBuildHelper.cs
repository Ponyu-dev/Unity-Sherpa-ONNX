using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using PonyuDev.SherpaOnnx.Common.Data;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace PonyuDev.SherpaOnnx.Editor.Common.Build
{
    /// <summary>
    /// Shared logic for LocalZip build processors.
    /// Zips model directories before build and restores them after.
    /// Manages zipped-profile state internally, keyed by backupRoot.
    /// Each model-type processor is a thin wrapper that calls
    /// <see cref="Preprocess"/> and <see cref="Postprocess"/>.
    /// </summary>
    internal static class LocalZipBuildHelper
    {
        /// <summary>
        /// Minimal view of a profile for build processing.
        /// Avoids coupling to concrete profile types.
        /// </summary>
        internal readonly struct ProfileEntry
        {
            internal readonly string ProfileName;
            internal readonly ModelSource Source;

            internal ProfileEntry(string profileName, ModelSource source)
            {
                ProfileName = profileName;
                Source = source;
            }
        }

        private static readonly Dictionary<string, List<string>> ZippedMap = new();

        /// <summary>
        /// Zips LocalZip model directories, backs up originals,
        /// and tracks zipped profile names for later restore.
        /// </summary>
        internal static void Preprocess(
            string backupRoot,
            IReadOnlyList<ProfileEntry> entries,
            Func<string, string> getModelDir)
        {
            var zipped = new List<string>();

            foreach (ProfileEntry entry in entries)
            {
                if (entry.Source != ModelSource.LocalZip)
                    continue;

                if (string.IsNullOrEmpty(entry.ProfileName))
                    continue;

                string modelDir = getModelDir(entry.ProfileName);
                if (!Directory.Exists(modelDir))
                {
                    Debug.LogWarning(
                        $"[SherpaOnnx] LocalZip profile '{entry.ProfileName}' " +
                        $"has no model directory at {modelDir}. Skipping.");
                    continue;
                }

                string zipPath = modelDir + ".zip";
                ZipDirectory(modelDir, zipPath);
                BackupDirectory(modelDir, entry.ProfileName, backupRoot);

                zipped.Add(entry.ProfileName);
            }

            ZippedMap[backupRoot] = zipped;

            if (zipped.Count > 0)
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// Restores backed-up directories, deletes zips,
        /// and clears tracked state for this backupRoot.
        /// </summary>
        internal static void Postprocess(
            string backupRoot,
            Func<string, string> getModelDir)
        {
            if (!ZippedMap.TryGetValue(backupRoot, out List<string> zipped))
                return;

            foreach (string profileName in zipped)
            {
                string modelDir = getModelDir(profileName);
                DeleteZipFromAssets(modelDir + ".zip");
                RestoreDirectory(modelDir, profileName, backupRoot);
            }

            ZippedMap.Remove(backupRoot);

            if (zipped.Count > 0)
                AssetDatabase.Refresh();
        }

        // ── File operations ──

        private static void ZipDirectory(string sourceDir, string zipPath)
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(
                sourceDir, zipPath, CompressionLevel.Optimal, false);
        }

        private static void BackupDirectory(
            string modelDir, string profileName, string backupRoot)
        {
            string backupDir = GetBackupPath(profileName, backupRoot);

            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);

            Directory.CreateDirectory(Path.GetDirectoryName(backupDir));
            Directory.Move(modelDir, backupDir);
        }

        private static void RestoreDirectory(
            string modelDir, string profileName, string backupRoot)
        {
            string backupDir = GetBackupPath(profileName, backupRoot);

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

        private static string GetBackupPath(
            string profileName, string backupRoot)
        {
            return Path.Combine(
                Application.temporaryCachePath, backupRoot, profileName);
        }
    }
}
