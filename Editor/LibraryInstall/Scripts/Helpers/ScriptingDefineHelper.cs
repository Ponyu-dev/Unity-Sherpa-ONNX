using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Adds or removes the SHERPA_ONNX scripting define symbol
    /// across all build target groups.
    /// Runs automatically on domain reload to restore the define
    /// if it was removed externally.
    /// </summary>
    [InitializeOnLoad]
    internal static class ScriptingDefineHelper
    {
        private const string Define = "SHERPA_ONNX";

        static ScriptingDefineHelper()
        {
            SyncDefineWithInstallState();
        }

        /// <summary>
        /// Syncs the define with the standard managed DLL presence.
        /// The define must match whether <c>SherpaOnnx</c> types are
        /// available in the Editor — only the standard managed DLL
        /// provides them (the iOS DLL cannot load in Editor).
        /// Called on every domain reload (compilation, project open).
        /// </summary>
        internal static void SyncDefineWithInstallState()
        {
            if (LibraryInstallStatus.IsManagedDllPresent())
                EnsureDefine();
            else
                RemoveDefine();
        }

        internal static void EnsureDefine()
        {
            foreach (var target in GetActiveTargets())
                EnsureDefineForTarget(target);
        }

        internal static void RemoveDefine()
        {
            foreach (var target in GetActiveTargets())
                RemoveDefineForTarget(target);
        }

        internal static void EnsureDefineForTarget(
            NamedBuildTarget target)
        {
            List<string> defines = GetDefines(target);
            if (defines.Contains(Define))
                return;

            defines.Add(Define);
            SetDefines(target, defines);
        }

        internal static void RemoveDefineForTarget(
            NamedBuildTarget target)
        {
            List<string> defines = GetDefines(target);
            if (!defines.Remove(Define))
                return;

            SetDefines(target, defines);
        }

        private static List<string> GetDefines(NamedBuildTarget target)
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] symbols);
            return symbols.ToList();
        }

        private static void SetDefines(NamedBuildTarget target, List<string> defines)
        {
            PlayerSettings.SetScriptingDefineSymbols(target, defines.ToArray());
        }

        private static IEnumerable<NamedBuildTarget> GetActiveTargets()
        {
            yield return NamedBuildTarget.Standalone;
            yield return NamedBuildTarget.Android;
            yield return NamedBuildTarget.iOS;
            yield return NamedBuildTarget.Server;
        }
    }
}
