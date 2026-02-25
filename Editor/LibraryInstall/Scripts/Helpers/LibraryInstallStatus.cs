using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Resolves install paths and checks installed state for a LibraryArch.
    /// </summary>
    internal static class LibraryInstallStatus
    {
        internal static bool IsInstalled(LibraryArch arch)
        {
            if (arch == LibraryPlatforms.ManagedLibrary)
                return IsManagedDllPresent();

            if (InstallPipelineFactory.IsIOS(arch))
                return IsIosInstalled(arch);

            string dir = GetInstallDirectory(arch);
            return Directory.Exists(dir)
                   && Directory.GetFiles(dir).Length > 0;
        }

        internal static bool IsManagedDllPresent()
        {
            return File.Exists(Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                ConstantsInstallerPaths.ManagedDllFileName));
        }

        internal static bool IsIosManagedDllPresent()
        {
            return File.Exists(Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                ConstantsInstallerPaths.IosManagedDllSubDir,
                ConstantsInstallerPaths.ManagedDllFileName));
        }

        /// <summary>
        /// Returns true if at least one managed DLL exists
        /// (standard or iOS). Note: NOT used for the SHERPA_ONNX
        /// define — the iOS DLL cannot provide types in Editor.
        /// Use <see cref="IsManagedDllPresent"/> for define logic.
        /// </summary>
        internal static bool IsAnyManagedDllPresent()
        {
            return IsAnyManagedDllPresent(ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, ConstantsInstallerPaths.ManagedDllFileName);
        }

        /// <summary>
        /// Testable overload that accepts explicit paths.
        /// </summary>
        internal static bool IsAnyManagedDllPresent(string baseDir, string fileName)
        {
            return File.Exists(Path.Combine(baseDir, fileName))
                   || File.Exists(Path.Combine(
                       baseDir,
                       ConstantsInstallerPaths.IosManagedDllSubDir,
                       fileName));
        }

        internal static bool HasAnyInstalled()
        {
            if (IsManagedDllPresent())
                return true;

            return LibraryPlatforms.Platforms
                .SelectMany(p => p.Arches)
                .Any(IsInstalled);
        }

        internal static bool HasAnyAndroidInstalled()
        {
            return LibraryPlatforms.Platforms
                .SelectMany(p => p.Arches)
                .Where(a => a.Platform == PlatformType.Android)
                .Any(IsInstalled);
        }

        internal static bool CanOperate(LibraryArch arch)
        {
            return !arch.IsManagedDllRoot || IsManagedDllPresent();
        }

        internal static string GetDeleteTargetPath(LibraryArch arch)
        {
            if (arch == LibraryPlatforms.ManagedLibrary)
            {
                return Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    ConstantsInstallerPaths.ManagedDllFileName);
            }

            return GetInstallDirectory(arch);
        }

        /// <summary>
        /// iOS installs share one folder. arm64 is installed when ios-arm64/
        /// subfolder exists inside any xcframework. Simulator checks for
        /// ios-arm64_x86_64-simulator/.
        /// </summary>
        private static bool IsIosInstalled(LibraryArch arch)
        {
            string iosDir = Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, "iOS");

            if (!Directory.Exists(iosDir))
                return false;

            string marker = arch.Name == "arm64"
                ? "ios-arm64"
                : "ios-arm64_x86_64-simulator";

            string[] dirs = Directory.GetDirectories(iosDir, marker, SearchOption.AllDirectories);
            return dirs.Length > 0;
        }

        /// <summary>
        /// Returns install directories of all non-iOS native libraries
        /// that are currently installed. Used for cascade deletion when
        /// the managed DLL is removed.
        /// </summary>
        internal static List<string> GetNonIosInstalledPaths()
        {
            var paths = new List<string>();

            foreach (var platform in LibraryPlatforms.Platforms)
            {
                foreach (var arch in platform.Arches)
                {
                    if (arch.Platform == PlatformType.iOS)
                        continue;

                    if (IsInstalled(arch))
                        paths.Add(GetInstallDirectory(arch));
                }
            }

            return paths;
        }

        internal static string GetInstallDirectory(LibraryArch arch)
        {
            if (InstallPipelineFactory.IsAndroid(arch))
            {
                return Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    "Android",
                    arch.Name);
            }

            if (InstallPipelineFactory.IsIOS(arch))
            {
                return Path.Combine(
                    ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                    "iOS");
            }

            return Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                arch.Name);
        }
    }
}
