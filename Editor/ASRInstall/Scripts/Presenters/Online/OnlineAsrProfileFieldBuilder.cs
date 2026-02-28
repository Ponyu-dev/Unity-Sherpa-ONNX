using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    /// <summary>
    /// Builds model-specific UI fields for OnlineAsrProfile detail panel.
    /// </summary>
    internal static class OnlineAsrProfileFieldBuilder
    {
        internal static void BuildTransducer(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Encoder", b.Profile.transducerEncoder, OnlineAsrProfileField.TransducerEncoder));
            root.Add(b.BindText("Decoder", b.Profile.transducerDecoder, OnlineAsrProfileField.TransducerDecoder));
            root.Add(b.BindText("Joiner", b.Profile.transducerJoiner, OnlineAsrProfileField.TransducerJoiner));
        }

        internal static void BuildParaformer(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Encoder", b.Profile.paraformerEncoder, OnlineAsrProfileField.ParaformerEncoder));
            root.Add(b.BindText("Decoder", b.Profile.paraformerDecoder, OnlineAsrProfileField.ParaformerDecoder));
        }

        internal static void BuildZipformer2Ctc(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.zipformer2CtcModel, OnlineAsrProfileField.Zipformer2CtcModel));
        }

        internal static void BuildNemoCtc(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.nemoCtcModel, OnlineAsrProfileField.NemoCtcModel));
        }

        internal static void BuildToneCtc(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.toneCtcModel, OnlineAsrProfileField.ToneCtcModel));
        }
    }
}
