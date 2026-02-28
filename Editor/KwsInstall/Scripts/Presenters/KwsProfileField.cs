namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters
{
    /// <summary>
    /// Identifies a single editable field on <see cref="Kws.Data.KwsProfile"/>.
    /// Used by <see cref="KwsProfileFieldBinder"/> to route change events.
    /// </summary>
    internal enum KwsProfileField
    {
        // Identity
        ProfileName,
        SourceUrl,

        // Common
        SampleRate,
        FeatureDim,
        NumThreads,
        Provider,

        // Transducer model files
        Tokens,
        Encoder,
        Decoder,
        Joiner,

        // Keywords
        KeywordsFile,
        CustomKeywords,
        KeywordsScore,
        KeywordsThreshold,
        MaxActivePaths,
        NumTrailingBlanks,

        // Remote
        RemoteBaseUrl
    }
}
