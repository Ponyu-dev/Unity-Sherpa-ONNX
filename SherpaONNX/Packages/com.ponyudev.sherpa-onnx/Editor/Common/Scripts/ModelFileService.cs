using System;
using System.IO;
using System.IO.Compression;
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

        internal static void DeleteModelDirectory(string modelDir)
        {
            if (!Directory.Exists(modelDir)) return;

            Directory.Delete(modelDir, true);

            string metaPath = modelDir + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);

            AssetDatabase.Refresh();
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

            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                ZipFile.CreateFromDirectory(modelDir, zipPath, CompressionLevel.Optimal, false);
                Debug.Log($"[SherpaOnnx] Packed → {zipPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SherpaOnnx] Pack to zip failed: {ex}");
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

            Debug.Log($"[SherpaOnnx] Deleted zip: {zipPath}");
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