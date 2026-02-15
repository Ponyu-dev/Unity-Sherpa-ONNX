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
            root.Add(b.BindText("Model", b.Profile.vitsModel, ProfileField.VitsModel));
            root.Add(b.BindText("Tokens", b.Profile.vitsTokens, ProfileField.VitsTokens));
            root.Add(b.BindText("Lexicon", b.Profile.vitsLexicon, ProfileField.VitsLexicon));
            root.Add(b.BindText("Data dir", b.Profile.vitsDataDir, ProfileField.VitsDataDir));
            root.Add(b.BindText("Dict dir", b.Profile.vitsDictDir, ProfileField.VitsDictDir));
            root.Add(b.BindFloat("Noise scale", b.Profile.vitsNoiseScale, ProfileField.VitsNoiseScale));
            root.Add(b.BindFloat("Noise scale W", b.Profile.vitsNoiseScaleW, ProfileField.VitsNoiseScaleW));
            root.Add(b.BindFloat("Length scale", b.Profile.vitsLengthScale, ProfileField.VitsLengthScale));
        }

        internal static void BuildMatcha(
            VisualElement root, ProfileFieldBinder b,
            TtsProjectSettings settings, Action onRefresh)
        {
            root.Add(b.BindText("Acoustic model", b.Profile.matchaAcousticModel, ProfileField.MatchaAcousticModel));
            root.Add(b.BindText("Vocoder", b.Profile.matchaVocoder, ProfileField.MatchaVocoder));

            var vocoderField = new MatchaVocoderProfileField(
                b.Profile, settings, onRefresh);
            root.Add(vocoderField.Build());

            root.Add(b.BindText("Tokens", b.Profile.matchaTokens, ProfileField.MatchaTokens));
            root.Add(b.BindText("Lexicon", b.Profile.matchaLexicon, ProfileField.MatchaLexicon));
            root.Add(b.BindText("Data dir", b.Profile.matchaDataDir, ProfileField.MatchaDataDir));
            root.Add(b.BindText("Dict dir", b.Profile.matchaDictDir, ProfileField.MatchaDictDir));
            root.Add(b.BindFloat("Noise scale", b.Profile.matchaNoiseScale, ProfileField.MatchaNoiseScale));
            root.Add(b.BindFloat("Length scale", b.Profile.matchaLengthScale, ProfileField.MatchaLengthScale));
        }

        internal static void BuildKokoro(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.kokoroModel, ProfileField.KokoroModel));
            root.Add(b.BindText("Voices", b.Profile.kokoroVoices, ProfileField.KokoroVoices));
            root.Add(b.BindText("Tokens", b.Profile.kokoroTokens, ProfileField.KokoroTokens));
            root.Add(b.BindText("Data dir", b.Profile.kokoroDataDir, ProfileField.KokoroDataDir));
            root.Add(b.BindText("Dict dir", b.Profile.kokoroDictDir, ProfileField.KokoroDictDir));
            root.Add(b.BindText("Lexicon", b.Profile.kokoroLexicon, ProfileField.KokoroLexicon));
            root.Add(b.BindText("Language", b.Profile.kokoroLang, ProfileField.KokoroLang));
            root.Add(b.BindFloat("Length scale", b.Profile.kokoroLengthScale, ProfileField.KokoroLengthScale));
        }

        internal static void BuildKitten(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindText("Model", b.Profile.kittenModel, ProfileField.KittenModel));
            root.Add(b.BindText("Voices", b.Profile.kittenVoices, ProfileField.KittenVoices));
            root.Add(b.BindText("Tokens", b.Profile.kittenTokens, ProfileField.KittenTokens));
            root.Add(b.BindText("Data dir", b.Profile.kittenDataDir, ProfileField.KittenDataDir));
            root.Add(b.BindFloat("Length scale", b.Profile.kittenLengthScale, ProfileField.KittenLengthScale));
        }

        internal static void BuildZipVoice(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindText("Tokens", b.Profile.zipVoiceTokens, ProfileField.ZipVoiceTokens));
            root.Add(b.BindText("Encoder", b.Profile.zipVoiceEncoder, ProfileField.ZipVoiceEncoder));
            root.Add(b.BindText("Decoder", b.Profile.zipVoiceDecoder, ProfileField.ZipVoiceDecoder));
            root.Add(b.BindText("Vocoder", b.Profile.zipVoiceVocoder, ProfileField.ZipVoiceVocoder));
            root.Add(b.BindText("Data dir", b.Profile.zipVoiceDataDir, ProfileField.ZipVoiceDataDir));
            root.Add(b.BindText("Lexicon", b.Profile.zipVoiceLexicon, ProfileField.ZipVoiceLexicon));
            root.Add(b.BindFloat("Feat scale", b.Profile.zipVoiceFeatScale, ProfileField.ZipVoiceFeatScale));
            root.Add(b.BindFloat("T-shift", b.Profile.zipVoiceTshift, ProfileField.ZipVoiceTshift));
            root.Add(b.BindFloat("Target RMS", b.Profile.zipVoiceTargetRms, ProfileField.ZipVoiceTargetRms));
            root.Add(b.BindFloat("Guidance scale", b.Profile.zipVoiceGuidanceScale, ProfileField.ZipVoiceGuidanceScale));
        }

        internal static void BuildPocket(
            VisualElement root, ProfileFieldBinder b)
        {
            root.Add(b.BindText("LM flow", b.Profile.pocketLmFlow, ProfileField.PocketLmFlow));
            root.Add(b.BindText("LM main", b.Profile.pocketLmMain, ProfileField.PocketLmMain));
            root.Add(b.BindText("Encoder", b.Profile.pocketEncoder, ProfileField.PocketEncoder));
            root.Add(b.BindText("Decoder", b.Profile.pocketDecoder, ProfileField.PocketDecoder));
            root.Add(b.BindText("Text conditioner", b.Profile.pocketTextConditioner, ProfileField.PocketTextConditioner));
            root.Add(b.BindText("vocab.json", b.Profile.pocketVocabJson, ProfileField.PocketVocabJson));
            root.Add(b.BindText("token_scores.json", b.Profile.pocketTokenScoresJson, ProfileField.PocketTokenScoresJson));
        }
    }
}
