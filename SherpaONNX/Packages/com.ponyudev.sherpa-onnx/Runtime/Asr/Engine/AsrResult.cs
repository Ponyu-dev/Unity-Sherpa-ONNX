namespace PonyuDev.SherpaOnnx.Asr.Engine
{
    /// <summary>
    /// Result of offline speech recognition.
    /// Copies all data from the native OfflineRecognizerResult
    /// so the native stream can be disposed immediately.
    /// </summary>
    public sealed class AsrResult
    {
        /// <summary>Recognized text.</summary>
        public string Text { get; }

        /// <summary>Individual tokens (may be null if not available).</summary>
        public string[] Tokens { get; }

        /// <summary>Per-token timestamps in seconds (may be null).</summary>
        public float[] Timestamps { get; }

        /// <summary>Per-token durations in seconds (may be null).</summary>
        public float[] Durations { get; }

        /// <summary>True when Text is not null or empty.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Text);

        internal AsrResult(
            string text,
            string[] tokens,
            float[] timestamps,
            float[] durations)
        {
            Text = text;
            Tokens = tokens;
            Timestamps = timestamps;
            Durations = durations;
        }
    }
}
