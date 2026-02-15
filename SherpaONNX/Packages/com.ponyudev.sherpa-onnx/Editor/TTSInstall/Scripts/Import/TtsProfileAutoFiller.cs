using System.IO;
using System.Linq;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Scans a model directory and fills <see cref="TtsProfile"/> path fields.
    /// Stores only file names / folder names — full paths are assembled at runtime
    /// from <see cref="TtsModelPaths.TtsModelsRelative"/> + profile name + entry name.
    /// </summary>
    internal static class TtsProfileAutoFiller
    {
        internal static void Fill(TtsProfile profile, string modelDir)
        {
            FillCommonFields(profile, modelDir);
            FillByModelType(profile, modelDir);
        }

        // ── Common fields (shared across model types) ──

        private static void FillCommonFields(TtsProfile profile, string dir)
        {
            FillRuleFsts(profile, dir);
            FillRuleFars(profile, dir);
        }

        // ── Per-model-type fields ──

        private static void FillByModelType(TtsProfile profile, string dir)
        {
            switch (profile.modelType)
            {
                case TtsModelType.Vits:
                    FillVits(profile, dir);
                    break;
                case TtsModelType.Matcha:
                    FillMatcha(profile, dir);
                    break;
                case TtsModelType.Kokoro:
                    FillKokoro(profile, dir);
                    break;
                case TtsModelType.Kitten:
                    FillKitten(profile, dir);
                    break;
            }
        }

        private static void FillVits(TtsProfile profile, string dir)
        {
            profile.vitsModel = FindOnnxModel(dir);
            profile.vitsTokens = FindFileIfExists(dir, "tokens.txt");
            profile.vitsLexicon = FindLexicon(dir, profile.vitsModel);
            profile.vitsDataDir = FindSubDir(dir, "espeak-ng-data");
            profile.vitsDictDir = FindSubDir(dir, "dict");
        }

        private static void FillMatcha(TtsProfile profile, string dir)
        {
            profile.matchaAcousticModel = FindOnnxModel(dir);
            profile.matchaTokens = FindFileIfExists(dir, "tokens.txt");
            profile.matchaDataDir = FindSubDir(dir, "espeak-ng-data");
            profile.matchaDictDir = FindSubDir(dir, "dict");
            profile.matchaNoiseScale = 0.667f;
            profile.matchaLengthScale = 1.0f;
        }

        private static void FillKitten(TtsProfile profile, string dir)
        {
            profile.kittenModel = FindOnnxModel(dir);
            profile.kittenVoices = FindFileIfExists(dir, "voices.bin");
            profile.kittenTokens = FindFileIfExists(dir, "tokens.txt");
            profile.kittenDataDir = FindSubDir(dir, "espeak-ng-data");
            profile.kittenLengthScale = 1.0f;
        }

        private static void FillKokoro(TtsProfile profile, string dir)
        {
            profile.kokoroModel = FindOnnxModelWithInt8Fallback(dir);
            profile.kokoroVoices = FindFileIfExists(dir, "voices.bin");
            profile.kokoroTokens = FindFileIfExists(dir, "tokens.txt");
            profile.kokoroDataDir = FindSubDir(dir, "espeak-ng-data");
            profile.kokoroDictDir = FindSubDir(dir, "dict");
            profile.kokoroLexicon = JoinFileNames(dir, "lexicon-*.txt");
            profile.kokoroLengthScale = 1.0f;
        }

        // ── Rule files ──

        private static void FillRuleFsts(TtsProfile profile, string dir)
        {
            string joined = JoinFileNames(dir, "*.fst");
            if (!string.IsNullOrEmpty(joined))
                profile.ruleFsts = joined;
        }

        private static void FillRuleFars(TtsProfile profile, string dir)
        {
            string joined = JoinFileNames(dir, "*.far");
            if (!string.IsNullOrEmpty(joined))
                profile.ruleFars = joined;
        }

        // ── Shared helpers (reusable by future model fillers) ──

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
        /// Prefers non-int8 .onnx, falls back to .int8.onnx if no other found.
        /// </summary>
        internal static string FindOnnxModelWithInt8Fallback(string dir)
        {
            string[] allOnnx = GetOnnxFileNames(dir);
            if (allOnnx.Length == 0) return string.Empty;

            return allOnnx.FirstOrDefault(IsNotInt8Onnx) ?? allOnnx[0];
        }

        /// <summary>
        /// Finds .onnx file whose name does NOT contain the keyword (skips .int8.onnx).
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
        /// Finds .onnx file whose name contains the keyword (skips .int8.onnx).
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
        /// Returns file name if the file exists, otherwise empty string.
        /// </summary>
        internal static string FindFileIfExists(string dir, string fileName)
        {
            string path = Path.Combine(dir, fileName);
            return File.Exists(path) ? fileName : string.Empty;
        }

        /// <summary>
        /// Finds lexicon: first tries lexicon.txt, then {modelName}.onnx.json fallback.
        /// </summary>
        internal static string FindLexicon(string dir, string modelFileName)
        {
            string lexiconTxt = FindFileIfExists(dir, "lexicon.txt");
            if (!string.IsNullOrEmpty(lexiconTxt)) return lexiconTxt;

            if (string.IsNullOrEmpty(modelFileName)) return string.Empty;

            string jsonName = modelFileName + ".json";
            return FindFileIfExists(dir, jsonName);
        }

        /// <summary>
        /// Finds files matching <paramref name="pattern"/> and joins their names with commas.
        /// Returns empty string if no matches found.
        /// </summary>
        internal static string JoinFileNames(string dir, string pattern)
        {
            string[] files = FindFiles(dir, pattern);
            if (files.Length == 0) return string.Empty;

            return string.Join(",", files.Select(Path.GetFileName));
        }

        /// <summary>
        /// Returns subdirectory name if it exists, otherwise empty string.
        /// </summary>
        internal static string FindSubDir(string dir, string subDirName)
        {
            string path = Path.Combine(dir, subDirName);
            return Directory.Exists(path) ? subDirName : string.Empty;
        }

        private static string[] FindFiles(string dir, string pattern)
        {
            if (!Directory.Exists(dir)) return System.Array.Empty<string>();
            return Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
        }

        private static string[] GetOnnxFileNames(string dir)
        {
            return FindFiles(dir, "*.onnx")
                .Select(Path.GetFileName)
                .ToArray();
        }

        private static bool IsNotInt8Onnx(string fileName)
        {
            return !fileName.EndsWith(".int8.onnx");
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value,
                System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
