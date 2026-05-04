namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// Algorithm used by <see cref="Resampler"/> when the microphone's
    /// hardware sample rate differs from the requested target rate.
    /// </summary>
    public enum ResamplingMode
    {
        /// <summary>
        /// Linear interpolation only. Cheapest, allocates one output array.
        /// Introduces aliasing on downsampling, but the artifacts sit above
        /// 8 kHz where ASR / VAD models do not look — fine for speech.
        /// </summary>
        Linear = 0,

        /// <summary>
        /// Single-pole IIR low-pass at the Nyquist of the target rate,
        /// followed by linear interpolation. Fewer aliasing artifacts at
        /// the cost of one extra pass over the buffer. Pick this if the
        /// resampled audio is also routed to playback or analysis other
        /// than ASR/VAD.
        /// </summary>
        Lowpass = 1
    }
}
