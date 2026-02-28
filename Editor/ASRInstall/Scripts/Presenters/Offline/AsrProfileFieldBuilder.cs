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
            root.Add(b.BindFile("Encoder", b.Profile.transducerEncoder, AsrProfileField.TransducerEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.transducerDecoder, AsrProfileField.TransducerDecoder, keyword: "decoder"));
            root.Add(b.BindFile("Joiner", b.Profile.transducerJoiner, AsrProfileField.TransducerJoiner, keyword: "joiner"));
        }

        internal static void BuildParaformer(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.paraformerModel, AsrProfileField.ParaformerModel));
        }

        internal static void BuildWhisper(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.whisperEncoder, AsrProfileField.WhisperEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.whisperDecoder, AsrProfileField.WhisperDecoder, keyword: "decoder"));
            root.Add(b.BindText("Language", b.Profile.whisperLanguage, AsrProfileField.WhisperLanguage));
            root.Add(b.BindText("Task", b.Profile.whisperTask, AsrProfileField.WhisperTask));
            root.Add(b.BindInt("Tail paddings", b.Profile.whisperTailPaddings, AsrProfileField.WhisperTailPaddings));
        }

        internal static void BuildSenseVoice(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.senseVoiceModel, AsrProfileField.SenseVoiceModel));
            root.Add(b.BindText("Language", b.Profile.senseVoiceLanguage, AsrProfileField.SenseVoiceLanguage));
        }

        internal static void BuildMoonshine(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Preprocessor", b.Profile.moonshinePreprocessor, AsrProfileField.MoonshinePreprocessor, keyword: "preprocess"));
            root.Add(b.BindFile("Encoder", b.Profile.moonshineEncoder, AsrProfileField.MoonshineEncoder, keyword: "encode"));
            root.Add(b.BindFile("Uncached decoder", b.Profile.moonshineUncachedDecoder, AsrProfileField.MoonshineUncachedDecoder, keyword: "uncached"));
            root.Add(b.BindFile("Cached decoder", b.Profile.moonshineCachedDecoder, AsrProfileField.MoonshineCachedDecoder, keyword: "cached"));
        }

        internal static void BuildNemoCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.nemoCtcModel, AsrProfileField.NemoCtcModel));
        }

        internal static void BuildZipformerCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.zipformerCtcModel, AsrProfileField.ZipformerCtcModel));
        }

        internal static void BuildTdnn(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.tdnnModel, AsrProfileField.TdnnModel));
        }

        internal static void BuildFireRedAsr(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.fireRedAsrEncoder, AsrProfileField.FireRedAsrEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.fireRedAsrDecoder, AsrProfileField.FireRedAsrDecoder, keyword: "decoder"));
        }

        internal static void BuildDolphin(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.dolphinModel, AsrProfileField.DolphinModel));
        }
    }
}
