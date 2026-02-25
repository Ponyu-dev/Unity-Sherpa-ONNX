namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    /// <summary>
    /// Identifies a single editable field on <see cref="Vad.Data.VadProfile"/>.
    /// Used by <see cref="VadProfileFieldBinder"/> to route change events.
    /// </summary>
    internal enum VadProfileField
    {
        // Identity
        ProfileName,
        SourceUrl,

        // Common
        SampleRate,
        NumThreads,
        Provider,

        // Thresholds
        Threshold,
        MinSilenceDuration,
        MinSpeechDuration,
        MaxSpeechDuration,

        // Model
        Model,
        WindowSize,

        // Buffer
        BufferSizeInSeconds
    }
}
