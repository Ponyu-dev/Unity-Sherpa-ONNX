using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    /// <summary>
    /// Builds model-specific UI fields for AsrProfile detail panel.
    /// Partial class — continued in AsrProfileFieldBuilderExt.cs.
    /// </summary>
    internal static partial class AsrProfileFieldBuilder
    {
        internal static void BuildTransducer(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.transducerEncoder, AsrProfileField.TransducerEncoder, keyword: "encoder", isRequired: true));
            root.Add(b.BindFile("Decoder", b.Profile.transducerDecoder, AsrProfileField.TransducerDecoder, keyword: "decoder", isRequired: true));
            root.Add(b.BindFile("Joiner", b.Profile.transducerJoiner, AsrProfileField.TransducerJoiner, keyword: "joiner", isRequired: true));
        }

        internal static void BuildParaformer(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.paraformerModel, AsrProfileField.ParaformerModel, isRequired: true));
        }

        internal static void BuildWhisper(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.whisperEncoder, AsrProfileField.WhisperEncoder, keyword: "encoder", isRequired: true));
            root.Add(b.BindFile("Decoder", b.Profile.whisperDecoder, AsrProfileField.WhisperDecoder, keyword: "decoder", isRequired: true));
            root.Add(b.BindText("Language", b.Profile.whisperLanguage, AsrProfileField.WhisperLanguage));
            root.Add(b.BindText("Task", b.Profile.whisperTask, AsrProfileField.WhisperTask));
            root.Add(b.BindInt("Tail paddings", b.Profile.whisperTailPaddings, AsrProfileField.WhisperTailPaddings));
        }

        internal static void BuildSenseVoice(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.senseVoiceModel, AsrProfileField.SenseVoiceModel, isRequired: true));
            root.Add(b.BindText("Language", b.Profile.senseVoiceLanguage, AsrProfileField.SenseVoiceLanguage));
        }

        internal static void BuildMoonshine(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Preprocessor", b.Profile.moonshinePreprocessor, AsrProfileField.MoonshinePreprocessor, keyword: "preprocess", isRequired: true));
            root.Add(b.BindFile("Encoder", b.Profile.moonshineEncoder, AsrProfileField.MoonshineEncoder, keyword: "encode", isRequired: true));
            root.Add(b.BindFile("Uncached decoder", b.Profile.moonshineUncachedDecoder, AsrProfileField.MoonshineUncachedDecoder, keyword: "uncached", isRequired: true));
            root.Add(b.BindFile("Cached decoder", b.Profile.moonshineCachedDecoder, AsrProfileField.MoonshineCachedDecoder, keyword: "cached", isRequired: true));
        }

        internal static void BuildNemoCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.nemoCtcModel, AsrProfileField.NemoCtcModel, isRequired: true));
        }

        internal static void BuildZipformerCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.zipformerCtcModel, AsrProfileField.ZipformerCtcModel, isRequired: true));
        }

        internal static void BuildTdnn(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.tdnnModel, AsrProfileField.TdnnModel, isRequired: true));
        }

        internal static void BuildFireRedAsr(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.fireRedAsrEncoder, AsrProfileField.FireRedAsrEncoder, keyword: "encoder", isRequired: true));
            root.Add(b.BindFile("Decoder", b.Profile.fireRedAsrDecoder, AsrProfileField.FireRedAsrDecoder, keyword: "decoder", isRequired: true));
        }

        internal static void BuildDolphin(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.dolphinModel, AsrProfileField.DolphinModel, isRequired: true));
        }
    }
}
