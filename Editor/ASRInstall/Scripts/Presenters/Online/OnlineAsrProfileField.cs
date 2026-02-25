namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    /// <summary>
    /// Identifies a single editable field on
    /// <see cref="Asr.Online.Data.OnlineAsrProfile"/>.
    /// </summary>
    internal enum OnlineAsrProfileField
    {
        // Identity
        ProfileName,
        SourceUrl,

        // Common
        NumThreads,
        Provider,
        Tokens,

        // Feature
        SampleRate,
        FeatureDim,

        // Recognizer
        DecodingMethod,
        MaxActivePaths,
        HotwordsFile,
        HotwordsScore,
        RuleFsts,
        RuleFars,
        BlankPenalty,

        // Endpoint detection
        Rule1MinTrailingSilence,
        Rule2MinTrailingSilence,
        Rule3MinUtteranceLength,

        // CtcFstDecoder
        CtcFstDecoderGraph,
        CtcFstDecoderMaxActive,

        // Transducer
        TransducerEncoder,
        TransducerDecoder,
        TransducerJoiner,

        // Paraformer
        ParaformerEncoder,
        ParaformerDecoder,

        // Zipformer2Ctc
        Zipformer2CtcModel,

        // NemoCtc
        NemoCtcModel,

        // ToneCtc
        ToneCtcModel,

        // Remote
        RemoteBaseUrl
    }
}
