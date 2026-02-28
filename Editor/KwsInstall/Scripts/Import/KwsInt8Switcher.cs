using System.Linq;
using System.Text.RegularExpressions;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Kws.Data;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Import
{
    /// <summary>
    /// Checks for int8 .onnx alternatives and switches KWS
    /// profile fields between int8 and normal variants.
    /// </summary>
    internal static class KwsInt8Switcher
    {
        private static readonly Regex Int8Regex =
            new Regex(@"[._\-]int8", RegexOptions.IgnoreCase);

        internal static bool HasInt8Alternative(KwsProfile profile, string dir)
        {
            string[] allOnnx = ModelFileScanner.GetOnnxFileNames(dir);
            var int8Set = allOnnx.Where(IsInt8).Select(GetBaseName).ToArray();
            var normalSet = allOnnx.Where(IsNotInt8).Select(GetBaseName).ToArray();
            return int8Set.Any(normalSet.Contains);
        }

        internal static bool IsUsingInt8(KwsProfile profile)
        {
            return IsInt8(profile.encoder)
                || IsInt8(profile.decoder)
                || IsInt8(profile.joiner);
        }

        internal static void SwitchToInt8(KwsProfile profile, string dir)
        {
            KwsProfileAutoFiller.Fill(profile, dir, useInt8: true);
        }

        internal static void SwitchToNormal(KwsProfile profile, string dir)
        {
            KwsProfileAutoFiller.Fill(profile, dir, useInt8: false);
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
