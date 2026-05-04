using System;
using System.IO;

namespace PonyuDev.SherpaOnnx.Common.Validation
{
    /// <summary>
    /// Pre-validates model files before passing them to native
    /// sherpa-onnx constructors. Detects INT8 quantized models and
    /// logs a warning — INT8 ops can crash on devices that do not
    /// support the corresponding ONNX operators, so the user should
    /// know which engine is loading an INT8 build.
    /// </summary>
    public static class ModelFileValidator
    {
        /// <summary>
        /// Scans <paramref name="modelDir"/> for ONNX files containing
        /// "int8" in the filename. If any are found, logs one warning.
        /// Never blocks loading — replacing an INT8 build with a FP32
        /// one is the user's call.
        /// </summary>
        /// <param name="modelDir">Model directory path.</param>
        /// <param name="engineName">
        /// Engine label for the log message (e.g. "ASR", "TTS").
        /// </param>
        public static void LogIfInt8(string modelDir, string engineName)
        {
            if (string.IsNullOrEmpty(modelDir) || !Directory.Exists(modelDir))
                return;

            try
            {
                string[] onnxFiles = Directory.GetFiles(
                    modelDir, "*.onnx", SearchOption.TopDirectoryOnly);

                foreach (string filePath in onnxFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    if (!ContainsInt8Marker(fileName))
                        continue;

                    SherpaOnnxLog.RuntimeWarning(
                        $"[SherpaOnnx] {engineName}: INT8 quantized model detected: " +
                        $"'{fileName}'. INT8 ops may crash on devices that do not " +
                        "support them — replace with a FP32 build if you see crashes.");
                    return;
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] {engineName}: failed to scan " +
                    $"model directory for INT8 files: {ex.Message}");
            }
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
