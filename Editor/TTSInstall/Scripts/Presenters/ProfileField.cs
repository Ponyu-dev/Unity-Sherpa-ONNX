namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Identifies a single editable field on <see cref="Tts.Data.TtsProfile"/>.
    /// Used by <see cref="ProfileFieldBinder"/> to route change events.
    /// </summary>
    internal enum ProfileField
    {
        // Identity
        ProfileName,

        // Generation
        SpeakerId,
        Speed,
        SilenceScale,
        MaxNumSentences,
        RuleFsts,
        RuleFars,
        NumThreads,
        Provider,

        // Vits
        VitsModel,
        VitsTokens,
        VitsLexicon,
        VitsDataDir,
        VitsDictDir,
        VitsNoiseScale,
        VitsNoiseScaleW,
        VitsLengthScale,

        // Matcha
        MatchaAcousticModel,
        MatchaVocoder,
        MatchaTokens,
        MatchaLexicon,
        MatchaDataDir,
        MatchaDictDir,
        MatchaNoiseScale,
        MatchaLengthScale,

        // Kokoro
        KokoroModel,
        KokoroVoices,
        KokoroTokens,
        KokoroDataDir,
        KokoroDictDir,
        KokoroLexicon,
        KokoroLang,
        KokoroLengthScale,

        // Kitten
        KittenModel,
        KittenVoices,
        KittenTokens,
        KittenDataDir,
        KittenLengthScale,

        // ZipVoice
        ZipVoiceTokens,
        ZipVoiceEncoder,
        ZipVoiceDecoder,
        ZipVoiceVocoder,
        ZipVoiceDataDir,
        ZipVoiceLexicon,
        ZipVoiceFeatScale,
        ZipVoiceTshift,
        ZipVoiceTargetRms,
        ZipVoiceGuidanceScale,

        // Pocket
        PocketLmFlow,
        PocketLmMain,
        PocketEncoder,
        PocketDecoder,
        PocketTextConditioner,
        PocketVocabJson,
        PocketTokenScoresJson,

        // Remote
        RemoteBaseUrl
    }
}
