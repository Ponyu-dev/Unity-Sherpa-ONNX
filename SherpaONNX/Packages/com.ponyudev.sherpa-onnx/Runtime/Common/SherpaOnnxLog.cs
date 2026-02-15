using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common
{
    /// <summary>
    /// Centralized logging for the SherpaOnnx package.
    /// All Debug.Log calls go through this class so they can be
    /// toggled via Project Settings → Sherpa-ONNX.
    /// </summary>
    public static class SherpaOnnxLog
    {
        /// <summary>Controls logging from Editor code.</summary>
        public static bool EditorEnabled { get; set; } = true;

        /// <summary>Controls logging from Runtime code.</summary>
        public static bool RuntimeEnabled { get; set; } = true;

        // ── Editor ──

        public static void EditorLog(string msg)
        {
            if (EditorEnabled) Debug.Log(msg);
        }

        public static void EditorWarning(string msg)
        {
            if (EditorEnabled) Debug.LogWarning(msg);
        }

        public static void EditorError(string msg)
        {
            if (EditorEnabled) Debug.LogError(msg);
        }

        // ── Runtime ──

        public static void RuntimeLog(string msg)
        {
            if (RuntimeEnabled) Debug.Log(msg);
        }

        public static void RuntimeWarning(string msg)
        {
            if (RuntimeEnabled) Debug.LogWarning(msg);
        }

        public static void RuntimeError(string msg)
        {
            if (RuntimeEnabled) Debug.LogError(msg);
        }
    }
}
