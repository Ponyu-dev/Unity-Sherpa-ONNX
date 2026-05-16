using PonyuDev.SherpaOnnx.Common;
using UnityEditor;

namespace PonyuDev.SherpaOnnx.Editor.Common
{
    /// <summary>
    /// Disposes all registered native engines before an Assembly Reload
    /// to prevent native handle leaks when scripts recompile during Play Mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class EngineReloadGuard
    {
        static EngineReloadGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            EngineRegistry.DisposeAll();
        }
    }
}
