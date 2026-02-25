using PonyuDev.SherpaOnnx.Asr.Online.Data;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    /// <summary>
    /// Applies a value to the correct field on
    /// <see cref="OnlineAsrProfile"/> based on enum.
    /// </summary>
    internal static class OnlineAsrProfileFieldSetter
    {
        internal static void SetString(
            OnlineAsrProfile p, OnlineAsrProfileField f, string v)
        {
            switch (f)
            {
                case OnlineAsrProfileField.ProfileName: p.profileName = v; break;
                case OnlineAsrProfileField.SourceUrl: p.sourceUrl = v; break;
                case OnlineAsrProfileField.Provider: p.provider = v; break;
                case OnlineAsrProfileField.Tokens: p.tokens = v; break;
                case OnlineAsrProfileField.DecodingMethod: p.decodingMethod = v; break;
                case OnlineAsrProfileField.HotwordsFile: p.hotwordsFile = v; break;
                case OnlineAsrProfileField.RuleFsts: p.ruleFsts = v; break;
                case OnlineAsrProfileField.RuleFars: p.ruleFars = v; break;
                case OnlineAsrProfileField.CtcFstDecoderGraph: p.ctcFstDecoderGraph = v; break;

                // Transducer
                case OnlineAsrProfileField.TransducerEncoder: p.transducerEncoder = v; break;
                case OnlineAsrProfileField.TransducerDecoder: p.transducerDecoder = v; break;
                case OnlineAsrProfileField.TransducerJoiner: p.transducerJoiner = v; break;

                // Paraformer
                case OnlineAsrProfileField.ParaformerEncoder: p.paraformerEncoder = v; break;
                case OnlineAsrProfileField.ParaformerDecoder: p.paraformerDecoder = v; break;

                // Single-model types
                case OnlineAsrProfileField.Zipformer2CtcModel: p.zipformer2CtcModel = v; break;
                case OnlineAsrProfileField.NemoCtcModel: p.nemoCtcModel = v; break;
                case OnlineAsrProfileField.ToneCtcModel: p.toneCtcModel = v; break;
            }
        }

        internal static void SetFloat(
            OnlineAsrProfile p, OnlineAsrProfileField f, float v)
        {
            switch (f)
            {
                case OnlineAsrProfileField.HotwordsScore: p.hotwordsScore = v; break;
                case OnlineAsrProfileField.BlankPenalty: p.blankPenalty = v; break;
                case OnlineAsrProfileField.Rule1MinTrailingSilence: p.rule1MinTrailingSilence = v; break;
                case OnlineAsrProfileField.Rule2MinTrailingSilence: p.rule2MinTrailingSilence = v; break;
                case OnlineAsrProfileField.Rule3MinUtteranceLength: p.rule3MinUtteranceLength = v; break;
            }
        }

        internal static void SetInt(
            OnlineAsrProfile p, OnlineAsrProfileField f, int v)
        {
            switch (f)
            {
                case OnlineAsrProfileField.NumThreads: p.numThreads = v; break;
                case OnlineAsrProfileField.SampleRate: p.sampleRate = v; break;
                case OnlineAsrProfileField.FeatureDim: p.featureDim = v; break;
                case OnlineAsrProfileField.MaxActivePaths: p.maxActivePaths = v; break;
                case OnlineAsrProfileField.CtcFstDecoderMaxActive: p.ctcFstDecoderMaxActive = v; break;
            }
        }
    }
}
