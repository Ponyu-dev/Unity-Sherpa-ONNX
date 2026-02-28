using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Kws.Data;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Import
{
    /// <summary>
    /// Scans a model directory and fills <see cref="KwsProfile"/> path fields.
    /// Stores only file names — full paths are assembled at runtime
    /// from <see cref="ModelPaths.KwsModelsDir"/> + profile name + entry name.
    /// </summary>
    internal static class KwsProfileAutoFiller
    {
        internal static void Fill(KwsProfile profile, string modelDir,
            bool useInt8 = false)
        {
            profile.tokens = ModelFileScanner.FindFileByPattern(modelDir, "*tokens*.txt");
            profile.encoder = ModelFileScanner.FindEncoderOrDecoder(modelDir, "encoder", useInt8);
            profile.decoder = ModelFileScanner.FindEncoderOrDecoder(modelDir, "decoder", useInt8);
            profile.joiner = ModelFileScanner.FindEncoderOrDecoder(modelDir, "joiner", useInt8);
            profile.keywordsFile = ModelFileScanner.FindFileByPattern(modelDir, "*keywords*");
        }
    }
}
