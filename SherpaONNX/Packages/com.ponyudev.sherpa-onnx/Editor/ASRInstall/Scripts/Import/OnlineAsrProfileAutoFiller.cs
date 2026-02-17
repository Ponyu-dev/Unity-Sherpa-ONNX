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
            OnlineAsrProfile profile, string modelDir)
        {
            FillCommonFields(profile, modelDir);
            FillByModelType(profile, modelDir);
        }

        // ── Common ──

        private static void FillCommonFields(
            OnlineAsrProfile profile, string dir)
        {
            profile.tokens = ModelFileScanner.FindFileIfExists(
                dir, "tokens.txt");

            string fsts = ModelFileScanner.JoinFileNames(dir, "*.fst");
            if (!string.IsNullOrEmpty(fsts)) profile.ruleFsts = fsts;

            string fars = ModelFileScanner.JoinFileNames(dir, "*.far");
            if (!string.IsNullOrEmpty(fars)) profile.ruleFars = fars;
        }

        // ── Per-model-type ──

        private static void FillByModelType(
            OnlineAsrProfile profile, string dir)
        {
            switch (profile.modelType)
            {
                case OnlineAsrModelType.Transducer:
                    FillTransducer(profile, dir);
                    break;
                case OnlineAsrModelType.Paraformer:
                    FillParaformer(profile, dir);
                    break;
                case OnlineAsrModelType.Zipformer2Ctc:
                    profile.zipformer2CtcModel =
                        ModelFileScanner.FindOnnxModel(dir);
                    break;
                case OnlineAsrModelType.NemoCtc:
                    profile.nemoCtcModel =
                        ModelFileScanner.FindOnnxModel(dir);
                    break;
                case OnlineAsrModelType.ToneCtc:
                    profile.toneCtcModel =
                        ModelFileScanner.FindOnnxModel(dir);
                    break;
            }
        }

        private static void FillTransducer(
            OnlineAsrProfile p, string dir)
        {
            p.transducerEncoder =
                ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.transducerDecoder =
                ModelFileScanner.FindOnnxContaining(dir, "decoder");
            p.transducerJoiner =
                ModelFileScanner.FindOnnxContaining(dir, "joiner");
        }

        private static void FillParaformer(
            OnlineAsrProfile p, string dir)
        {
            p.paraformerEncoder =
                ModelFileScanner.FindOnnxContaining(dir, "encoder");
            p.paraformerDecoder =
                ModelFileScanner.FindOnnxContaining(dir, "decoder");
        }
    }
}
