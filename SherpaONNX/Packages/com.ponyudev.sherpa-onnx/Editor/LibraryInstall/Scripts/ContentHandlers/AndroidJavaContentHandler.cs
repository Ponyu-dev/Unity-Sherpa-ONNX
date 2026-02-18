using System;
using System.IO;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers
{
    /// <summary>
    /// Copies Java source files from the package <c>Plugins~/Android/</c>
    /// into <c>Assets/Plugins/SherpaOnnx/Android/</c>.
    /// Unity compiles <c>.java</c> files in Assets automatically
    /// for Android builds.
    /// </summary>
    internal static class AndroidJavaContentHandler
    {
        private const string PackagePluginsAndroid =
            "Packages/com.ponyudev.sherpa-onnx/Plugins~/Android";

        internal static void CopyJavaSources()
        {
            string srcDir = Path.GetFullPath(PackagePluginsAndroid);

            if (!Directory.Exists(srcDir))
            {
                SherpaOnnxLog.EditorWarning(
                    "[SherpaOnnx] AndroidJavaContentHandler: " +
                    $"source directory not found: {srcDir}");
                return;
            }

            string destDir = Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                "Android");
            Directory.CreateDirectory(destDir);

            string[] javaFiles = Directory.GetFiles(
                srcDir, "*.java");

            if (javaFiles.Length == 0)
            {
                SherpaOnnxLog.EditorLog(
                    "[SherpaOnnx] AndroidJavaContentHandler: " +
                    "no .java files found in package Plugins~.");
                return;
            }

            foreach (string srcPath in javaFiles)
            {
                string fileName = Path.GetFileName(srcPath);
                string destPath = Path.Combine(destDir, fileName);

                File.Copy(srcPath, destPath, overwrite: true);

                SherpaOnnxLog.EditorLog(
                    $"[SherpaOnnx] Copied {fileName} " +
                    $"to {destDir}");
            }
        }
    }
}
