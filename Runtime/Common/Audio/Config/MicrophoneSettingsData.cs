using System;

namespace PonyuDev.SherpaOnnx.Common.Audio.Config
{
    /// <summary>
    /// Microphone capture settings loaded from
    /// <c>microphone-settings.json</c> in StreamingAssets.
    /// All times are in seconds.
    /// </summary>
    [Serializable]
    public sealed class MicrophoneSettingsData
    {
        /// <summary>Sample rate in Hz.</summary>
        public int sampleRate = 16000;

        /// <summary>Circular buffer length in seconds.</summary>
        public int clipLengthSec = 10;

        /// <summary>
        /// Max wait time for <c>Microphone.Start</c>
        /// to begin producing samples.
        /// </summary>
        public float micStartTimeoutSec = 2f;

        /// <summary>
        /// Amplitude below this value is treated as silence.
        /// Real speech is typically maxAbs &gt; 0.05.
        /// </summary>
        public float silenceThreshold = 0.05f;

        /// <summary>
        /// Consecutive silent frames before fallback triggers.
        /// At 30 fps: 90 frames ~ 3 seconds.
        /// </summary>
        public int silenceFrameLimit = 90;

        /// <summary>
        /// Number of diagnostic log frames at the start
        /// of each recording path.
        /// </summary>
        public int diagFrameCount = 5;
    }
}
