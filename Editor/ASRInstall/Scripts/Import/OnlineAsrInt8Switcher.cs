using System.Linq;
using System.Text.RegularExpressions;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Checks for int8 .onnx alternatives and switches online ASR
    /// profile fields between int8 and normal variants.
    /// </summary>
    internal static class OnlineAsrInt8Switcher
    {
        private static readonly Regex Int8Regex =
            new Regex(@"[._\-]int8", RegexOptions.IgnoreCase);

        internal static bool HasInt8Alternative(
            OnlineAsrProfile profile, string dir)
        {
            string[] allOnnx = ModelFileScanner.GetOnnxFileNames(dir);
            var int8Set = allOnnx.Where(IsInt8).Select(GetBaseName).ToArray();
            var normalSet = allOnnx.Where(IsNotInt8).Select(GetBaseName).ToArray();
            return int8Set.Any(normalSet.Contains);
        }

        internal static bool IsUsingInt8(OnlineAsrProfile profile)
        {
            switch (profile.modelType)
            {
                case OnlineAsrModelType.Transducer:
                    return IsInt8(profile.transducerEncoder)
                        || IsInt8(profile.transducerDecoder)
                        || IsInt8(profile.transducerJoiner);
                case OnlineAsrModelType.Paraformer:
                    return IsInt8(profile.paraformerEncoder)
                        || IsInt8(profile.paraformerDecoder);
                case OnlineAsrModelType.Zipformer2Ctc:
                    return IsInt8(profile.zipformer2CtcModel);
                case OnlineAsrModelType.NemoCtc:
                    return IsInt8(profile.nemoCtcModel);
                case OnlineAsrModelType.ToneCtc:
                    return IsInt8(profile.toneCtcModel);
                default: return false;
            }
        }

        internal static void SwitchToInt8(
            OnlineAsrProfile profile, string dir)
        {
            OnlineAsrProfileAutoFiller.Fill(profile, dir, true);
        }

        internal static void SwitchToNormal(
            OnlineAsrProfile profile, string dir)
        {
            OnlineAsrProfileAutoFiller.Fill(profile, dir, false);
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
