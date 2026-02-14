using System.IO;

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
