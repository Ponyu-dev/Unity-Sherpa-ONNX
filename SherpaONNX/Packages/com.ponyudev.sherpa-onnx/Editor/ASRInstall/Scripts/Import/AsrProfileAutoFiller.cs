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
        internal static void Fill(
            AsrProfile profile, string modelDir, bool useInt8 = false)
        {
            FillCommonFields(profile, modelDir);
            FillByModelType(profile, modelDir, useInt8);
        }

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

        private static void FillByModelType(
            AsrProfile profile, string dir, bool useInt8)
        {
            switch (profile.modelType)
            {
                case AsrModelType.Transducer: FillTransducer(profile, dir, useInt8); break;
                case AsrModelType.Paraformer: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.Whisper: FillWhisper(profile, dir, useInt8); break;
                case AsrModelType.SenseVoice: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.Moonshine: FillMoonshine(profile, dir, useInt8); break;
                case AsrModelType.NemoCtc: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.ZipformerCtc: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.Tdnn: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.Dolphin: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.WenetCtc: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.Omnilingual: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.MedAsr: FillSingleModel(profile, dir, useInt8); break;
                case AsrModelType.FireRedAsr: FillFireRedAsr(profile, dir, useInt8); break;
                case AsrModelType.Canary: FillCanary(profile, dir, useInt8); break;
                case AsrModelType.FunAsrNano: FillFunAsrNano(profile, dir, useInt8); break;
            }
        }

        private static void FillTransducer(
            AsrProfile p, string dir, bool useInt8)
        {
            p.transducerEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.transducerDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder", useInt8);
            p.transducerJoiner = ModelFileScanner.FindOnnxContaining(dir, "joiner", useInt8);
        }

        private static void FillWhisper(
            AsrProfile p, string dir, bool useInt8)
        {
            p.whisperEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.whisperDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder", useInt8);
        }

        private static void FillMoonshine(
            AsrProfile p, string dir, bool useInt8)
        {
            p.moonshinePreprocessor = ModelFileScanner.FindOnnxContaining(dir, "preprocess", useInt8);
            p.moonshineEncoder = ModelFileScanner.FindOnnxContaining(dir, "encode", useInt8);
            p.moonshineUncachedDecoder = ModelFileScanner.FindOnnxContaining(dir, "uncached", useInt8);
            p.moonshineCachedDecoder = ModelFileScanner.FindOnnxContaining(dir, "cached", useInt8);
        }

        private static void FillFireRedAsr(
            AsrProfile p, string dir, bool useInt8)
        {
            p.fireRedAsrEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.fireRedAsrDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder", useInt8);
        }

        private static void FillCanary(
            AsrProfile p, string dir, bool useInt8)
        {
            p.canaryEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.canaryDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder", useInt8);
        }

        private static void FillFunAsrNano(
            AsrProfile p, string dir, bool useInt8)
        {
            p.funAsrNanoEncoderAdaptor = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.funAsrNanoLlm = ModelFileScanner.FindOnnxContaining(dir, "llm", useInt8);
            p.funAsrNanoEmbedding = ModelFileScanner.FindOnnxContaining(dir, "embedding", useInt8);
            p.funAsrNanoTokenizer = ModelFileScanner.FindFileIfExists(dir, "tokenizer.json");
        }

        private static void FillSingleModel(
            AsrProfile p, string dir, bool useInt8)
        {
            string model = useInt8
                ? ModelFileScanner.FindOnnxModelInt8(dir)
                : ModelFileScanner.FindOnnxModel(dir);

            switch (p.modelType)
            {
                case AsrModelType.Paraformer: p.paraformerModel = model; break;
                case AsrModelType.SenseVoice: p.senseVoiceModel = model; break;
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
