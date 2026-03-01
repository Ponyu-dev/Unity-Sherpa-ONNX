using System.Collections.Generic;
using System.IO;

namespace PonyuDev.SherpaOnnx.Common.Validation
{
    /// <summary>
    /// Tokenization scheme used by a KWS model.
    /// Detected from the model's <c>tokens.txt</c> vocabulary.
    /// </summary>
    public enum KeywordTokenType
    {
        Unknown,
        /// <summary>BPE subwords with ▁ prefix (e.g. gigaspeech).</summary>
        Bpe,
        /// <summary>Pinyin with tone diacriticals (e.g. wenetspeech).</summary>
        Ppinyin,
        /// <summary>ARPAbet phonemes + pinyin (e.g. zh-en).</summary>
        PhonePpinyin
    }

    /// <summary>
    /// Pre-validates keyword tokens against a model's vocabulary
    /// (<c>tokens.txt</c>) before passing them to the native
    /// <c>KeywordSpotter</c> constructor. Invalid tokens cause a
    /// native SEGFAULT that cannot be caught by try/catch.
    /// </summary>
    public static class KeywordTokenValidator
    {
        // Known ARPAbet phonemes — a subset is enough for detection.
        private static readonly HashSet<string> ArpabetMarkers = new()
        {
            "AA0", "AA1", "AE0", "AE1", "AH0", "AH1",
            "AO0", "AO1", "AW0", "AW1", "AY0", "AY1"
        };
        /// <summary>
        /// Loads token strings from a <c>tokens.txt</c> file.
        /// Each line format: <c>TOKEN_STRING ID</c>.
        /// Returns a set of all valid token strings.
        /// </summary>
        public static HashSet<string> LoadVocabulary(string tokensFilePath)
        {
            var vocabulary = new HashSet<string>();

            if (string.IsNullOrEmpty(tokensFilePath) || !File.Exists(tokensFilePath))
                return vocabulary;

            string[] lines = File.ReadAllLines(tokensFilePath);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Format: "TOKEN_STRING ID" — split on last space.
                int lastSpace = trimmed.LastIndexOf(' ');
                if (lastSpace <= 0)
                    continue;

                string token = trimmed.Substring(0, lastSpace);
                vocabulary.Add(token);
            }

            return vocabulary;
        }

        /// <summary>
        /// Validates keyword text against a vocabulary set.
        /// Returns a list of warnings for tokens not found in the vocabulary.
        /// </summary>
        public static List<string> ValidateKeywords(string keywordsText, HashSet<string> vocabulary)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(keywordsText) || vocabulary.Count == 0)
                return warnings;

            string[] lines = keywordsText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var invalid = FindInvalidTokensInLine(line, vocabulary);
                foreach (string token in invalid)
                    warnings.Add($"Line {i + 1}: token '{token}' not found in model vocabulary.");
            }

            return warnings;
        }

        /// <summary>
        /// Validates a keywords file on disk against a vocabulary set.
        /// Returns a list of warnings for invalid tokens.
        /// </summary>
        public static List<string> ValidateKeywordsFile(string keywordsFilePath, HashSet<string> vocabulary)
        {
            if (string.IsNullOrEmpty(keywordsFilePath) || !File.Exists(keywordsFilePath))
                return new List<string>();

            string text = File.ReadAllText(keywordsFilePath);
            return ValidateKeywords(text, vocabulary);
        }

        /// <summary>
        /// Pre-validates keyword tokens before the native constructor.
        /// Call this BEFORE <c>new KeywordSpotter(config)</c> to
        /// prevent a SEGFAULT when tokens are not in the vocabulary.
        /// </summary>
        /// <returns>
        /// True if loading should be blocked (invalid tokens found).
        /// False otherwise.
        /// </returns>
        public static bool BlockIfInvalidTokens(
            string tokensFilePath, string keywordsFilePath,
            string customKeywords, string engineName)
        {
            if (string.IsNullOrEmpty(tokensFilePath) || !File.Exists(tokensFilePath))
                return false;

            var vocabulary = LoadVocabulary(tokensFilePath);
            if (vocabulary.Count == 0)
                return false;

            bool hasInvalid = false;

            // Validate keywords file.
            if (!string.IsNullOrEmpty(keywordsFilePath) && File.Exists(keywordsFilePath))
            {
                var fileWarnings = ValidateKeywordsFile(keywordsFilePath, vocabulary);
                foreach (string w in fileWarnings)
                    SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {engineName} keywords file: {w}");

                if (fileWarnings.Count > 0)
                    hasInvalid = true;
            }

            // Validate custom keywords buffer.
            if (!string.IsNullOrEmpty(customKeywords))
            {
                var bufWarnings = ValidateKeywords(customKeywords, vocabulary);
                foreach (string w in bufWarnings)
                    SherpaOnnxLog.RuntimeError($"[SherpaOnnx] {engineName} custom keywords: {w}");

                if (bufWarnings.Count > 0)
                    hasInvalid = true;
            }

            if (hasInvalid)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] {engineName}: keywords contain tokens " +
                    "not in the model vocabulary. Loading blocked to " +
                    "prevent native crash. Ensure keyword tokens match " +
                    "tokens.txt — use sherpa-onnx text2token to " +
                    "generate correct tokens for your model.");
            }

            return hasInvalid;
        }

        /// <summary>
        /// Detects the tokenization scheme from the vocabulary.
        /// Uses heuristics: BPE tokens have <c>▁</c> prefix,
        /// ARPAbet has uppercase phonemes like <c>AA0</c>,
        /// ppinyin has tone diacriticals like <c>ǐ</c>.
        /// </summary>
        public static KeywordTokenType DetectTokenType(HashSet<string> vocabulary)
        {
            if (vocabulary == null || vocabulary.Count == 0)
                return KeywordTokenType.Unknown;

            bool hasBpe = false;
            bool hasArpabet = false;
            bool hasPinyin = false;

            foreach (string token in vocabulary)
            {
                if (string.IsNullOrEmpty(token))
                    continue;

                if (token[0] == '\u2581') // ▁
                    hasBpe = true;

                if (ArpabetMarkers.Contains(token))
                    hasArpabet = true;

                if (HasToneDiacritical(token))
                    hasPinyin = true;
            }

            if (hasBpe)
                return KeywordTokenType.Bpe;
            if (hasArpabet && hasPinyin)
                return KeywordTokenType.PhonePpinyin;
            if (hasPinyin)
                return KeywordTokenType.Ppinyin;

            return KeywordTokenType.Unknown;
        }

        // ── Private ──

        private static bool HasToneDiacritical(string token)
        {
            foreach (char c in token)
            {
                // Pinyin tone diacriticals: ā-ǖ range
                // Latin Extended-A (0x0100-0x017F) and Latin Extended-B (0x0180-0x024F)
                if (c >= '\u0100' && c <= '\u024F')
                    return true;
            }

            return false;
        }

        internal static List<string> FindInvalidTokensInLine(string line, HashSet<string> vocabulary)
        {
            var invalid = new List<string>();
            string[] parts = line.Split(' ');

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                // Skip metadata suffixes: @TAG, :boost, #threshold.
                if (part[0] == '@' || part[0] == ':' || part[0] == '#')
                    continue;

                if (!vocabulary.Contains(part))
                    invalid.Add(part);
            }

            return invalid;
        }
    }
}
