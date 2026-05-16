using System.Runtime.InteropServices;
using System.Text;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Sanitizes file paths before passing them to native sherpa-onnx via P/Invoke.
    ///
    /// The sherpa-onnx C# bindings use [MarshalAs(UnmanagedType.LPStr)] for all
    /// path strings, which converts to the system ANSI code page on Windows.
    /// Characters outside the current code page are silently corrupted.
    ///
    /// On macOS/Linux, Mono uses UTF-8 for LPStr — no issue.
    /// On Android, persistentDataPath is always ASCII — no issue.
    /// On Windows, this class attempts to convert non-ASCII paths to 8.3 short
    /// paths (always ASCII) via GetShortPathNameW.
    /// </summary>
    public static class NativePathSanitizer
    {
        /// <summary>
        /// Prepares a path for safe use with native P/Invoke [MarshalAs(LPStr)].
        /// On Windows: converts to 8.3 short path if non-ASCII characters found.
        /// On other platforms: returns the path unchanged (UTF-8 is safe).
        /// </summary>
        public static string Sanitize(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (!IsWindows())
                return path;

            if (!HasNonAsciiCharacters(path))
                return path;

            return TryGetShortPath(path);
        }

        /// <summary>
        /// Returns true if the path contains any non-ASCII character (code > 127).
        /// Spaces are NOT considered problematic — LPStr handles them correctly.
        /// </summary>
        public static bool HasNonAsciiCharacters(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] > 127)
                    return true;
            }

            return false;
        }

        // ── Private ──

        private static bool IsWindows()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return true;
#else
            return false;
#endif
        }

        private static string TryGetShortPath(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {
                int needed = GetShortPathNameW(path, null, 0);
                if (needed <= 0)
                {
                    LogNonAsciiWarning(path);
                    return path;
                }

                var sb = new StringBuilder(needed);
                int result = GetShortPathNameW(path, sb, needed);

                if (result <= 0 || result >= needed)
                {
                    LogNonAsciiWarning(path);
                    return path;
                }

                string shortPath = sb.ToString();

                if (HasNonAsciiCharacters(shortPath))
                {
                    LogNonAsciiWarning(path);
                    return path;
                }

                SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Path sanitized for native P/Invoke: '{shortPath}'");
                return shortPath;
            }
            catch (System.Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning($"[SherpaOnnx] GetShortPathName failed: {ex.Message}");
                LogNonAsciiWarning(path);
                return path;
            }
#else
            return path;
#endif
        }

        private static void LogNonAsciiWarning(string path)
        {
            SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] Model path contains non-ASCII characters which may not work on Windows. " + "Native sherpa-onnx uses ANSI string marshalling (LPStr). " + "Please move your Unity project to a path with only ASCII characters. " + $"Path: '{path}'");
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetShortPathNameW(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);
#endif
    }
}
