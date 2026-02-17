using System.Linq;
using System.Text.RegularExpressions;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Checks for int8 .onnx alternatives and switches offline ASR
    /// profile fields between int8 and normal variants.
    /// </summary>
    internal static class AsrInt8Switcher
    {
        private static readonly Regex Int8Regex =
            new Regex(@"[._\-]int8", RegexOptions.IgnoreCase);

        internal static bool HasInt8Alternative(
            AsrProfile profile, string dir)
        {
            string[] allOnnx = ModelFileScanner.GetOnnxFileNames(dir);
            var int8Set = allOnnx.Where(IsInt8).Select(GetBaseName).ToArray();
            var normalSet = allOnnx.Where(IsNotInt8).Select(GetBaseName).ToArray();
            return int8Set.Any(normalSet.Contains);
        }

        internal static bool IsUsingInt8(AsrProfile profile)
        {
            switch (profile.modelType)
            {
                case AsrModelType.Transducer:
                    return IsInt8(profile.transducerEncoder)
                        || IsInt8(profile.transducerDecoder)
                        || IsInt8(profile.transducerJoiner);
                case AsrModelType.Paraformer: return IsInt8(profile.paraformerModel);
                case AsrModelType.Whisper:
                    return IsInt8(profile.whisperEncoder) || IsInt8(profile.whisperDecoder);
                case AsrModelType.SenseVoice: return IsInt8(profile.senseVoiceModel);
                case AsrModelType.Moonshine:
                    return IsInt8(profile.moonshinePreprocessor) || IsInt8(profile.moonshineEncoder);
                case AsrModelType.NemoCtc: return IsInt8(profile.nemoCtcModel);
                case AsrModelType.ZipformerCtc: return IsInt8(profile.zipformerCtcModel);
                case AsrModelType.Tdnn: return IsInt8(profile.tdnnModel);
                case AsrModelType.FireRedAsr:
                    return IsInt8(profile.fireRedAsrEncoder) || IsInt8(profile.fireRedAsrDecoder);
                case AsrModelType.Dolphin: return IsInt8(profile.dolphinModel);
                case AsrModelType.Canary:
                    return IsInt8(profile.canaryEncoder) || IsInt8(profile.canaryDecoder);
                case AsrModelType.WenetCtc: return IsInt8(profile.wenetCtcModel);
                case AsrModelType.Omnilingual: return IsInt8(profile.omnilingualModel);
                case AsrModelType.MedAsr: return IsInt8(profile.medAsrModel);
                case AsrModelType.FunAsrNano:
                    return IsInt8(profile.funAsrNanoEncoderAdaptor) || IsInt8(profile.funAsrNanoLlm);
                default: return false;
            }
        }

        internal static void SwitchToInt8(AsrProfile profile, string dir)
        {
            AsrProfileAutoFiller.Fill(profile, dir, true);
        }

        internal static void SwitchToNormal(AsrProfile profile, string dir)
        {
            AsrProfileAutoFiller.Fill(profile, dir, false);
        }

        private static string GetBaseName(string fileName)
        {
            string withoutExt = fileName.EndsWith(".onnx")
                ? fileName.Substring(0, fileName.Length - 5)
                : fileName;
            return Int8Regex.Replace(withoutExt, "").ToLowerInvariant();
        }

        private static bool IsInt8(string f)
        {
            return !string.IsNullOrEmpty(f)
                && f.IndexOf("int8",
                    System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsNotInt8(string f)
        {
            return !IsInt8(f);
        }
    }
}
