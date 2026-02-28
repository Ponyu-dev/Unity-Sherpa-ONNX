using System.Collections.Generic;
using System.Globalization;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters
{
    /// <summary>
    /// Validates custom keywords text format.
    /// Each non-empty line: space-separated tokens + optional @TAG :boost #threshold.
    /// Returns a list of human-readable warnings (empty if valid).
    /// </summary>
    internal static class CustomKeywordsValidator
    {
        internal static List<string> Validate(string text)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return warnings;

            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                int lineNum = i + 1;
                ValidateLine(line, lineNum, warnings);
            }

            return warnings;
        }

        private static void ValidateLine(string line, int lineNum, List<string> warnings)
        {
            string[] parts = line.Split(' ');

            bool hasTokens = false;
            bool tagFound = false;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part))
                    continue;

                if (part.StartsWith("@"))
                {
                    if (tagFound)
                    {
                        warnings.Add($"Line {lineNum}: multiple @TAG entries found.");
                        return;
                    }

                    tagFound = true;

                    if (part.Length < 2)
                        warnings.Add($"Line {lineNum}: empty @TAG.");

                    if (part.Contains(" "))
                        warnings.Add($"Line {lineNum}: @TAG must not contain spaces.");
                }
                else if (part.StartsWith(":"))
                {
                    if (part.Length < 2)
                    {
                        warnings.Add($"Line {lineNum}: empty boost value after ':'.");
                        continue;
                    }

                    if (!float.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        warnings.Add($"Line {lineNum}: invalid boost value '{part}'.");
                }
                else if (part.StartsWith("#"))
                {
                    if (part.Length < 2)
                    {
                        warnings.Add($"Line {lineNum}: empty threshold value after '#'.");
                        continue;
                    }

                    if (!float.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        warnings.Add($"Line {lineNum}: invalid threshold value '{part}'.");
                }
                else
                {
                    hasTokens = true;
                }
            }

            if (!hasTokens)
                warnings.Add($"Line {lineNum}: no tokens found.");
        }
    }
}
