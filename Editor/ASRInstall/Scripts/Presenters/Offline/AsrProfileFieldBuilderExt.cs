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
            root.Add(b.BindFile("Encoder", b.Profile.canaryEncoder, AsrProfileField.CanaryEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.canaryDecoder, AsrProfileField.CanaryDecoder, keyword: "decoder"));
            root.Add(b.BindText("Source language", b.Profile.canarySrcLang, AsrProfileField.CanarySrcLang));
            root.Add(b.BindText("Target language", b.Profile.canaryTgtLang, AsrProfileField.CanaryTgtLang));
        }

        internal static void BuildWenetCtc(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.wenetCtcModel, AsrProfileField.WenetCtcModel));
        }

        internal static void BuildOmnilingual(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.omnilingualModel, AsrProfileField.OmnilingualModel));
        }

        internal static void BuildMedAsr(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.medAsrModel, AsrProfileField.MedAsrModel));
        }

        internal static void BuildFunAsrNano(
            VisualElement root, AsrProfileFieldBinder b)
        {
            root.Add(b.BindFile("Encoder adaptor", b.Profile.funAsrNanoEncoderAdaptor, AsrProfileField.FunAsrNanoEncoderAdaptor, keyword: "encoder"));
            root.Add(b.BindFile("LLM", b.Profile.funAsrNanoLlm, AsrProfileField.FunAsrNanoLlm, keyword: "llm"));
            root.Add(b.BindFile("Embedding", b.Profile.funAsrNanoEmbedding, AsrProfileField.FunAsrNanoEmbedding, keyword: "embedding"));
            root.Add(b.BindFile("Tokenizer", b.Profile.funAsrNanoTokenizer, AsrProfileField.FunAsrNanoTokenizer, "json", "tokenizer"));
            root.Add(b.BindText("System prompt", b.Profile.funAsrNanoSystemPrompt, AsrProfileField.FunAsrNanoSystemPrompt));
            root.Add(b.BindText("User prompt", b.Profile.funAsrNanoUserPrompt, AsrProfileField.FunAsrNanoUserPrompt));
            root.Add(b.BindInt("Max new tokens", b.Profile.funAsrNanoMaxNewTokens, AsrProfileField.FunAsrNanoMaxNewTokens));
            root.Add(b.BindFloat("Temperature", b.Profile.funAsrNanoTemperature, AsrProfileField.FunAsrNanoTemperature));
            root.Add(b.BindFloat("Top P", b.Profile.funAsrNanoTopP, AsrProfileField.FunAsrNanoTopP));
            root.Add(b.BindInt("Seed", b.Profile.funAsrNanoSeed, AsrProfileField.FunAsrNanoSeed));
            root.Add(b.BindText("Language", b.Profile.funAsrNanoLanguage, AsrProfileField.FunAsrNanoLanguage));
            root.Add(b.BindText("Hotwords", b.Profile.funAsrNanoHotwords, AsrProfileField.FunAsrNanoHotwords));
        }
    }
}
