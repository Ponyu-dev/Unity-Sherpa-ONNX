using System;
using System.Collections.Generic;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common
{
    /// <summary>
    /// Static registry of active native engine instances.
    /// Allows <c>EngineReloadGuard</c> (Editor) to dispose all engines
    /// before an Assembly Reload, preventing native handle leaks.
    /// </summary>
    public static class EngineRegistry
    {
        private static readonly List<IDisposable> _engines = new();

        internal static void Register(IDisposable engine)
        {
            if (engine == null) return;
            _engines.Add(engine);
        }

        internal static void Unregister(IDisposable engine)
        {
            if (engine == null) return;
            _engines.Remove(engine);
        }

        public static void DisposeAll()
        {
            if (_engines.Count == 0) return;

            int count = _engines.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                try
                {
                    _engines[i]?.Dispose();
                }
                catch (Exception ex)
                {
                    SherpaOnnxLog.RuntimeWarning($"[SherpaOnnx] EngineRegistry: dispose failed: {ex.Message}");
                }
            }

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] EngineRegistry: disposed {count} engine(s) before assembly reload.");
            _engines.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            _engines.Clear();
        }
    }
}
