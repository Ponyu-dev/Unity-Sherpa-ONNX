using System;
using System.IO;
using System.Linq;

namespace PonyuDev.SherpaOnnx.Editor.Common
{
    /// <summary>
    /// File-system scanning helpers for model directories.
    /// Finds .onnx models, tokens, lexicons, sub-dirs, etc.
    /// Shared by TTS and ASR import pipelines.
    /// </summary>
    internal static class ModelFileScanner
    {
        // ── ONNX model finders ──

        /// <summary>
        /// Finds the primary .onnx model file, skipping .int8.onnx variants.
        /// </summary>
        internal static string FindOnnxModel(string dir)
        {
            string[] allOnnx = GetOnnxFileNames(dir);
            string primary = allOnnx.FirstOrDefault(IsNotInt8Onnx);
            return primary ?? string.Empty;
        }

        /// <summary>
        /// Finds the primary .onnx model file, preferring .int8.onnx variants.
        /// Falls back to non-int8 if no int8 file found.
        /// </summary>
        internal static string FindOnnxModelInt8(string dir)
        {
            string[] allOnnx = GetOnnxFileNames(dir);
            if (allOnnx.Length == 0) return string.Empty;

            return allOnnx.FirstOrDefault(IsInt8Onnx)
                ?? allOnnx.FirstOrDefault(IsNotInt8Onnx)
                ?? string.Empty;
        }

        /// <summary>
        /// Finds .onnx containing keyword, preferring int8 variant when
        /// <paramref name="useInt8"/> is true.
        /// </summary>
        internal static string FindOnnxContaining(
            string dir, string keyword, bool useInt8)
        {
            string[] allOnnx = GetOnnxFileNames(dir);
            var matching = allOnnx.Where(
                f => ContainsIgnoreCase(f, keyword));

            if (useInt8)
            {
                string int8Match = matching.FirstOrDefault(IsInt8Onnx);
                if (int8Match != null) return int8Match;
            }

            string normalMatch = matching.FirstOrDefault(IsNotInt8Onnx);
            return normalMatch ?? matching.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Prefers non-int8 .onnx, falls back to .int8.onnx if no other found.
        /// </summary>
        internal static string FindOnnxModelWithInt8Fallback(string dir)
        {
            string[] allOnnx = GetOnnxFileNames(dir);
            if (allOnnx.Length == 0) return string.Empty;

            return allOnnx.FirstOrDefault(IsNotInt8Onnx) ?? allOnnx[0];
        }

        /// <summary>
        /// Finds .onnx file whose name does NOT contain the keyword
        /// (skips .int8.onnx).
        /// </summary>
        internal static string FindOnnxExcluding(string dir, string keyword)
        {
            string[] allOnnx = GetOnnxFileNames(dir);

            string match = allOnnx
                .Where(IsNotInt8Onnx)
                .FirstOrDefault(f => !ContainsIgnoreCase(f, keyword));

            return match ?? string.Empty;
        }

        /// <summary>
        /// Finds .onnx file whose name contains the keyword
        /// (skips .int8.onnx).
        /// </summary>
        internal static string FindOnnxContaining(string dir, string keyword)
        {
            string[] allOnnx = GetOnnxFileNames(dir);

            string match = allOnnx
                .Where(IsNotInt8Onnx)
                .FirstOrDefault(f => ContainsIgnoreCase(f, keyword));

            return match ?? string.Empty;
        }

        /// <summary>
        /// Finds encoder or decoder .onnx by keyword.
        /// When <paramref name="useInt8"/> is true, prefers int8 variant.
        /// </summary>
        internal static string FindEncoderOrDecoder(
            string dir, string keyword, bool useInt8)
        {
            string[] allOnnx = GetOnnxFileNames(dir);
            var matching = allOnnx.Where(f => ContainsIgnoreCase(f, keyword));

            if (useInt8)
            {
                string int8Match = matching.FirstOrDefault(IsInt8Onnx);
                if (int8Match != null) return int8Match;
            }

            string normalMatch = matching.FirstOrDefault(IsNotInt8Onnx);
            return normalMatch ?? matching.FirstOrDefault() ?? string.Empty;
        }

        // ── File / directory finders ──

        /// <summary>
        /// Returns file name if the file exists, otherwise empty string.
        /// </summary>
        internal static string FindFileIfExists(string dir, string fileName)
        {
            string path = Path.Combine(dir, fileName);
            return File.Exists(path) ? fileName : string.Empty;
        }

        /// <summary>
        /// Finds the first file matching a wildcard pattern and returns
        /// its file name (without path). Returns empty string if none found.
        /// Example: FindFileByPattern(dir, "*tokens*.txt") matches both
        /// "tokens.txt" and "tiny.en-tokens.txt".
        /// </summary>
        internal static string FindFileByPattern(string dir, string pattern)
        {
            string[] files = FindFiles(dir, pattern);
            if (files.Length == 0) return string.Empty;
            return Path.GetFileName(files[0]);
        }

        /// <summary>
        /// Returns subdirectory name if it exists, otherwise empty string.
        /// </summary>
        internal static string FindSubDir(string dir, string subDirName)
        {
            string path = Path.Combine(dir, subDirName);
            return Directory.Exists(path) ? subDirName : string.Empty;
        }

        /// <summary>
        /// Collects all lexicon*.txt files as comma-separated names.
        /// Falls back to {modelName}.json or {modelName}.onnx.json
        /// if no lexicon files found.
        /// </summary>
        internal static string FindAllLexicons(
            string dir, string modelFileName)
        {
            string joined = JoinFileNames(dir, "lexicon*.txt");
            if (!string.IsNullOrEmpty(joined)) return joined;

            if (string.IsNullOrEmpty(modelFileName)) return string.Empty;

            // Try {model}.onnx.json first, then {model}.json.
            string withExt = FindFileIfExists(
                dir, modelFileName + ".json");
            if (!string.IsNullOrEmpty(withExt)) return withExt;

            string baseName = Path.GetFileNameWithoutExtension(
                modelFileName);
            return FindFileIfExists(dir, baseName + ".json");
        }

        /// <summary>
        /// Finds files matching <paramref name="pattern"/> and joins
        /// their names with commas. Returns empty string if no matches.
        /// </summary>
        internal static string JoinFileNames(string dir, string pattern)
        {
            string[] files = FindFiles(dir, pattern);
            if (files.Length == 0) return string.Empty;

            return string.Join(",", files.Select(Path.GetFileName));
        }

        /// <summary>
        /// Returns all .onnx file names (without path) in the directory.
        /// </summary>
        internal static string[] GetOnnxFileNames(string dir)
        {
            return FindFiles(dir, "*.onnx")
                .Select(Path.GetFileName)
                .ToArray();
        }

        // ── Private helpers ──

        private static string[] FindFiles(string dir, string pattern)
        {
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir, pattern,
                SearchOption.TopDirectoryOnly);
        }

        private static bool IsNotInt8Onnx(string fileName)
        {
            return !IsInt8Onnx(fileName);
        }

        private static bool IsInt8Onnx(string fileName)
        {
            return ContainsIgnoreCase(fileName, "int8");
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
