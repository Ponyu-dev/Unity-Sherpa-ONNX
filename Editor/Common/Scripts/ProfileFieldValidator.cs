using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Kws.Data;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Vad.Data;

namespace PonyuDev.SherpaOnnx.Editor.Common
{
    internal static class ProfileFieldValidator
    {
        internal static bool HasMissingFields(AsrProfile p)
        {
            if (Empty(p.tokens)) return true;

            return p.modelType switch
            {
                AsrModelType.Transducer   => Empty(p.transducerEncoder) || Empty(p.transducerDecoder) || Empty(p.transducerJoiner),
                AsrModelType.Paraformer   => Empty(p.paraformerModel),
                AsrModelType.Whisper      => Empty(p.whisperEncoder) || Empty(p.whisperDecoder),
                AsrModelType.SenseVoice   => Empty(p.senseVoiceModel),
                AsrModelType.Moonshine    => Empty(p.moonshinePreprocessor) || Empty(p.moonshineEncoder) || Empty(p.moonshineUncachedDecoder) || Empty(p.moonshineCachedDecoder),
                AsrModelType.NemoCtc      => Empty(p.nemoCtcModel),
                AsrModelType.ZipformerCtc => Empty(p.zipformerCtcModel),
                AsrModelType.Tdnn         => Empty(p.tdnnModel),
                AsrModelType.FireRedAsr   => Empty(p.fireRedAsrEncoder) || Empty(p.fireRedAsrDecoder),
                AsrModelType.Dolphin      => Empty(p.dolphinModel),
                AsrModelType.Canary       => Empty(p.canaryEncoder) || Empty(p.canaryDecoder),
                AsrModelType.WenetCtc     => Empty(p.wenetCtcModel),
                AsrModelType.Omnilingual  => Empty(p.omnilingualModel),
                AsrModelType.MedAsr       => Empty(p.medAsrModel),
                AsrModelType.FunAsrNano   => Empty(p.funAsrNanoEncoderAdaptor) || Empty(p.funAsrNanoLlm) || Empty(p.funAsrNanoEmbedding) || Empty(p.funAsrNanoTokenizer),
                _ => false
            };
        }

        internal static bool HasMissingFields(OnlineAsrProfile p)
        {
            if (Empty(p.tokens)) return true;

            return p.modelType switch
            {
                OnlineAsrModelType.Transducer    => Empty(p.transducerEncoder) || Empty(p.transducerDecoder) || Empty(p.transducerJoiner),
                OnlineAsrModelType.Paraformer    => Empty(p.paraformerEncoder) || Empty(p.paraformerDecoder),
                OnlineAsrModelType.Zipformer2Ctc => Empty(p.zipformer2CtcModel),
                OnlineAsrModelType.NemoCtc       => Empty(p.nemoCtcModel),
                OnlineAsrModelType.ToneCtc       => Empty(p.toneCtcModel),
                _ => false
            };
        }

        internal static bool HasMissingFields(TtsProfile p)
        {
            return p.modelType switch
            {
                TtsModelType.Vits    => Empty(p.vitsModel) || Empty(p.vitsTokens),
                TtsModelType.Matcha  => Empty(p.matchaAcousticModel) || Empty(p.matchaVocoder) || Empty(p.matchaTokens),
                TtsModelType.Kokoro  => Empty(p.kokoroModel) || Empty(p.kokoroVoices) || Empty(p.kokoroTokens),
                TtsModelType.Kitten  => Empty(p.kittenModel) || Empty(p.kittenVoices) || Empty(p.kittenTokens),
                TtsModelType.ZipVoice => Empty(p.zipVoiceTokens) || Empty(p.zipVoiceEncoder) || Empty(p.zipVoiceDecoder) || Empty(p.zipVoiceVocoder),
                TtsModelType.Pocket  => Empty(p.pocketLmFlow) || Empty(p.pocketLmMain) || Empty(p.pocketEncoder) || Empty(p.pocketDecoder) || Empty(p.pocketTextConditioner) || Empty(p.pocketVocabJson) || Empty(p.pocketTokenScoresJson),
                _ => false
            };
        }

        internal static bool HasMissingFields(VadProfile p)
        {
            return Empty(p.model);
        }

        internal static bool HasMissingFields(KwsProfile p)
        {
            if (Empty(p.tokens)) return true;
            if (Empty(p.keywordsFile) && Empty(p.customKeywords)) return true;

            return p.modelType switch
            {
                KwsModelType.Transducer => Empty(p.encoder) || Empty(p.decoder) || Empty(p.joiner),
                _ => false
            };
        }

        private static bool Empty(string value) => string.IsNullOrEmpty(value);
    }
}
