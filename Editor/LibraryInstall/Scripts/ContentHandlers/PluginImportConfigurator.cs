using System.Collections.Generic;
using System.IO;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEditor;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers
{
    /// <summary>
    /// Applies PluginImporter settings to managed and native plugins
    /// after they are copied into Assets/Plugins/SherpaOnnx/.
    /// Must be called after AssetDatabase.Refresh().
    /// </summary>
    internal static class PluginImportConfigurator
    {
        private static readonly Dictionary<string, (BuildTarget Target, string Cpu)> s_nativeRidMap = new()
        {
            ["win-x64"] = (BuildTarget.StandaloneWindows64, "x86_64"),
            ["win-x86"] = (BuildTarget.StandaloneWindows, "x86"),
            ["win-arm64"] = (BuildTarget.StandaloneWindows64, "ARM64"),
            ["osx-x64"] = (BuildTarget.StandaloneOSX, "x86_64"),
            ["osx-arm64"] = (BuildTarget.StandaloneOSX, "ARM64"),
            ["linux-x64"] = (BuildTarget.StandaloneLinux64, "x86_64"),
            ["linux-arm64"] = (BuildTarget.StandaloneLinux64, "ARM64"),
            ["arm64-v8a"] = (BuildTarget.Android, "ARM64"),
            ["armeabi-v7a"] = (BuildTarget.Android, "ARMv7"),
            ["x86"] = (BuildTarget.Android, "x86"),
            ["x86_64"] = (BuildTarget.Android, "x86_64"),
            ["iOS"] = (BuildTarget.iOS, "AnyCPU"),
        };

        /// <summary>
        /// Single entry point. Decides managed vs native by LibraryArch.
        /// </summary>
        internal static void Configure(LibraryArch arch)
        {
            if (arch == LibraryPlatforms.ManagedLibrary)
            {
                string assetPath = Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    ConstantsInstallerPaths.ManagedDllFileName);
                ConfigureManagedDll(assetPath);
            }
            else if (InstallPipelineFactory.IsAndroid(arch))
            {
                string dirPath = Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    "Android",
                    arch.Name);
                ConfigureNativeDirectory(dirPath, arch.Name);
            }
            else if (InstallPipelineFactory.IsIOS(arch))
            {
                string dirPath = Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    "iOS");
                ConfigureIosXcframeworks(dirPath, arch.Name);
            }
            else
            {
                string dirPath = Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    arch.Name);
                ConfigureNativeDirectory(dirPath, arch.Name);
            }
        }

        private static void ConfigureManagedDll(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
                return;

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);

            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, true);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, true);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, true);

            // Exclude iOS
            importer.SetCompatibleWithPlatform(BuildTarget.iOS, false);

            importer.SaveAndReimport();
        }

        private static void ConfigureNativePlugin(string assetPath, string rid)
        {
            if (!s_nativeRidMap.TryGetValue(rid, out var mapping))
                return;

            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
                return;

            importer.SetCompatibleWithAnyPlatform(false);

            bool isMobile = mapping.Target == BuildTarget.Android
                            || mapping.Target == BuildTarget.iOS;

            // Mobile plugins are not compatible with Editor
            importer.SetCompatibleWithEditor(!isMobile);

            if (!isMobile)
            {
                importer.SetEditorData("OS", GetEditorOs(mapping.Target));
                importer.SetEditorData("CPU", mapping.Cpu);
            }

            importer.SetCompatibleWithPlatform(mapping.Target, true);
            importer.SetPlatformData(mapping.Target, "CPU", mapping.Cpu);

            importer.SaveAndReimport();
        }

        private static void ConfigureIosXcframeworks(string directoryAssetPath, string rid)
        {
            if (!Directory.Exists(directoryAssetPath))
                return;

            if (!s_nativeRidMap.TryGetValue(rid, out var mapping))
                return;

            // Configure xcframeworks
            string[] subDirs = Directory.GetDirectories(directoryAssetPath);
            foreach (string subDir in subDirs)
            {
                if (!subDir.EndsWith(".xcframework"))
                    continue;

                string assetPath = subDir.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer == null)
                    continue;

                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SetCompatibleWithPlatform(mapping.Target, true);
                importer.SetPlatformData(mapping.Target, "CPU", mapping.Cpu);

                importer.SaveAndReimport();
            }

            // Configure iOS managed DLL (sherpa-onnx.dll with __Internal binding)
            ConfigureIosManagedDll(directoryAssetPath);
        }

        private static void ConfigureIosManagedDll(string iosDirectoryPath)
        {
            string dllPath = Path.Combine(iosDirectoryPath,
                ConstantsInstallerPaths.ManagedDllFileName);
            string assetPath = dllPath.Replace('\\', '/');

            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
                return;

            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(false);
            importer.SetCompatibleWithPlatform(BuildTarget.iOS, true);
            importer.SetPlatformData(BuildTarget.iOS, "CPU", "AnyCPU");

            importer.SaveAndReimport();
        }

        private static void ConfigureNativeDirectory(string directoryAssetPath, string rid)
        {
            if (!Directory.Exists(directoryAssetPath))
                return;

            string[] files = Directory.GetFiles(directoryAssetPath);
            foreach (string file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;

                string assetPath = file.Replace('\\', '/');
                ConfigureNativePlugin(assetPath, rid);
            }
        }

        private static string GetEditorOs(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.StandaloneWindows => "Windows",
                BuildTarget.StandaloneWindows64 => "Windows",
                BuildTarget.StandaloneOSX => "OSX",
                BuildTarget.StandaloneLinux64 => "Linux",
                _ => "AnyOS"
            };
        }
    }
}
