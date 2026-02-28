#if SHERPA_ONNX
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Kws.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Kws.Config
{
    /// <summary>
    /// Maps a <see cref="KwsProfile"/> to a native
    /// <see cref="KeywordSpotterConfig"/> struct.
    /// </summary>
    public static class KwsConfigBuilder
    {
        public static KeywordSpotterConfig Build(KwsProfile profile, string modelDir)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] KwsConfigBuilder.Build: " +
                $"profile='{profile.profileName}', " +
                $"modelType={profile.modelType}, " +
                $"modelDir='{modelDir}'");

            var config = new KeywordSpotterConfig();

            // ── FeatureConfig ──

            config.FeatConfig.SampleRate = profile.sampleRate;
            config.FeatConfig.FeatureDim = profile.featureDim;

            // ── ModelConfig common ──

            config.ModelConfig.Tokens = R(modelDir, profile.tokens);
            config.ModelConfig.NumThreads = profile.numThreads;
            config.ModelConfig.Provider = profile.provider ?? "cpu";

            // ── KWS-specific ──

            config.MaxActivePaths = profile.maxActivePaths;
            config.NumTrailingBlanks = profile.numTrailingBlanks;
            config.KeywordsScore = profile.keywordsScore;
            config.KeywordsThreshold = profile.keywordsThreshold;

            // Keywords file (resolved to absolute path)
            config.KeywordsFile = R(modelDir, profile.keywordsFile);

            // Custom keywords via buffer
            if (!string.IsNullOrEmpty(profile.customKeywords))
            {
                config.KeywordsBuf = profile.customKeywords;
                config.KeywordsBufSize = System.Text.Encoding.UTF8
                    .GetByteCount(profile.customKeywords);
            }

            // ── Per-model sub-config ──

            switch (profile.modelType)
            {
                case KwsModelType.Transducer:
                    BuildTransducer(ref config, profile, modelDir);
                    break;
            }

            return config;
        }

        // ── Per-model builders ──

        private static void BuildTransducer(ref KeywordSpotterConfig c, KwsProfile p, string dir)
        {
            c.ModelConfig.Transducer.Encoder = R(dir, p.encoder);
            c.ModelConfig.Transducer.Decoder = R(dir, p.decoder);
            c.ModelConfig.Transducer.Joiner = R(dir, p.joiner);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] KWS Transducer: Encoder='{c.ModelConfig.Transducer.Encoder}'");
        }

        // ── Shorthand ──

        private static string R(string modelDir, string relativePath)
        {
            return KwsModelPathResolver.Resolve(modelDir, relativePath);
        }
    }
}
#endif
