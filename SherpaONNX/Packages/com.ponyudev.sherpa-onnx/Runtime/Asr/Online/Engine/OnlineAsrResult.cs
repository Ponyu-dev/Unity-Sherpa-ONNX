namespace PonyuDev.SherpaOnnx.Asr.Online.Engine
{
    /// <summary>
    /// Result of online (streaming) speech recognition.
    /// <see cref="IsFinal"/> is true when an endpoint was detected.
    /// </summary>
    public sealed class OnlineAsrResult
    {
        /// <summary>Recognized text so far.</summary>
        public string Text { get; }

        /// <summary>Individual tokens (characters or subwords).</summary>
        public string[] Tokens { get; }

        /// <summary>Timestamp for each token in seconds.</summary>
        public float[] Timestamps { get; }

        /// <summary>True when endpoint was detected (final result).</summary>
        public bool IsFinal { get; }

        /// <summary>True when Text is not null or empty.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Text);

        internal OnlineAsrResult(
            string text, string[] tokens,
            float[] timestamps, bool isFinal)
        {
            Text = text;
            Tokens = tokens;
            Timestamps = timestamps;
            IsFinal = isFinal;
        }
    }
}
