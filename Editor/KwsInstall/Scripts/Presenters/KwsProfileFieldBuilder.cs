using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters
{
    /// <summary>
    /// Builds model-specific UI fields for KwsProfile detail panel.
    /// Currently only Transducer type exists — builds encoder/decoder/joiner fields.
    /// </summary>
    internal static class KwsProfileFieldBuilder
    {
        internal static void BuildTransducerFields(VisualElement root, KwsProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.encoder, KwsProfileField.Encoder, isRequired: true));
            root.Add(b.BindFile("Decoder", b.Profile.decoder, KwsProfileField.Decoder, isRequired: true));
            root.Add(b.BindFile("Joiner", b.Profile.joiner, KwsProfileField.Joiner, isRequired: true));
        }
    }
}
