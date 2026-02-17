#if SHERPA_ONNX
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Asr.Config;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Asr.Offline.Config
{
    /// <summary>
    /// Maps an <see cref="AsrProfile"/> to a native
    /// <see cref="OfflineRecognizerConfig"/> struct ready for engine creation.
    /// All relative file paths are resolved to absolute via
    /// <see cref="AsrModelPathResolver"/>.
    /// </summary>
    public static class AsrConfigBuilder
    {
        public static OfflineRecognizerConfig Build(
            AsrProfile profile, string modelDir)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] AsrConfigBuilder.Build: " +
                $"profile='{profile.profileName}', " +
                $"modelType={profile.modelType}, " +
                $"modelDir='{modelDir}'");

            var config = new OfflineRecognizerConfig();

            // ── FeatureConfig ──

            config.FeatConfig.SampleRate = profile.sampleRate;
            config.FeatConfig.FeatureDim = profile.featureDim;

            // ── ModelConfig common ──

            config.ModelConfig.Tokens = R(modelDir, profile.tokens);
            config.ModelConfig.NumThreads = profile.numThreads;
            config.ModelConfig.Provider = profile.provider ?? "cpu";

            // ── LmConfig ──

            config.LmConfig.Model = R(modelDir, profile.lmModel);
            config.LmConfig.Scale = profile.lmScale;

            // ── RecognizerConfig ──

            config.DecodingMethod = profile.decodingMethod ?? "greedy_search";
            config.MaxActivePaths = profile.maxActivePaths;
            config.HotwordsFile = R(modelDir, profile.hotwordsFile);
            config.HotwordsScore = profile.hotwordsScore;
            config.RuleFsts = R(modelDir, profile.ruleFsts);
            config.RuleFars = R(modelDir, profile.ruleFars);
            config.BlankPenalty = profile.blankPenalty;

            // ── Per-model sub-config ──

            switch (profile.modelType)
            {
                case AsrModelType.Transducer:
                    BuildTransducer(ref config, profile, modelDir);
                    break;
                case AsrModelType.Paraformer:
                    BuildParaformer(ref config, profile, modelDir);
                    break;
                case AsrModelType.Whisper:
                    BuildWhisper(ref config, profile, modelDir);
                    break;
                case AsrModelType.SenseVoice:
                    BuildSenseVoice(ref config, profile, modelDir);
                    break;
                case AsrModelType.Moonshine:
                    BuildMoonshine(ref config, profile, modelDir);
                    break;
                case AsrModelType.NemoCtc:
                    BuildNemoCtc(ref config, profile, modelDir);
                    break;
                case AsrModelType.ZipformerCtc:
                    BuildZipformerCtc(ref config, profile, modelDir);
                    break;
                case AsrModelType.Tdnn:
                    BuildTdnn(ref config, profile, modelDir);
                    break;
                case AsrModelType.FireRedAsr:
                    BuildFireRedAsr(ref config, profile, modelDir);
                    break;
                case AsrModelType.Dolphin:
                    BuildDolphin(ref config, profile, modelDir);
                    break;
                case AsrModelType.Canary:
                    BuildCanary(ref config, profile, modelDir);
                    break;
                case AsrModelType.WenetCtc:
                    BuildWenetCtc(ref config, profile, modelDir);
                    break;
                case AsrModelType.Omnilingual:
                    BuildOmnilingual(ref config, profile, modelDir);
                    break;
                case AsrModelType.MedAsr:
                    BuildMedAsr(ref config, profile, modelDir);
                    break;
                case AsrModelType.FunAsrNano:
                    BuildFunAsrNano(ref config, profile, modelDir);
                    break;
            }

            return config;
        }

        // ── Per-model builders ──

        private static void BuildTransducer(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Transducer.Encoder = R(dir, p.transducerEncoder);
            c.ModelConfig.Transducer.Decoder = R(dir, p.transducerDecoder);
            c.ModelConfig.Transducer.Joiner = R(dir, p.transducerJoiner);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Transducer config: " +
                $"Encoder='{c.ModelConfig.Transducer.Encoder}', " +
                $"Decoder='{c.ModelConfig.Transducer.Decoder}', " +
                $"Joiner='{c.ModelConfig.Transducer.Joiner}'");
        }

        private static void BuildParaformer(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Paraformer.Model = R(dir, p.paraformerModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Paraformer config: " +
                $"Model='{c.ModelConfig.Paraformer.Model}'");
        }

        private static void BuildWhisper(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Whisper.Encoder = R(dir, p.whisperEncoder);
            c.ModelConfig.Whisper.Decoder = R(dir, p.whisperDecoder);
            c.ModelConfig.Whisper.Language = p.whisperLanguage ?? "";
            c.ModelConfig.Whisper.Task = p.whisperTask ?? "transcribe";
            c.ModelConfig.Whisper.TailPaddings = p.whisperTailPaddings;
            c.ModelConfig.Whisper.EnableTokenTimestamps =
                p.whisperEnableTokenTimestamps ? 1 : 0;
            c.ModelConfig.Whisper.EnableSegmentTimestamps =
                p.whisperEnableSegmentTimestamps ? 1 : 0;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Whisper config: " +
                $"Encoder='{c.ModelConfig.Whisper.Encoder}', " +
                $"Decoder='{c.ModelConfig.Whisper.Decoder}', " +
                $"Language='{c.ModelConfig.Whisper.Language}', " +
                $"Task='{c.ModelConfig.Whisper.Task}'");
        }

        private static void BuildSenseVoice(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.SenseVoice.Model = R(dir, p.senseVoiceModel);
            c.ModelConfig.SenseVoice.Language = p.senseVoiceLanguage ?? "";
            c.ModelConfig.SenseVoice.UseInverseTextNormalization =
                p.senseVoiceUseInverseTextNormalization ? 1 : 0;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] SenseVoice config: " +
                $"Model='{c.ModelConfig.SenseVoice.Model}', " +
                $"Language='{c.ModelConfig.SenseVoice.Language}'");
        }

        private static void BuildMoonshine(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Moonshine.Preprocessor =
                R(dir, p.moonshinePreprocessor);
            c.ModelConfig.Moonshine.Encoder = R(dir, p.moonshineEncoder);
            c.ModelConfig.Moonshine.UncachedDecoder =
                R(dir, p.moonshineUncachedDecoder);
            c.ModelConfig.Moonshine.CachedDecoder =
                R(dir, p.moonshineCachedDecoder);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Moonshine config: " +
                $"Preprocessor='{c.ModelConfig.Moonshine.Preprocessor}', " +
                $"Encoder='{c.ModelConfig.Moonshine.Encoder}'");
        }

        private static void BuildNemoCtc(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.NeMoCtc.Model = R(dir, p.nemoCtcModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] NemoCtc config: " +
                $"Model='{c.ModelConfig.NeMoCtc.Model}'");
        }

        private static void BuildZipformerCtc(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.ZipformerCtc.Model = R(dir, p.zipformerCtcModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ZipformerCtc config: " +
                $"Model='{c.ModelConfig.ZipformerCtc.Model}'");
        }

        private static void BuildTdnn(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Tdnn.Model = R(dir, p.tdnnModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Tdnn config: " +
                $"Model='{c.ModelConfig.Tdnn.Model}'");
        }

        private static void BuildFireRedAsr(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.FireRedAsr.Encoder = R(dir, p.fireRedAsrEncoder);
            c.ModelConfig.FireRedAsr.Decoder = R(dir, p.fireRedAsrDecoder);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] FireRedAsr config: " +
                $"Encoder='{c.ModelConfig.FireRedAsr.Encoder}', " +
                $"Decoder='{c.ModelConfig.FireRedAsr.Decoder}'");
        }

        private static void BuildDolphin(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Dolphin.Model = R(dir, p.dolphinModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Dolphin config: " +
                $"Model='{c.ModelConfig.Dolphin.Model}'");
        }

        private static void BuildCanary(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Canary.Encoder = R(dir, p.canaryEncoder);
            c.ModelConfig.Canary.Decoder = R(dir, p.canaryDecoder);
            c.ModelConfig.Canary.SrcLang = p.canarySrcLang ?? "en";
            c.ModelConfig.Canary.TgtLang = p.canaryTgtLang ?? "en";
            c.ModelConfig.Canary.UsePnc = p.canaryUsePnc ? 1 : 0;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Canary config: " +
                $"Encoder='{c.ModelConfig.Canary.Encoder}', " +
                $"Decoder='{c.ModelConfig.Canary.Decoder}', " +
                $"SrcLang='{c.ModelConfig.Canary.SrcLang}', " +
                $"TgtLang='{c.ModelConfig.Canary.TgtLang}'");
        }

        private static void BuildWenetCtc(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.WenetCtc.Model = R(dir, p.wenetCtcModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] WenetCtc config: " +
                $"Model='{c.ModelConfig.WenetCtc.Model}'");
        }

        private static void BuildOmnilingual(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.Omnilingual.Model = R(dir, p.omnilingualModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Omnilingual config: " +
                $"Model='{c.ModelConfig.Omnilingual.Model}'");
        }

        private static void BuildMedAsr(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.MedAsr.Model = R(dir, p.medAsrModel);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] MedAsr config: " +
                $"Model='{c.ModelConfig.MedAsr.Model}'");
        }

        private static void BuildFunAsrNano(
            ref OfflineRecognizerConfig c, AsrProfile p, string dir)
        {
            c.ModelConfig.FunAsrNano.EncoderAdaptor =
                R(dir, p.funAsrNanoEncoderAdaptor);
            c.ModelConfig.FunAsrNano.LLM = R(dir, p.funAsrNanoLlm);
            c.ModelConfig.FunAsrNano.Embedding =
                R(dir, p.funAsrNanoEmbedding);
            c.ModelConfig.FunAsrNano.Tokenizer =
                R(dir, p.funAsrNanoTokenizer);
            c.ModelConfig.FunAsrNano.SystemPrompt =
                p.funAsrNanoSystemPrompt ?? "You are a helpful assistant.";
            c.ModelConfig.FunAsrNano.UserPrompt =
                p.funAsrNanoUserPrompt ?? "";
            c.ModelConfig.FunAsrNano.MaxNewTokens =
                p.funAsrNanoMaxNewTokens;
            c.ModelConfig.FunAsrNano.Temperature =
                p.funAsrNanoTemperature;
            c.ModelConfig.FunAsrNano.TopP = p.funAsrNanoTopP;
            c.ModelConfig.FunAsrNano.Seed = p.funAsrNanoSeed;
            c.ModelConfig.FunAsrNano.Language =
                p.funAsrNanoLanguage ?? "";
            c.ModelConfig.FunAsrNano.Itn = p.funAsrNanoItn ? 1 : 0;
            c.ModelConfig.FunAsrNano.Hotwords =
                p.funAsrNanoHotwords ?? "";

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] FunAsrNano config: " +
                $"LLM='{c.ModelConfig.FunAsrNano.LLM}', " +
                $"Language='{c.ModelConfig.FunAsrNano.Language}'");
        }

        // ── Shorthand ──

        private static string R(string modelDir, string relativePath)
        {
            return AsrModelPathResolver.Resolve(modelDir, relativePath);
        }
    }
}
#endif