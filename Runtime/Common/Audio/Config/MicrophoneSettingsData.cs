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

        /// <summary>
        /// When <c>true</c>, <see cref="MicrophoneSource"/> auto-configures
        /// the platform audio session on start/stop:
        /// iOS — switches AVAudioSession between PlayAndRecord and Playback;
        /// Android — sets AudioManager MODE_IN_COMMUNICATION + speakerphone.
        /// Disable when the host project manages its own audio session.
        /// </summary>
        public bool manageAudioSession = true;

        /// <summary>
        /// Android only. When <c>true</c>, <see cref="MicrophoneSource"/>
        /// returns AudioManager to MODE_NORMAL on stop. By default the
        /// MODE_IN_COMMUNICATION + speakerphone setup is left in place
        /// for the rest of the session — switching modes mid-session
        /// triggers an audio route change that can break the next capture.
        /// Enable only when you need normal media volume between captures.
        /// </summary>
        public bool androidReturnToNormalOnStop;

        /// <summary>
        /// Android only. Delay in milliseconds applied between switching
        /// AudioManager into MODE_IN_COMMUNICATION + speakerphone and
        /// starting <see cref="UnityEngine.Microphone"/>. The mode change
        /// triggers an asynchronous audio route change; without the wait
        /// the mic starts on the old route and produces no samples until
        /// the routing settles, which often exceeds
        /// <see cref="micStartTimeoutSec"/>. Only applied on the first
        /// capture (subsequent captures reuse the already-configured
        /// mode).
        /// </summary>
        public int androidAudioSessionSettleMs = 500;
    }
}
