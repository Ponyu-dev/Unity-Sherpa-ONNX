namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    /// <summary>
    /// Identifies a single editable field on <see cref="Asr.Offline.Data.AsrProfile"/>.
    /// </summary>
    internal enum AsrProfileField
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

        // LM
        LmModel,
        LmScale,

        // Transducer
        TransducerEncoder,
        TransducerDecoder,
        TransducerJoiner,

        // Paraformer
        ParaformerModel,

        // Whisper
        WhisperEncoder,
        WhisperDecoder,
        WhisperLanguage,
        WhisperTask,
        WhisperTailPaddings,

        // SenseVoice
        SenseVoiceModel,
        SenseVoiceLanguage,

        // Moonshine
        MoonshinePreprocessor,
        MoonshineEncoder,
        MoonshineUncachedDecoder,
        MoonshineCachedDecoder,

        // NemoCtc
        NemoCtcModel,

        // ZipformerCtc
        ZipformerCtcModel,

        // Tdnn
        TdnnModel,

        // FireRedAsr
        FireRedAsrEncoder,
        FireRedAsrDecoder,

        // Dolphin
        DolphinModel,

        // Canary
        CanaryEncoder,
        CanaryDecoder,
        CanarySrcLang,
        CanaryTgtLang,

        // WenetCtc
        WenetCtcModel,

        // Omnilingual
        OmnilingualModel,

        // MedAsr
        MedAsrModel,

        // FunAsrNano
        FunAsrNanoEncoderAdaptor,
        FunAsrNanoLlm,
        FunAsrNanoEmbedding,
        FunAsrNanoTokenizer,
        FunAsrNanoSystemPrompt,
        FunAsrNanoUserPrompt,
        FunAsrNanoMaxNewTokens,
        FunAsrNanoTemperature,
        FunAsrNanoTopP,
        FunAsrNanoSeed,
        FunAsrNanoLanguage,
        FunAsrNanoHotwords
    }
}
