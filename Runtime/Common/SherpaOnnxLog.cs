using System;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common
{
    /// <summary>
    /// Log severity level.
    /// </summary>
    public enum LogLevel
    {
        Log,
        Warning,
        Error
    }

    /// <summary>
    /// Centralized logging for the SherpaOnnx package.
    /// All Debug.Log calls go through this class so they can be
    /// toggled via Project Settings → Sherpa-ONNX.
    ///
    /// Subscribe to <see cref="OnRuntimeLog"/> to forward messages
    /// to Firebase Crashlytics, analytics, or custom logging.
    /// </summary>
    public static class SherpaOnnxLog
    {
        /// <summary>Controls logging from Editor code.</summary>
        public static bool EditorEnabled { get; set; } = true;

        /// <summary>Controls logging from Runtime code.</summary>
        public static bool RuntimeEnabled { get; set; } = true;

        /// <summary>
        /// Fires on every runtime log call regardless of
        /// <see cref="RuntimeEnabled"/> state.
        /// Use this to forward messages to Firebase Crashlytics,
        /// analytics, or any external logging system.
        /// </summary>
        public static event Action<LogLevel, string> OnRuntimeLog;

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
            OnRuntimeLog?.Invoke(LogLevel.Log, msg);
            if (RuntimeEnabled) Debug.Log(msg);
        }

        public static void RuntimeWarning(string msg)
        {
            OnRuntimeLog?.Invoke(LogLevel.Warning, msg);
            if (RuntimeEnabled) Debug.LogWarning(msg);
        }

        public static void RuntimeError(string msg)
        {
            OnRuntimeLog?.Invoke(LogLevel.Error, msg);
            if (RuntimeEnabled) Debug.LogError(msg);
        }
    }
}
