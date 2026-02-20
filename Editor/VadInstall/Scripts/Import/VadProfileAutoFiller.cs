using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Vad.Data;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Import
{
    /// <summary>
    /// Scans a model directory and fills <see cref="VadProfile"/> path fields.
    /// Stores only file names — full paths are assembled at runtime
    /// from <see cref="VadModelPaths.VadModelsRelative"/> + profile name + entry name.
    /// </summary>
    internal static class VadProfileAutoFiller
    {
        internal static void Fill(VadProfile profile, string modelDir)
        {
            profile.model = ModelFileScanner.FindOnnxModel(modelDir);
        }
    }
}
