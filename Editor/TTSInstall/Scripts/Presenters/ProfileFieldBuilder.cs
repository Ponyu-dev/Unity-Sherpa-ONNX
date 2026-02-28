using System;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Builds model-specific UI fields for TtsProfile detail panel.
    /// Uses <see cref="ProfileFieldBinder"/> to bind fields without lambdas.
    /// </summary>
    internal static class ProfileFieldBuilder
    {
        internal static void BuildVits(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.vitsModel, ProfileField.VitsModel));
            root.Add(b.BindFile("Tokens", b.Profile.vitsTokens, ProfileField.VitsTokens, "txt", "tokens"));
            root.Add(b.BindFile("Lexicon", b.Profile.vitsLexicon, ProfileField.VitsLexicon, "txt", "lexicon"));
            root.Add(b.BindFolder("Data dir", b.Profile.vitsDataDir, ProfileField.VitsDataDir, "espeak-ng-data"));
            root.Add(b.BindFolder("Dict dir", b.Profile.vitsDictDir, ProfileField.VitsDictDir, "dict"));
            root.Add(b.BindFloat("Noise scale", b.Profile.vitsNoiseScale, ProfileField.VitsNoiseScale));
            root.Add(b.BindFloat("Noise scale W", b.Profile.vitsNoiseScaleW, ProfileField.VitsNoiseScaleW));
            root.Add(b.BindFloat("Length scale", b.Profile.vitsLengthScale, ProfileField.VitsLengthScale));
        }

        internal static void BuildMatcha(
            VisualElement root, ProfileFieldBinder b,
            TtsProjectSettings settings, Action onRefresh)
        {
            root.Add(b.BindFile("Acoustic model", b.Profile.matchaAcousticModel, ProfileField.MatchaAcousticModel, keyword: "acoustic"));
            root.Add(b.BindFile("Vocoder", b.Profile.matchaVocoder, ProfileField.MatchaVocoder, keyword: "vocoder"));

            var vocoderField = new MatchaVocoderProfileField(
                b.Profile, settings, onRefresh);
            root.Add(vocoderField.Build());

            root.Add(b.BindFile("Tokens", b.Profile.matchaTokens, ProfileField.MatchaTokens, "txt", "tokens"));
            root.Add(b.BindFile("Lexicon", b.Profile.matchaLexicon, ProfileField.MatchaLexicon, "txt", "lexicon"));
            root.Add(b.BindFolder("Data dir", b.Profile.matchaDataDir, ProfileField.MatchaDataDir, "espeak-ng-data"));
            root.Add(b.BindFolder("Dict dir", b.Profile.matchaDictDir, ProfileField.MatchaDictDir, "dict"));
            root.Add(b.BindFloat("Noise scale", b.Profile.matchaNoiseScale, ProfileField.MatchaNoiseScale));
            root.Add(b.BindFloat("Length scale", b.Profile.matchaLengthScale, ProfileField.MatchaLengthScale));
        }

        internal static void BuildKokoro(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.kokoroModel, ProfileField.KokoroModel));
            root.Add(b.BindFile("Voices", b.Profile.kokoroVoices, ProfileField.KokoroVoices, "bin", "voices"));
            root.Add(b.BindFile("Tokens", b.Profile.kokoroTokens, ProfileField.KokoroTokens, "txt", "tokens"));
            root.Add(b.BindFolder("Data dir", b.Profile.kokoroDataDir, ProfileField.KokoroDataDir, "espeak-ng-data"));
            root.Add(b.BindFolder("Dict dir", b.Profile.kokoroDictDir, ProfileField.KokoroDictDir, "dict"));
            root.Add(b.BindFile("Lexicon", b.Profile.kokoroLexicon, ProfileField.KokoroLexicon, "txt", "lexicon"));
            root.Add(b.BindText("Language", b.Profile.kokoroLang, ProfileField.KokoroLang));
            root.Add(b.BindFloat("Length scale", b.Profile.kokoroLengthScale, ProfileField.KokoroLengthScale));
        }

        internal static void BuildKitten(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindFile("Model", b.Profile.kittenModel, ProfileField.KittenModel));
            root.Add(b.BindFile("Voices", b.Profile.kittenVoices, ProfileField.KittenVoices, "bin", "voices"));
            root.Add(b.BindFile("Tokens", b.Profile.kittenTokens, ProfileField.KittenTokens, "txt", "tokens"));
            root.Add(b.BindFolder("Data dir", b.Profile.kittenDataDir, ProfileField.KittenDataDir, "espeak-ng-data"));
            root.Add(b.BindFloat("Length scale", b.Profile.kittenLengthScale, ProfileField.KittenLengthScale));
        }

        internal static void BuildZipVoice(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindFile("Tokens", b.Profile.zipVoiceTokens, ProfileField.ZipVoiceTokens, "txt", "tokens"));
            root.Add(b.BindFile("Encoder", b.Profile.zipVoiceEncoder, ProfileField.ZipVoiceEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.zipVoiceDecoder, ProfileField.ZipVoiceDecoder, keyword: "decoder"));
            root.Add(b.BindFile("Vocoder", b.Profile.zipVoiceVocoder, ProfileField.ZipVoiceVocoder, keyword: "vocos"));
            root.Add(b.BindFolder("Data dir", b.Profile.zipVoiceDataDir, ProfileField.ZipVoiceDataDir, "espeak-ng-data"));
            root.Add(b.BindFile("Lexicon", b.Profile.zipVoiceLexicon, ProfileField.ZipVoiceLexicon, "txt", "lexicon"));
            root.Add(b.BindFloat("Feat scale", b.Profile.zipVoiceFeatScale, ProfileField.ZipVoiceFeatScale));
            root.Add(b.BindFloat("T-shift", b.Profile.zipVoiceTshift, ProfileField.ZipVoiceTshift));
            root.Add(b.BindFloat("Target RMS", b.Profile.zipVoiceTargetRms, ProfileField.ZipVoiceTargetRms));
            root.Add(b.BindFloat("Guidance scale", b.Profile.zipVoiceGuidanceScale, ProfileField.ZipVoiceGuidanceScale));
        }

        internal static void BuildPocket(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindFile("LM flow", b.Profile.pocketLmFlow, ProfileField.PocketLmFlow, keyword: "lm_flow"));
            root.Add(b.BindFile("LM main", b.Profile.pocketLmMain, ProfileField.PocketLmMain, keyword: "lm_main"));
            root.Add(b.BindFile("Encoder", b.Profile.pocketEncoder, ProfileField.PocketEncoder, keyword: "encoder"));
            root.Add(b.BindFile("Decoder", b.Profile.pocketDecoder, ProfileField.PocketDecoder, keyword: "decoder"));
            root.Add(b.BindFile("Text conditioner", b.Profile.pocketTextConditioner, ProfileField.PocketTextConditioner, keyword: "text_conditioner"));
            root.Add(b.BindFile("vocab.json", b.Profile.pocketVocabJson, ProfileField.PocketVocabJson, "json", "vocab"));
            root.Add(b.BindFile("token_scores.json", b.Profile.pocketTokenScoresJson, ProfileField.PocketTokenScoresJson, "json", "token_scores"));
        }
    }
}
