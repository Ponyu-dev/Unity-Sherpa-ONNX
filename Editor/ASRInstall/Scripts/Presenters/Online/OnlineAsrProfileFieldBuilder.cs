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
            root.Add(b.BindFile("Encoder", b.Profile.transducerEncoder, OnlineAsrProfileField.TransducerEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.transducerDecoder, OnlineAsrProfileField.TransducerDecoder, keyword: "decoder"));
            root.Add(b.BindFile("Joiner", b.Profile.transducerJoiner, OnlineAsrProfileField.TransducerJoiner, keyword: "joiner"));
        }

        internal static void BuildParaformer(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.paraformerEncoder, OnlineAsrProfileField.ParaformerEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.paraformerDecoder, OnlineAsrProfileField.ParaformerDecoder, keyword: "decoder"));
        }

        internal static void BuildZipformer2Ctc(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.zipformer2CtcModel, OnlineAsrProfileField.Zipformer2CtcModel));
        }

        internal static void BuildNemoCtc(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.nemoCtcModel, OnlineAsrProfileField.NemoCtcModel));
        }

        internal static void BuildToneCtc(VisualElement root, OnlineAsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.toneCtcModel, OnlineAsrProfileField.ToneCtcModel));
        }
    }
}
