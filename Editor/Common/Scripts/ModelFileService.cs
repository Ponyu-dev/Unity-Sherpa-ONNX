using System;
using System.IO;
using System.IO.Compression;
using PonyuDev.SherpaOnnx.Common;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace PonyuDev.SherpaOnnx.Editor.Common
{
    /// <summary>
    /// Shared file-system operations for model directories.
    /// Used by TTS (and later STT) presenters to keep I/O out of UI code.
    /// </summary>
    internal static class ModelFileService
    {
        internal static bool ModelDirExists(string modelDir)
        {
            return Directory.Exists(modelDir);
        }

        internal static bool IsProfileMissing(string profileName, Func<string, string> getModelDir)
        {
            if (string.IsNullOrEmpty(profileName)) return false;
            return !ModelDirExists(getModelDir(profileName));
        }

        internal static void DeleteModelDirectory(string modelDir)
        {
            if (!Directory.Exists(modelDir)) return;

            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Deleting model directory: {modelDir}");

            Directory.Delete(modelDir, true);

            string metaPath = modelDir + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);

            AssetDatabase.Refresh();
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Model directory deleted: {modelDir}");
        }

        internal static void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            if (!File.Exists(filePath)) return;

            File.Delete(filePath);
        }

        internal static bool ZipExists(string modelDir)
        {
            return File.Exists(modelDir + ".zip");
        }

        internal static void PackToZip(string modelDir)
        {
            string zipPath = modelDir + ".zip";
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Packing to zip: {modelDir}");

            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(modelDir, zipPath, CompressionLevel.Optimal, false);
                SherpaOnnxLog.EditorLog($"[SherpaOnnx] Packed → {zipPath}");
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.EditorError($"[SherpaOnnx] Pack to zip failed: {ex}");
            }
        }

        internal static void DeleteZip(string modelDir)
        {
            string zipPath = modelDir + ".zip";

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            string metaPath = zipPath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);

            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Deleted zip: {zipPath}");
        }

        // ── Editor ──

        internal static void PingFirstAsset(string modelDir)
        {
            if (!Directory.Exists(modelDir))
                return;

            string[] files = Directory.GetFiles(modelDir);
            if (files.Length == 0)
                return;

            string assetPath = files[0].Replace('\\', '/');
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return;

            EditorGUIUtility.PingObject(asset);
        }
    }
}