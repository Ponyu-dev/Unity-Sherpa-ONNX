using System;
using System.IO;

namespace PonyuDev.SherpaOnnx.Common.Validation
{
    /// <summary>
    /// Pre-validates model files before passing them to native
    /// sherpa-onnx constructors. Detects INT8 quantized models
    /// and blocks loading — INT8 models crash on devices
    /// that do not support INT8 ONNX operators.
    /// </summary>
    public static class ModelFileValidator
    {
        /// <summary>
        /// Scans model directory for ONNX files containing "int8"
        /// in the filename. Blocks loading unless explicitly allowed.
        /// Call this BEFORE any native constructor to prevent
        /// a segfault that cannot be caught by try/catch.
        /// </summary>
        /// <param name="modelDir">Model directory path.</param>
        /// <param name="engineName">
        /// Engine label for log messages (e.g. "ASR", "TTS").
        /// </param>
        /// <param name="allowInt8">
        /// When true, logs a warning but allows loading.
        /// When false (default), logs an error and blocks.
        /// </param>
        /// <returns>
        /// True if loading should be blocked (INT8 detected,
        /// not allowed). False otherwise.
        /// </returns>
        public static bool BlockIfInt8Model(
            string modelDir, string engineName,
            bool allowInt8 = false)
        {
            if (string.IsNullOrEmpty(modelDir)
                || !Directory.Exists(modelDir))
            {
                return false;
            }

            try
            {
                string[] onnxFiles = Directory.GetFiles(
                    modelDir, "*.onnx",
                    SearchOption.TopDirectoryOnly);

                foreach (string filePath in onnxFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (!ContainsInt8Marker(fileName))
                        continue;

                    if (allowInt8)
                    {
                        SherpaOnnxLog.RuntimeWarning(
                            $"[SherpaOnnx] {engineName}: INT8 " +
                            $"quantized model detected: " +
                            $"'{fileName}'. Loading is allowed " +
                            "via allowInt8 flag. If Unity crashes, " +
                            "switch to a FP32 model.");
                        return false;
                    }

                    SherpaOnnxLog.RuntimeError(
                        $"[SherpaOnnx] {engineName}: INT8 " +
                        $"quantized model detected: " +
                        $"'{fileName}'. Loading blocked — " +
                        "INT8 models may crash on devices " +
                        "that do not support INT8 ONNX " +
                        "operators. Set allowInt8 = true " +
                        "in the profile to override.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] {engineName}: failed to scan " +
                    $"model directory for INT8 files: " +
                    $"{ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Checks whether the file name contains an INT8 marker.
        /// Matches: "-int8", "_int8", ".int8" (case-insensitive).
        /// </summary>
        internal static bool ContainsInt8Marker(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string lower = fileName.ToLowerInvariant();
            return lower.Contains("-int8")
                   || lower.Contains("_int8")
                   || lower.Contains(".int8");
        }
    }
}
