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
        /// Algorithm applied when the device's native rate differs from
        /// <see cref="sampleRate"/>. Common on Android where hardware
        /// is locked at 44.1/48 kHz regardless of the requested rate.
        /// </summary>
        public ResamplingMode resamplingMode = ResamplingMode.Linear;
    }
}
