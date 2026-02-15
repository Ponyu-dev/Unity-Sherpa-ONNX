using System.Linq;
using System.Text.RegularExpressions;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Checks for int8 .onnx alternatives and switches profile fields to use them.
    /// Delegates to <see cref="TtsProfileAutoFiller"/> helpers for file discovery.
    /// </summary>
    internal static class TtsInt8Switcher
    {
        private static readonly Regex Int8Regex =
            new Regex(@"[._\-]int8", RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns true when at least one .onnx file has both an int8
        /// and a normal variant sharing the same base name.
        /// E.g. decoder.onnx + decoder.int8.onnx → true,
        /// but decoder.int8.onnx + encoder.onnx → false.
        /// </summary>
        internal static bool HasInt8Alternative(TtsProfile profile, string dir)
        {
            string[] allOnnx = TtsProfileAutoFiller.GetOnnxFileNames(dir);

            var int8Set = allOnnx.Where(IsInt8).Select(GetBaseName).ToArray();
            var normalSet = allOnnx.Where(IsNotInt8).Select(GetBaseName).ToArray();

            return int8Set.Any(normalSet.Contains);
        }

        /// <summary>
        /// Returns true if the profile currently uses int8 model files.
        /// </summary>
        internal static bool IsUsingInt8(TtsProfile profile)
        {
            switch (profile.modelType)
            {
                case TtsModelType.Vits:    return IsInt8(profile.vitsModel);
                case TtsModelType.Matcha:  return IsInt8(profile.matchaAcousticModel);
                case TtsModelType.Kokoro:  return IsInt8(profile.kokoroModel);
                case TtsModelType.Kitten:  return IsInt8(profile.kittenModel);
                case TtsModelType.ZipVoice:
                    return IsInt8(profile.zipVoiceEncoder) || IsInt8(profile.zipVoiceDecoder);
                case TtsModelType.Pocket:
                    return IsInt8(profile.pocketLmFlow) || IsInt8(profile.pocketLmMain)
                        || IsInt8(profile.pocketEncoder) || IsInt8(profile.pocketDecoder);
                default: return false;
            }
        }

        /// <summary>
        /// Switches model .onnx fields to their int8 variants.
        /// </summary>
        internal static void SwitchToInt8(TtsProfile profile, string dir)
        {
            string[] allOnnx = TtsProfileAutoFiller.GetOnnxFileNames(dir);

            switch (profile.modelType)
            {
                case TtsModelType.Vits:
                    profile.vitsModel = FindInt8For(allOnnx, profile.vitsModel);
                    break;
                case TtsModelType.Matcha:
                    profile.matchaAcousticModel =
                        FindInt8For(allOnnx, profile.matchaAcousticModel);
                    break;
                case TtsModelType.Kokoro:
                    profile.kokoroModel = FindInt8For(allOnnx, profile.kokoroModel);
                    break;
                case TtsModelType.Kitten:
                    profile.kittenModel = FindInt8For(allOnnx, profile.kittenModel);
                    break;
                case TtsModelType.ZipVoice:
                    profile.zipVoiceEncoder =
                        TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "encoder", true);
                    profile.zipVoiceDecoder =
                        TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "decoder", true);
                    break;
                case TtsModelType.Pocket:
                    SwitchPocket(profile, dir, true);
                    break;
            }
        }

        /// <summary>
        /// Switches model .onnx fields back to non-int8 variants.
        /// </summary>
        internal static void SwitchToNormal(TtsProfile profile, string dir)
        {
            string[] allOnnx = TtsProfileAutoFiller.GetOnnxFileNames(dir);

            switch (profile.modelType)
            {
                case TtsModelType.Vits:
                    profile.vitsModel = FindNormalFor(allOnnx, profile.vitsModel);
                    break;
                case TtsModelType.Matcha:
                    profile.matchaAcousticModel =
                        FindNormalFor(allOnnx, profile.matchaAcousticModel);
                    break;
                case TtsModelType.Kokoro:
                    profile.kokoroModel = FindNormalFor(allOnnx, profile.kokoroModel);
                    break;
                case TtsModelType.Kitten:
                    profile.kittenModel = FindNormalFor(allOnnx, profile.kittenModel);
                    break;
                case TtsModelType.ZipVoice:
                    profile.zipVoiceEncoder =
                        TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "encoder", false);
                    profile.zipVoiceDecoder =
                        TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "decoder", false);
                    break;
                case TtsModelType.Pocket:
                    SwitchPocket(profile, dir, false);
                    break;
            }
        }

        /// <summary>
        /// Switches each Pocket .onnx field independently.
        /// Each field gets int8 only if an int8 variant exists for that keyword.
        /// </summary>
        private static void SwitchPocket(TtsProfile profile, string dir, bool useInt8)
        {
            profile.pocketLmFlow =
                TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "lm_flow", useInt8);
            profile.pocketLmMain =
                TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "lm_main", useInt8);
            profile.pocketEncoder =
                TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "encoder", useInt8);
            profile.pocketDecoder =
                TtsProfileAutoFiller.FindEncoderOrDecoder(dir, "decoder", useInt8);
        }

        /// <summary>
        /// Finds the int8 counterpart sharing the same base name.
        /// </summary>
        private static string FindInt8For(string[] allOnnx, string currentFile)
        {
            if (string.IsNullOrEmpty(currentFile)) return currentFile;

            string baseName = GetBaseName(currentFile);
            return allOnnx.FirstOrDefault(f => IsInt8(f) && GetBaseName(f) == baseName)
                ?? currentFile;
        }

        /// <summary>
        /// Finds the normal counterpart sharing the same base name.
        /// </summary>
        private static string FindNormalFor(string[] allOnnx, string currentFile)
        {
            if (string.IsNullOrEmpty(currentFile)) return currentFile;

            string baseName = GetBaseName(currentFile);
            return allOnnx.FirstOrDefault(f => IsNotInt8(f) && GetBaseName(f) == baseName)
                ?? currentFile;
        }

        /// <summary>
        /// Strips the int8 marker and .onnx extension to get a comparable base name.
        /// E.g. "decoder.int8.onnx" → "decoder", "decoder_int8.onnx" → "decoder",
        /// "decoder.onnx" → "decoder".
        /// </summary>
        private static string GetBaseName(string fileName)
        {
            string withoutExt = fileName.EndsWith(".onnx")
                ? fileName.Substring(0, fileName.Length - 5)
                : fileName;

            return Int8Regex.Replace(withoutExt, "").ToLowerInvariant();
        }

        private static bool IsInt8(string f)
        {
            return ContainsIgnoreCase(f, "int8");
        }

        private static bool IsNotInt8(string f)
        {
            return !IsInt8(f);
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value,
                System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
