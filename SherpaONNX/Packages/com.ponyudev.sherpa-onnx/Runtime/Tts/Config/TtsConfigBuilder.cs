#if SHERPA_ONNX
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Tts.Config
{
    /// <summary>
    /// Maps a <see cref="TtsProfile"/> to a native
    /// <see cref="OfflineTtsConfig"/> struct ready for engine creation.
    /// All relative file paths are resolved to absolute via
    /// <see cref="TtsModelPathResolver"/>.
    /// </summary>
    public static class TtsConfigBuilder
    {
        public static OfflineTtsConfig Build(TtsProfile profile, string modelDir)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TtsConfigBuilder.Build: " +
                $"profile='{profile.profileName}', " +
                $"modelType={profile.modelType}, " +
                $"modelSource={profile.modelSource}, " +
                $"modelDir='{modelDir}'");

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TtsConfigBuilder: " +
                $"silenceScale={profile.silenceScale}, " +
                $"maxNumSentences={profile.maxNumSentences}, " +
                $"numThreads={profile.numThreads}, " +
                $"provider='{profile.provider}', " +
                $"speed={profile.speed}, " +
                $"speakerId={profile.speakerId}");

            var config = new OfflineTtsConfig
            {
                RuleFsts = R(modelDir, profile.ruleFsts),
                RuleFars = R(modelDir, profile.ruleFars),
                MaxNumSentences = profile.maxNumSentences,
                SilenceScale = profile.silenceScale
            };

            config.Model.NumThreads = profile.numThreads;
            config.Model.Provider = profile.provider ?? "cpu";

            switch (profile.modelType)
            {
                case TtsModelType.Vits:
                    BuildVits(ref config.Model.Vits, profile, modelDir);
                    break;
                case TtsModelType.Matcha:
                    BuildMatcha(ref config.Model.Matcha, profile, modelDir);
                    break;
                case TtsModelType.Kokoro:
                    BuildKokoro(ref config.Model.Kokoro, profile, modelDir);
                    break;
                case TtsModelType.Kitten:
                    BuildKitten(ref config.Model.Kitten, profile, modelDir);
                    break;
                case TtsModelType.ZipVoice:
                    BuildZipVoice(ref config.Model.ZipVoice, profile, modelDir);
                    break;
                case TtsModelType.Pocket:
                    BuildPocket(ref config.Model.Pocket, profile, modelDir);
                    break;
            }

            return config;
        }

        // ── Per-model builders ──

        private static void BuildVits(
            ref OfflineTtsVitsModelConfig c,
            TtsProfile p,
            string dir)
        {
            c.Model = R(dir, p.vitsModel);
            c.Lexicon = R(dir, p.vitsLexicon);
            c.Tokens = R(dir, p.vitsTokens);
            c.DataDir = R(dir, p.vitsDataDir);
            c.DictDir = R(dir, p.vitsDictDir);
            c.NoiseScale = p.vitsNoiseScale;
            c.NoiseScaleW = p.vitsNoiseScaleW;
            c.LengthScale = p.vitsLengthScale;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] VITS config: " +
                $"Model='{c.Model}', Tokens='{c.Tokens}', " +
                $"Lexicon='{c.Lexicon}', DataDir='{c.DataDir}', " +
                $"DictDir='{c.DictDir}', " +
                $"NoiseScale={c.NoiseScale}, NoiseScaleW={c.NoiseScaleW}, " +
                $"LengthScale={c.LengthScale}");
        }

        private static void BuildMatcha(
            ref OfflineTtsMatchaModelConfig c,
            TtsProfile p,
            string dir)
        {
            c.AcousticModel = R(dir, p.matchaAcousticModel);
            c.Vocoder = R(dir, p.matchaVocoder);
            c.Lexicon = R(dir, p.matchaLexicon);
            c.Tokens = R(dir, p.matchaTokens);
            c.DataDir = R(dir, p.matchaDataDir);
            c.DictDir = R(dir, p.matchaDictDir);
            c.NoiseScale = p.matchaNoiseScale;
            c.LengthScale = p.matchaLengthScale;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Matcha config: " +
                $"AcousticModel='{c.AcousticModel}', " +
                $"Vocoder='{c.Vocoder}', Tokens='{c.Tokens}', " +
                $"Lexicon='{c.Lexicon}', DataDir='{c.DataDir}', " +
                $"DictDir='{c.DictDir}', " +
                $"NoiseScale={c.NoiseScale}, LengthScale={c.LengthScale}");
        }

        private static void BuildKokoro(
            ref OfflineTtsKokoroModelConfig c,
            TtsProfile p,
            string dir)
        {
            c.Model = R(dir, p.kokoroModel);
            c.Voices = R(dir, p.kokoroVoices);
            c.Tokens = R(dir, p.kokoroTokens);
            c.DataDir = R(dir, p.kokoroDataDir);
            c.DictDir = R(dir, p.kokoroDictDir);
            c.Lexicon = R(dir, p.kokoroLexicon);
            c.Lang = p.kokoroLang ?? "";
            c.LengthScale = p.kokoroLengthScale;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Kokoro config: " +
                $"Model='{c.Model}', Voices='{c.Voices}', " +
                $"Tokens='{c.Tokens}', DataDir='{c.DataDir}', " +
                $"DictDir='{c.DictDir}', Lexicon='{c.Lexicon}', " +
                $"Lang='{c.Lang}', LengthScale={c.LengthScale}");
        }

        private static void BuildKitten(
            ref OfflineTtsKittenModelConfig c,
            TtsProfile p,
            string dir)
        {
            c.Model = R(dir, p.kittenModel);
            c.Voices = R(dir, p.kittenVoices);
            c.Tokens = R(dir, p.kittenTokens);
            c.DataDir = R(dir, p.kittenDataDir);
            c.LengthScale = p.kittenLengthScale;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Kitten config: " +
                $"Model='{c.Model}', Voices='{c.Voices}', " +
                $"Tokens='{c.Tokens}', DataDir='{c.DataDir}', " +
                $"LengthScale={c.LengthScale}");
        }

        private static void BuildZipVoice(
            ref OfflineTtsZipVoiceModelConfig c,
            TtsProfile p,
            string dir)
        {
            c.Tokens = R(dir, p.zipVoiceTokens);
            c.Encoder = R(dir, p.zipVoiceEncoder);
            c.Decoder = R(dir, p.zipVoiceDecoder);
            c.Vocoder = R(dir, p.zipVoiceVocoder);
            c.DataDir = R(dir, p.zipVoiceDataDir);
            c.Lexicon = R(dir, p.zipVoiceLexicon);
            c.FeatScale = p.zipVoiceFeatScale;
            c.Tshift = p.zipVoiceTshift;
            c.TargetRms = p.zipVoiceTargetRms;
            c.GuidanceScale = p.zipVoiceGuidanceScale;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ZipVoice config: " +
                $"Tokens='{c.Tokens}', Encoder='{c.Encoder}', " +
                $"Decoder='{c.Decoder}', Vocoder='{c.Vocoder}', " +
                $"DataDir='{c.DataDir}', Lexicon='{c.Lexicon}', " +
                $"FeatScale={c.FeatScale}, Tshift={c.Tshift}, " +
                $"TargetRms={c.TargetRms}, GuidanceScale={c.GuidanceScale}");
        }

        private static void BuildPocket(
            ref OfflineTtsPocketModelConfig c,
            TtsProfile p,
            string dir)
        {
            c.LmFlow = R(dir, p.pocketLmFlow);
            c.LmMain = R(dir, p.pocketLmMain);
            c.Encoder = R(dir, p.pocketEncoder);
            c.Decoder = R(dir, p.pocketDecoder);
            c.TextConditioner = R(dir, p.pocketTextConditioner);
            c.VocabJson = R(dir, p.pocketVocabJson);
            c.TokenScoresJson = R(dir, p.pocketTokenScoresJson);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Pocket config: " +
                $"LmFlow='{c.LmFlow}', LmMain='{c.LmMain}', " +
                $"Encoder='{c.Encoder}', Decoder='{c.Decoder}', " +
                $"TextConditioner='{c.TextConditioner}', " +
                $"VocabJson='{c.VocabJson}', " +
                $"TokenScoresJson='{c.TokenScoresJson}'");
        }

        // ── Shorthand ──

        private static string R(string modelDir, string relativePath)
        {
            return TtsModelPathResolver.Resolve(modelDir, relativePath);
        }
    }
}
#endif
