using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    /// <summary>
    /// Continuation of <see cref="AsrProfileFieldBuilder"/>.
    /// Canary, WenetCtc, Omnilingual, MedAsr, FunAsrNano.
    /// </summary>
    internal static partial class AsrProfileFieldBuilder
    {
        internal static void BuildCanary(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder", b.Profile.canaryEncoder, AsrProfileField.CanaryEncoder, keyword: "encoder", isRequired: true));
            root.Add(b.BindFile("Decoder", b.Profile.canaryDecoder, AsrProfileField.CanaryDecoder, keyword: "decoder", isRequired: true));
            root.Add(b.BindText("Source language", b.Profile.canarySrcLang, AsrProfileField.CanarySrcLang));
            root.Add(b.BindText("Target language", b.Profile.canaryTgtLang, AsrProfileField.CanaryTgtLang));
        }

        internal static void BuildWenetCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.wenetCtcModel, AsrProfileField.WenetCtcModel, isRequired: true));
        }

        internal static void BuildOmnilingual(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.omnilingualModel, AsrProfileField.OmnilingualModel, isRequired: true));
        }

        internal static void BuildMedAsr(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.medAsrModel, AsrProfileField.MedAsrModel, isRequired: true));
        }

        internal static void BuildFunAsrNano(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder adaptor", b.Profile.funAsrNanoEncoderAdaptor, AsrProfileField.FunAsrNanoEncoderAdaptor, keyword: "encoder", isRequired: true));
            root.Add(b.BindFile("LLM", b.Profile.funAsrNanoLlm, AsrProfileField.FunAsrNanoLlm, keyword: "llm", isRequired: true));
            root.Add(b.BindFile("Embedding", b.Profile.funAsrNanoEmbedding, AsrProfileField.FunAsrNanoEmbedding, keyword: "embedding", isRequired: true));
            root.Add(b.BindFile("Tokenizer", b.Profile.funAsrNanoTokenizer, AsrProfileField.FunAsrNanoTokenizer, "json", "tokenizer", isRequired: true));
            root.Add(b.BindText("System prompt", b.Profile.funAsrNanoSystemPrompt, AsrProfileField.FunAsrNanoSystemPrompt));
            root.Add(b.BindText("User prompt", b.Profile.funAsrNanoUserPrompt, AsrProfileField.FunAsrNanoUserPrompt));
            root.Add(b.BindInt("Max new tokens", b.Profile.funAsrNanoMaxNewTokens, AsrProfileField.FunAsrNanoMaxNewTokens));
            root.Add(b.BindFloat("Temperature", b.Profile.funAsrNanoTemperature, AsrProfileField.FunAsrNanoTemperature));
            root.Add(b.BindFloat("Top P", b.Profile.funAsrNanoTopP, AsrProfileField.FunAsrNanoTopP));
            root.Add(b.BindInt("Seed", b.Profile.funAsrNanoSeed, AsrProfileField.FunAsrNanoSeed));
            root.Add(b.BindText("Language", b.Profile.funAsrNanoLanguage, AsrProfileField.FunAsrNanoLanguage));
            root.Add(b.BindText("Hotwords", b.Profile.funAsrNanoHotwords, AsrProfileField.FunAsrNanoHotwords));
        }

        internal static void BuildQwen3Asr(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Conv frontend", b.Profile.qwen3ConvFrontend, AsrProfileField.Qwen3ConvFrontend, keyword: "conv_frontend", isRequired: true));
            root.Add(b.BindFile("Encoder", b.Profile.qwen3Encoder, AsrProfileField.Qwen3Encoder, keyword: "encoder", isRequired: true));
            root.Add(b.BindFile("Decoder", b.Profile.qwen3Decoder, AsrProfileField.Qwen3Decoder, keyword: "decoder", isRequired: true));
            root.Add(b.BindFolder("Tokenizer dir", b.Profile.qwen3Tokenizer, AsrProfileField.Qwen3Tokenizer, keyword: "tokenizer", isRequired: true));
            root.Add(b.BindInt("Max total len", b.Profile.qwen3MaxTotalLen, AsrProfileField.Qwen3MaxTotalLen));
            root.Add(b.BindInt("Max new tokens", b.Profile.qwen3MaxNewTokens, AsrProfileField.Qwen3MaxNewTokens));
            root.Add(b.BindFloat("Temperature", b.Profile.qwen3Temperature, AsrProfileField.Qwen3Temperature));
            root.Add(b.BindFloat("Top P", b.Profile.qwen3TopP, AsrProfileField.Qwen3TopP));
            root.Add(b.BindInt("Seed", b.Profile.qwen3Seed, AsrProfileField.Qwen3Seed));
            root.Add(b.BindText("Hotwords", b.Profile.qwen3Hotwords, AsrProfileField.Qwen3Hotwords));
        }
    }
}
