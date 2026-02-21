using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Scans a model directory and fills <see cref="OnlineAsrProfile"/>
    /// path fields. Stores only file names.
    /// </summary>
    internal static class OnlineAsrProfileAutoFiller
    {
        internal static void Fill(
            OnlineAsrProfile profile, string modelDir,
            bool useInt8 = false)
        {
            FillCommonFields(profile, modelDir);
            FillByModelType(profile, modelDir, useInt8);
        }

        private static void FillCommonFields(
            OnlineAsrProfile profile, string dir)
        {
            profile.tokens = ModelFileScanner.FindFileByPattern(
                dir, "*tokens*.txt");

            string fsts = ModelFileScanner.JoinFileNames(dir, "*.fst");
            if (!string.IsNullOrEmpty(fsts)) profile.ruleFsts = fsts;

            string fars = ModelFileScanner.JoinFileNames(dir, "*.far");
            if (!string.IsNullOrEmpty(fars)) profile.ruleFars = fars;
        }

        private static void FillByModelType(
            OnlineAsrProfile profile, string dir, bool useInt8)
        {
            switch (profile.modelType)
            {
                case OnlineAsrModelType.Transducer:
                    FillTransducer(profile, dir, useInt8);
                    break;
                case OnlineAsrModelType.Paraformer:
                    FillParaformer(profile, dir, useInt8);
                    break;
                case OnlineAsrModelType.Zipformer2Ctc:
                    profile.zipformer2CtcModel = useInt8
                        ? ModelFileScanner.FindOnnxModelInt8(dir)
                        : ModelFileScanner.FindOnnxModel(dir);
                    break;
                case OnlineAsrModelType.NemoCtc:
                    profile.nemoCtcModel = useInt8
                        ? ModelFileScanner.FindOnnxModelInt8(dir)
                        : ModelFileScanner.FindOnnxModel(dir);
                    break;
                case OnlineAsrModelType.ToneCtc:
                    profile.toneCtcModel = useInt8
                        ? ModelFileScanner.FindOnnxModelInt8(dir)
                        : ModelFileScanner.FindOnnxModel(dir);
                    break;
            }
        }

        private static void FillTransducer(
            OnlineAsrProfile p, string dir, bool useInt8)
        {
            p.transducerEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.transducerDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder", useInt8);
            p.transducerJoiner = ModelFileScanner.FindOnnxContaining(dir, "joiner", useInt8);
        }

        private static void FillParaformer(
            OnlineAsrProfile p, string dir, bool useInt8)
        {
            p.paraformerEncoder = ModelFileScanner.FindOnnxContaining(dir, "encoder", useInt8);
            p.paraformerDecoder = ModelFileScanner.FindOnnxContaining(dir, "decoder", useInt8);
        }
    }
}
