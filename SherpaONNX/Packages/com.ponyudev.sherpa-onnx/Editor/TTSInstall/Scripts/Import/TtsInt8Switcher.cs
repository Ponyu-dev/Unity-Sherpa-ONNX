using System.Linq;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Checks for int8 .onnx alternatives and switches profile fields to use them.
    /// Delegates to <see cref="TtsProfileAutoFiller"/> helpers for file discovery.
    /// </summary>
    internal static class TtsInt8Switcher
    {
        /// <summary>
        /// Returns true if the model directory contains int8 alternatives
        /// for the current model type's .onnx fields.
        /// Both int8 and non-int8 variants must exist.
        /// </summary>
        internal static bool HasInt8Alternative(TtsProfile profile, string dir)
        {
            string[] allOnnx = TtsProfileAutoFiller.GetOnnxFileNames(dir);
            bool hasInt8 = allOnnx.Any(IsInt8);
            bool hasNormal = allOnnx.Any(IsNotInt8);

            if (!hasInt8 || !hasNormal) return false;

            if (profile.modelType == TtsModelType.ZipVoice)
            {
                return allOnnx.Any(MatchesInt8Encoder)
                    && allOnnx.Any(MatchesInt8Decoder);
            }

            return true;
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
            }
        }

        /// <summary>
        /// Finds the int8 counterpart for a given .onnx file name.
        /// Simply picks the first .onnx file containing "int8" in its name.
        /// </summary>
        private static string FindInt8For(string[] allOnnx, string currentFile)
        {
            if (string.IsNullOrEmpty(currentFile)) return currentFile;
            return allOnnx.FirstOrDefault(IsInt8) ?? currentFile;
        }

        private static string FindNormalFor(string[] allOnnx, string currentFile)
        {
            if (string.IsNullOrEmpty(currentFile)) return currentFile;
            return allOnnx.FirstOrDefault(IsNotInt8) ?? currentFile;
        }

        private static bool IsInt8(string f)
        {
            return ContainsIgnoreCase(f, "int8");
        }

        private static bool IsNotInt8(string f)
        {
            return !IsInt8(f);
        }

        private static bool MatchesInt8Encoder(string f)
        {
            return IsInt8(f) && ContainsIgnoreCase(f, "encoder");
        }

        private static bool MatchesInt8Decoder(string f)
        {
            return IsInt8(f) && ContainsIgnoreCase(f, "decoder");
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value,
                System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
