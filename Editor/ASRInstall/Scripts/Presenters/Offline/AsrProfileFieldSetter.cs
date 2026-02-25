using PonyuDev.SherpaOnnx.Asr.Offline.Data;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    /// <summary>
    /// Applies a value to the correct field on <see cref="AsrProfile"/>
    /// based on <see cref="AsrProfileField"/> enum.
    /// </summary>
    internal static class AsrProfileFieldSetter
    {
        internal static void SetString(
            AsrProfile p, AsrProfileField f, string v)
        {
            switch (f)
            {
                case AsrProfileField.ProfileName: p.profileName = v; break;
                case AsrProfileField.SourceUrl: p.sourceUrl = v; break;
                case AsrProfileField.Provider: p.provider = v; break;
                case AsrProfileField.Tokens: p.tokens = v; break;
                case AsrProfileField.DecodingMethod: p.decodingMethod = v; break;
                case AsrProfileField.HotwordsFile: p.hotwordsFile = v; break;
                case AsrProfileField.RuleFsts: p.ruleFsts = v; break;
                case AsrProfileField.RuleFars: p.ruleFars = v; break;
                case AsrProfileField.LmModel: p.lmModel = v; break;

                // Transducer
                case AsrProfileField.TransducerEncoder: p.transducerEncoder = v; break;
                case AsrProfileField.TransducerDecoder: p.transducerDecoder = v; break;
                case AsrProfileField.TransducerJoiner: p.transducerJoiner = v; break;

                // Paraformer
                case AsrProfileField.ParaformerModel: p.paraformerModel = v; break;

                // Whisper
                case AsrProfileField.WhisperEncoder: p.whisperEncoder = v; break;
                case AsrProfileField.WhisperDecoder: p.whisperDecoder = v; break;
                case AsrProfileField.WhisperLanguage: p.whisperLanguage = v; break;
                case AsrProfileField.WhisperTask: p.whisperTask = v; break;

                // SenseVoice
                case AsrProfileField.SenseVoiceModel: p.senseVoiceModel = v; break;
                case AsrProfileField.SenseVoiceLanguage: p.senseVoiceLanguage = v; break;

                // Moonshine
                case AsrProfileField.MoonshinePreprocessor: p.moonshinePreprocessor = v; break;
                case AsrProfileField.MoonshineEncoder: p.moonshineEncoder = v; break;
                case AsrProfileField.MoonshineUncachedDecoder: p.moonshineUncachedDecoder = v; break;
                case AsrProfileField.MoonshineCachedDecoder: p.moonshineCachedDecoder = v; break;

                // Single-model types
                case AsrProfileField.NemoCtcModel: p.nemoCtcModel = v; break;
                case AsrProfileField.ZipformerCtcModel: p.zipformerCtcModel = v; break;
                case AsrProfileField.TdnnModel: p.tdnnModel = v; break;
                case AsrProfileField.DolphinModel: p.dolphinModel = v; break;
                case AsrProfileField.WenetCtcModel: p.wenetCtcModel = v; break;
                case AsrProfileField.OmnilingualModel: p.omnilingualModel = v; break;
                case AsrProfileField.MedAsrModel: p.medAsrModel = v; break;

                // FireRedAsr
                case AsrProfileField.FireRedAsrEncoder: p.fireRedAsrEncoder = v; break;
                case AsrProfileField.FireRedAsrDecoder: p.fireRedAsrDecoder = v; break;

                // Canary
                case AsrProfileField.CanaryEncoder: p.canaryEncoder = v; break;
                case AsrProfileField.CanaryDecoder: p.canaryDecoder = v; break;
                case AsrProfileField.CanarySrcLang: p.canarySrcLang = v; break;
                case AsrProfileField.CanaryTgtLang: p.canaryTgtLang = v; break;

                // FunAsrNano
                case AsrProfileField.FunAsrNanoEncoderAdaptor: p.funAsrNanoEncoderAdaptor = v; break;
                case AsrProfileField.FunAsrNanoLlm: p.funAsrNanoLlm = v; break;
                case AsrProfileField.FunAsrNanoEmbedding: p.funAsrNanoEmbedding = v; break;
                case AsrProfileField.FunAsrNanoTokenizer: p.funAsrNanoTokenizer = v; break;
                case AsrProfileField.FunAsrNanoSystemPrompt: p.funAsrNanoSystemPrompt = v; break;
                case AsrProfileField.FunAsrNanoUserPrompt: p.funAsrNanoUserPrompt = v; break;
                case AsrProfileField.FunAsrNanoLanguage: p.funAsrNanoLanguage = v; break;
                case AsrProfileField.FunAsrNanoHotwords: p.funAsrNanoHotwords = v; break;

                // Remote
                case AsrProfileField.RemoteBaseUrl: p.remoteBaseUrl = v; break;
            }
        }

        internal static void SetFloat(
            AsrProfile p, AsrProfileField f, float v)
        {
            switch (f)
            {
                case AsrProfileField.HotwordsScore: p.hotwordsScore = v; break;
                case AsrProfileField.BlankPenalty: p.blankPenalty = v; break;
                case AsrProfileField.LmScale: p.lmScale = v; break;
                case AsrProfileField.FunAsrNanoTemperature: p.funAsrNanoTemperature = v; break;
                case AsrProfileField.FunAsrNanoTopP: p.funAsrNanoTopP = v; break;
            }
        }

        internal static void SetInt(
            AsrProfile p, AsrProfileField f, int v)
        {
            switch (f)
            {
                case AsrProfileField.NumThreads: p.numThreads = v; break;
                case AsrProfileField.SampleRate: p.sampleRate = v; break;
                case AsrProfileField.FeatureDim: p.featureDim = v; break;
                case AsrProfileField.MaxActivePaths: p.maxActivePaths = v; break;
                case AsrProfileField.WhisperTailPaddings: p.whisperTailPaddings = v; break;
                case AsrProfileField.FunAsrNanoMaxNewTokens: p.funAsrNanoMaxNewTokens = v; break;
                case AsrProfileField.FunAsrNanoSeed: p.funAsrNanoSeed = v; break;
            }
        }
    }
}
