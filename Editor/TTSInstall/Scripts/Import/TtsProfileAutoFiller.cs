using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Scans a model directory and fills <see cref="TtsProfile"/> path fields.
    /// Stores only file names / folder names — full paths are assembled at runtime
    /// from <see cref="TtsModelPaths.TtsModelsRelative"/> + profile name + entry name.
    /// </summary>
    internal static class TtsProfileAutoFiller
    {
        internal static void Fill(TtsProfile profile, string modelDir,
            bool useInt8 = false)
        {
            FillCommonFields(profile, modelDir);
            FillByModelType(profile, modelDir, useInt8);
        }

        // ── Common fields (shared across model types) ──

        private static void FillCommonFields(TtsProfile profile, string dir)
        {
            FillRuleFsts(profile, dir);
            FillRuleFars(profile, dir);
        }

        // ── Per-model-type fields ──

        private static void FillByModelType(TtsProfile profile, string dir,
            bool useInt8)
        {
            switch (profile.modelType)
            {
                case TtsModelType.Vits:
                    FillVits(profile, dir);
                    break;
                case TtsModelType.Matcha:
                    FillMatcha(profile, dir);
                    break;
                case TtsModelType.Kokoro:
                    FillKokoro(profile, dir);
                    break;
                case TtsModelType.Kitten:
                    FillKitten(profile, dir);
                    break;
                case TtsModelType.ZipVoice:
                    FillZipVoice(profile, dir, useInt8);
                    break;
                case TtsModelType.Pocket:
                    FillPocket(profile, dir, useInt8);
                    break;
            }
        }

        private static void FillVits(TtsProfile profile, string dir)
        {
            profile.vitsModel = ModelFileScanner.FindOnnxModel(dir);
            profile.vitsTokens = ModelFileScanner.FindFileByPattern(dir, "*tokens*.txt");
            profile.vitsLexicon = ModelFileScanner.FindAllLexicons(dir, profile.vitsModel);
            profile.vitsDataDir = ModelFileScanner.FindSubDir(dir, "espeak-ng-data");
            profile.vitsDictDir = ModelFileScanner.FindSubDir(dir, "dict");
        }

        private static void FillMatcha(TtsProfile profile, string dir)
        {
            profile.matchaAcousticModel = ModelFileScanner.FindOnnxModel(dir);
            profile.matchaTokens = ModelFileScanner.FindFileByPattern(dir, "*tokens*.txt");
            profile.matchaDataDir = ModelFileScanner.FindSubDir(dir, "espeak-ng-data");
            profile.matchaDictDir = ModelFileScanner.FindSubDir(dir, "dict");
            profile.matchaNoiseScale = 0.667f;
            profile.matchaLengthScale = 1.0f;
        }

        private static void FillKitten(TtsProfile profile, string dir)
        {
            profile.kittenModel = ModelFileScanner.FindOnnxModel(dir);
            profile.kittenVoices = ModelFileScanner.FindFileByPattern(dir, "*voices*.bin");
            profile.kittenTokens = ModelFileScanner.FindFileByPattern(dir, "*tokens*.txt");
            profile.kittenDataDir = ModelFileScanner.FindSubDir(dir, "espeak-ng-data");
            profile.kittenLengthScale = 1.0f;
        }

        private static void FillKokoro(TtsProfile profile, string dir)
        {
            profile.kokoroModel = ModelFileScanner.FindOnnxModelWithInt8Fallback(dir);
            profile.kokoroVoices = ModelFileScanner.FindFileByPattern(dir, "*voices*.bin");
            profile.kokoroTokens = ModelFileScanner.FindFileByPattern(dir, "*tokens*.txt");
            profile.kokoroDataDir = ModelFileScanner.FindSubDir(dir, "espeak-ng-data");
            profile.kokoroDictDir = ModelFileScanner.FindSubDir(dir, "dict");
            profile.kokoroLexicon = ModelFileScanner.FindAllLexicons(dir, profile.kokoroModel);
            profile.kokoroLengthScale = 1.0f;
        }

        private static void FillZipVoice(TtsProfile profile, string dir,
            bool useInt8)
        {
            profile.zipVoiceTokens = ModelFileScanner.FindFileByPattern(dir, "*tokens*.txt");
            profile.zipVoiceEncoder = ModelFileScanner.FindEncoderOrDecoder(dir, "encoder", useInt8);
            profile.zipVoiceDecoder = ModelFileScanner.FindEncoderOrDecoder(dir, "decoder", useInt8);
            profile.zipVoiceVocoder = ModelFileScanner.FindOnnxContaining(dir, "vocos");
            profile.zipVoiceDataDir = ModelFileScanner.FindSubDir(dir, "espeak-ng-data");
            profile.zipVoiceLexicon = ModelFileScanner.FindAllLexicons(dir, profile.zipVoiceEncoder);
            profile.zipVoiceFeatScale = 0.1f;
            profile.zipVoiceTshift = 0.5f;
            profile.zipVoiceTargetRms = 0.1f;
            profile.zipVoiceGuidanceScale = 1.0f;
        }

        private static void FillPocket(TtsProfile profile, string dir,
            bool useInt8)
        {
            profile.pocketLmFlow = ModelFileScanner.FindEncoderOrDecoder(dir, "lm_flow", useInt8);
            profile.pocketLmMain = ModelFileScanner.FindEncoderOrDecoder(dir, "lm_main", useInt8);
            profile.pocketEncoder = ModelFileScanner.FindEncoderOrDecoder(dir, "encoder", useInt8);
            profile.pocketDecoder = ModelFileScanner.FindEncoderOrDecoder(dir, "decoder", useInt8);
            profile.pocketTextConditioner = ModelFileScanner.FindFileByPattern(dir, "*text_conditioner*.onnx");
            profile.pocketVocabJson = ModelFileScanner.FindFileByPattern(dir, "*vocab*.json");
            profile.pocketTokenScoresJson = ModelFileScanner.FindFileByPattern(dir, "*token_scores*.json");
        }

        // ── Rule files ──

        private static void FillRuleFsts(TtsProfile profile, string dir)
        {
            string joined = ModelFileScanner.JoinFileNames(dir, "*.fst");
            if (!string.IsNullOrEmpty(joined))
                profile.ruleFsts = joined;
        }

        private static void FillRuleFars(TtsProfile profile, string dir)
        {
            string joined = ModelFileScanner.JoinFileNames(dir, "*.far");
            if (!string.IsNullOrEmpty(joined))
                profile.ruleFars = joined;
        }
    }
}
