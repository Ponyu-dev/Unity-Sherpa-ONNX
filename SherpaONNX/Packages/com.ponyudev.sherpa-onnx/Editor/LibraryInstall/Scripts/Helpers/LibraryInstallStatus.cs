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

        internal static string GetInstallDirectory(LibraryArch arch)
        {
            return Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx,
                arch.Name);
        }
    }
}
