#if SHERPA_ONNX
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Asr.Config;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Asr.Online.Config
{
    /// <summary>
    /// Maps an <see cref="OnlineAsrProfile"/> to a native
    /// <see cref="OnlineRecognizerConfig"/> struct.
    /// </summary>
    public static class OnlineAsrConfigBuilder
    {
        public static OnlineRecognizerConfig Build(
            OnlineAsrProfile profile, string modelDir)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] OnlineAsrConfigBuilder.Build: " +
                $"profile='{profile.profileName}', " +
                $"modelType={profile.modelType}");

            var config = new OnlineRecognizerConfig();

            // ── FeatureConfig ──

            config.FeatConfig.SampleRate = profile.sampleRate;
            config.FeatConfig.FeatureDim = profile.featureDim;

            // ── ModelConfig common ──

            config.ModelConfig.Tokens = R(modelDir, profile.tokens);
            config.ModelConfig.NumThreads = profile.numThreads;
            config.ModelConfig.Provider = profile.provider ?? "cpu";

            // ── RecognizerConfig ──

            config.DecodingMethod = profile.decodingMethod ?? "greedy_search";
            config.MaxActivePaths = profile.maxActivePaths;
            config.HotwordsFile = R(modelDir, profile.hotwordsFile);
            config.HotwordsScore = profile.hotwordsScore;
            config.RuleFsts = R(modelDir, profile.ruleFsts);
            config.RuleFars = R(modelDir, profile.ruleFars);
            config.BlankPenalty = profile.blankPenalty;

            // ── Endpoint detection ──

            config.EnableEndpoint = profile.enableEndpoint ? 1 : 0;
            config.Rule1MinTrailingSilence = profile.rule1MinTrailingSilence;
            config.Rule2MinTrailingSilence = profile.rule2MinTrailingSilence;
            config.Rule3MinUtteranceLength = profile.rule3MinUtteranceLength;

            // ── CtcFstDecoderConfig ──

            config.CtcFstDecoderConfig.Graph =
                R(modelDir, profile.ctcFstDecoderGraph);
            config.CtcFstDecoderConfig.MaxActive =
                profile.ctcFstDecoderMaxActive;

            // ── Per-model sub-config ──

            switch (profile.modelType)
            {
                case OnlineAsrModelType.Transducer:
                    BuildTransducer(ref config, profile, modelDir);
                    break;
                case OnlineAsrModelType.Paraformer:
                    BuildParaformer(ref config, profile, modelDir);
                    break;
                case OnlineAsrModelType.Zipformer2Ctc:
                    BuildZipformer2Ctc(ref config, profile, modelDir);
                    break;
                case OnlineAsrModelType.NemoCtc:
                    BuildNemoCtc(ref config, profile, modelDir);
                    break;
                case OnlineAsrModelType.ToneCtc:
                    BuildToneCtc(ref config, profile, modelDir);
                    break;
            }

            return config;
        }

        // ── Per-model builders ──

        private static void BuildTransducer(
            ref OnlineRecognizerConfig c, OnlineAsrProfile p, string dir)
        {
            c.ModelConfig.Transducer.Encoder = R(dir, p.transducerEncoder);
            c.ModelConfig.Transducer.Decoder = R(dir, p.transducerDecoder);
            c.ModelConfig.Transducer.Joiner = R(dir, p.transducerJoiner);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online Transducer: " +
                $"Encoder='{c.ModelConfig.Transducer.Encoder}'");
        }

        private static void BuildParaformer(
            ref OnlineRecognizerConfig c, OnlineAsrProfile p, string dir)
        {
            c.ModelConfig.Paraformer.Encoder = R(dir, p.paraformerEncoder);
            c.ModelConfig.Paraformer.Decoder = R(dir, p.paraformerDecoder);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online Paraformer: " +
                $"Encoder='{c.ModelConfig.Paraformer.Encoder}'");
        }

        private static void BuildZipformer2Ctc(
            ref OnlineRecognizerConfig c, OnlineAsrProfile p, string dir)
        {
            c.ModelConfig.Zipformer2Ctc.Model = R(dir, p.zipformer2CtcModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online Zipformer2Ctc: " +
                $"Model='{c.ModelConfig.Zipformer2Ctc.Model}'");
        }

        private static void BuildNemoCtc(
            ref OnlineRecognizerConfig c, OnlineAsrProfile p, string dir)
        {
            c.ModelConfig.NemoCtc.Model = R(dir, p.nemoCtcModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online NemoCtc: " +
                $"Model='{c.ModelConfig.NemoCtc.Model}'");
        }

        private static void BuildToneCtc(
            ref OnlineRecognizerConfig c, OnlineAsrProfile p, string dir)
        {
            c.ModelConfig.ToneCtc.Model = R(dir, p.toneCtcModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online ToneCtc: " +
                $"Model='{c.ModelConfig.ToneCtc.Model}'");
        }

        // ── Shorthand ──

        private static string R(string modelDir, string relativePath)
        {
            return AsrModelPathResolver.Resolve(modelDir, relativePath);
        }
    }
}
#endif
