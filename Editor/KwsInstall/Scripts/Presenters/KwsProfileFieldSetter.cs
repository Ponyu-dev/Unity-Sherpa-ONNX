using PonyuDev.SherpaOnnx.Kws.Data;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters
{
    /// <summary>
    /// Applies a value to the correct field on <see cref="KwsProfile"/>
    /// based on <see cref="KwsProfileField"/> enum.
    /// </summary>
    internal static class KwsProfileFieldSetter
    {
        internal static void SetString(KwsProfile p, KwsProfileField f, string v)
        {
            switch (f)
            {
                case KwsProfileField.ProfileName: p.profileName = v; break;
                case KwsProfileField.SourceUrl: p.sourceUrl = v; break;
                case KwsProfileField.Provider: p.provider = v; break;
                case KwsProfileField.Tokens: p.tokens = v; break;
                case KwsProfileField.Encoder: p.encoder = v; break;
                case KwsProfileField.Decoder: p.decoder = v; break;
                case KwsProfileField.Joiner: p.joiner = v; break;
                case KwsProfileField.KeywordsFile: p.keywordsFile = v; break;
                case KwsProfileField.CustomKeywords: p.customKeywords = v; break;
                case KwsProfileField.RemoteBaseUrl: p.remoteBaseUrl = v; break;
            }
        }

        internal static void SetFloat(KwsProfile p, KwsProfileField f, float v)
        {
            switch (f)
            {
                case KwsProfileField.KeywordsScore: p.keywordsScore = v; break;
                case KwsProfileField.KeywordsThreshold: p.keywordsThreshold = v; break;
            }
        }

        internal static void SetInt(KwsProfile p, KwsProfileField f, int v)
        {
            switch (f)
            {
                case KwsProfileField.SampleRate: p.sampleRate = v; break;
                case KwsProfileField.FeatureDim: p.featureDim = v; break;
                case KwsProfileField.NumThreads: p.numThreads = v; break;
                case KwsProfileField.MaxActivePaths: p.maxActivePaths = v; break;
                case KwsProfileField.NumTrailingBlanks: p.numTrailingBlanks = v; break;
            }
        }
    }
}
