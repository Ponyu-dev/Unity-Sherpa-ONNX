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
            root.Add(b.BindText("Encoder", b.Profile.transducerEncoder, AsrProfileField.TransducerEncoder));
            root.Add(b.BindText("Decoder", b.Profile.transducerDecoder, AsrProfileField.TransducerDecoder));
            root.Add(b.BindText("Joiner", b.Profile.transducerJoiner, AsrProfileField.TransducerJoiner));
        }

        internal static void BuildParaformer(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.paraformerModel, AsrProfileField.ParaformerModel));
        }

        internal static void BuildWhisper(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Encoder", b.Profile.whisperEncoder, AsrProfileField.WhisperEncoder));
            root.Add(b.BindText("Decoder", b.Profile.whisperDecoder, AsrProfileField.WhisperDecoder));
            root.Add(b.BindText("Language", b.Profile.whisperLanguage, AsrProfileField.WhisperLanguage));
            root.Add(b.BindText("Task", b.Profile.whisperTask, AsrProfileField.WhisperTask));
            root.Add(b.BindInt("Tail paddings", b.Profile.whisperTailPaddings, AsrProfileField.WhisperTailPaddings));
        }

        internal static void BuildSenseVoice(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.senseVoiceModel, AsrProfileField.SenseVoiceModel));
            root.Add(b.BindText("Language", b.Profile.senseVoiceLanguage, AsrProfileField.SenseVoiceLanguage));
        }

        internal static void BuildMoonshine(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Preprocessor", b.Profile.moonshinePreprocessor, AsrProfileField.MoonshinePreprocessor));
            root.Add(b.BindText("Encoder", b.Profile.moonshineEncoder, AsrProfileField.MoonshineEncoder));
            root.Add(b.BindText("Uncached decoder", b.Profile.moonshineUncachedDecoder, AsrProfileField.MoonshineUncachedDecoder));
            root.Add(b.BindText("Cached decoder", b.Profile.moonshineCachedDecoder, AsrProfileField.MoonshineCachedDecoder));
        }

        internal static void BuildNemoCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.nemoCtcModel, AsrProfileField.NemoCtcModel));
        }

        internal static void BuildZipformerCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.zipformerCtcModel, AsrProfileField.ZipformerCtcModel));
        }

        internal static void BuildTdnn(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.tdnnModel, AsrProfileField.TdnnModel));
        }

        internal static void BuildFireRedAsr(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Encoder", b.Profile.fireRedAsrEncoder, AsrProfileField.FireRedAsrEncoder));
            root.Add(b.BindText("Decoder", b.Profile.fireRedAsrDecoder, AsrProfileField.FireRedAsrDecoder));
        }

        internal static void BuildDolphin(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.dolphinModel, AsrProfileField.DolphinModel));
        }
    }
}
