using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Splits text into sentences for queued / per-sentence TTS generation.
    /// Recognises Latin (<c>. ! ?</c>) and CJK (<c>。 ！ ？</c>) terminators.
    /// Trailing whitespace is trimmed; empty fragments are skipped.
    /// <para/>
    /// Designed for sherpa-onnx playback chunking, not linguistic perfection.
    /// Edge cases like &quot;Mr.&quot;, decimals (&quot;3.14&quot;) and ellipses
    /// will produce extra splits — that's acceptable here because each fragment
    /// is independently synthesizable.
    /// </summary>
    public static class SentenceSplitter
    {
        // Match a run of terminators followed by a whitespace OR end-of-string.
        // Latin: . ! ? — CJK: 。 ！ ？
        private static readonly Regex SplitRegex = new(
            @"(?<=[\.!\?。！？])\s+|(?<=[\.!\?。！？])$",
            RegexOptions.Compiled);

        /// <summary>
        /// Returns trimmed, non-empty sentences. If <paramref name="text"/>
        /// has no terminators, returns the whole text as one element.
        /// </summary>
        public static IEnumerable<string> Split(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            string[] parts = SplitRegex.Split(text);
            foreach (var part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                    yield return trimmed;
            }
        }
    }
}
