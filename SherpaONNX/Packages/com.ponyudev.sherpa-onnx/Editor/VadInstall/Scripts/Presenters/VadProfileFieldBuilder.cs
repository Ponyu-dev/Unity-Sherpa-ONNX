using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    /// <summary>
    /// Builds model-specific UI fields for VadProfile detail panel.
    /// Both SileroVad and TenVad share the same model file field,
    /// so a single builder method is sufficient.
    /// </summary>
    internal static class VadProfileFieldBuilder
    {
        internal static void BuildModelFields(
            VisualElement root, VadProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.model,
                VadProfileField.Model));
        }
    }
}
