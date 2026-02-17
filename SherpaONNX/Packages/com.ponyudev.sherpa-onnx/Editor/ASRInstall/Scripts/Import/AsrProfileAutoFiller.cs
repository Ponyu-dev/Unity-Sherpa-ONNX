using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Scans a model directory and fills <see cref="AsrProfile"/>
    /// path fields. Stores only file names — full paths are
    /// assembled at runtime by <see cref="Asr.Config.AsrModelPathResolver"/>.
    /// </summary>
    internal static class AsrProfileAutoFiller
    {
        internal static void Fill(AsrProfile profile, string modelDir)
        {
            FillCommonFields(profile, modelDir);
            FillByModelType(profile, modelDir);
        }

        // ── Common ──

        private static void FillCommonFields(
            AsrProfile profile, string dir)
        {
            profile.tokens = ModelFileScanner.FindFileIfExists(
                dir, "tokens.txt");

            string fsts = ModelFileScanner.JoinFileNames(dir, "*.fst");
            if (!string.IsNullOrEmpty(fsts)) profile.ruleFsts = fsts;

            string fars = ModelFileScanner.JoinFileNames(dir, "*.far");
            if (!string.IsNullOrEmpty(fars)) profile.ruleFars = fars;

            string lm = ModelFileScanner.FindFileIfExists(
                dir, "lm.onnx");
            if (!string.IsNullOrEmpty(lm)) profile.lmModel = lm;
        }

        // ── Per-model-type ──

        private static void FillByModelType(
            AsrProfile profile, string dir)
        {
            switch (profile.modelType)
            {
                case AsrModelType.Transducer: FillTransducer(profile, dir); break;
                case AsrModelType.Paraformer: FillParaformer(profile, dir); break;
                case AsrModelType.Whisper: FillWhisper(profile, dir); break;
                case AsrModelType.SenseVoice: FillSenseVoice(profile, dir); break;
                case AsrModelType.Moonshine: FillMoonshine(profile, dir); break;
                case AsrModelType.NemoCtc: FillSingleModel(profile, dir); break;
                case AsrModelType.ZipformerCtc: FillSingleModel(profile, dir); break;
                case AsrModelType.Tdnn: FillSingleModel(profile, dir); break;
                case AsrModelType.Dolphin: FillSingleModel(profile, dir); break;
                case AsrModelType.WenetCtc: FillSingleModel(profile, dir); break;
                case AsrModelType.Omnilingual: FillSingleModel(profile, dir); break;
                case AsrModelType.MedAsr: FillSingleModel(profile, dir); break;
                case AsrModelType.FireRedAsr: FillFireRedAsr(profile, dir); break;
                case AsrModelType.Canary: FillCanary(profile, dir); break;
                case AsrModelType.FunAsrNano: FillFunAsrNano(profile, dir); break;
            }
        }

        private static void FillTransducer(AsrProfile p, string dir)
        {
            p.transducerEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.transducerDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder");
            p.transducerJoiner = ModelFileScanner.FindOnnxContaining(dir, "joiner");
        }

        private static void FillParaformer(AsrProfile p, string dir)
        {
            p.paraformerModel = ModelFileScanner.FindOnnxModel(dir);
        }

        private static void FillWhisper(AsrProfile p, string dir)
        {
            p.whisperEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.whisperDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder");
        }

        private static void FillSenseVoice(AsrProfile p, string dir)
        {
            p.senseVoiceModel = ModelFileScanner.FindOnnxModel(dir);
        }

        private static void FillMoonshine(AsrProfile p, string dir)
        {
            p.moonshinePreprocessor = ModelFileScanner.FindOnnxContaining(dir, "preprocess");
            p.moonshineEncoder = ModelFileScanner.FindOnnxContaining(dir, "encode");
            p.moonshineUncachedDecoder = ModelFileScanner.FindOnnxContaining(dir, "uncached");
            p.moonshineCachedDecoder = ModelFileScanner.FindOnnxContaining(dir, "cached");
        }

        private static void FillFireRedAsr(AsrProfile p, string dir)
        {
            p.fireRedAsrEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.fireRedAsrDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder");
        }

        private static void FillCanary(AsrProfile p, string dir)
        {
            p.canaryEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.canaryDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder");
        }

        private static void FillFunAsrNano(AsrProfile p, string dir)
        {
            p.funAsrNanoEncoderAdaptor = ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.funAsrNanoLlm = ModelFileScanner.FindOnnxContaining(dir, "llm");
            p.funAsrNanoEmbedding = ModelFileScanner.FindOnnxContaining(dir, "embedding");
            p.funAsrNanoTokenizer = ModelFileScanner.FindFileIfExists(dir, "tokenizer.json");
        }

        /// <summary>
        /// For single-model types: find the primary .onnx and assign
        /// to the appropriate field based on model type.
        /// </summary>
        private static void FillSingleModel(AsrProfile p, string dir)
        {
            string model = ModelFileScanner.FindOnnxModel(dir);

            switch (p.modelType)
            {
                case AsrModelType.NemoCtc: p.nemoCtcModel = model; break;
                case AsrModelType.ZipformerCtc: p.zipformerCtcModel = model; break;
                case AsrModelType.Tdnn: p.tdnnModel = model; break;
                case AsrModelType.Dolphin: p.dolphinModel = model; break;
                case AsrModelType.WenetCtc: p.wenetCtcModel = model; break;
                case AsrModelType.Omnilingual: p.omnilingualModel = model; break;
                case AsrModelType.MedAsr: p.medAsrModel = model; break;
            }
        }
    }
}
