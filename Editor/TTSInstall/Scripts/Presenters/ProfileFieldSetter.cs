using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Applies a value to the correct field on <see cref="TtsProfile"/>
    /// based on <see cref="ProfileField"/> enum.
    /// </summary>
    internal static class ProfileFieldSetter
    {
        internal static void SetString(TtsProfile p, ProfileField f, string v)
        {
            switch (f)
            {
                case ProfileField.ProfileName: p.profileName = v; break;
                case ProfileField.SourceUrl: p.sourceUrl = v; break;
                case ProfileField.RuleFsts: p.ruleFsts = v; break;
                case ProfileField.RuleFars: p.ruleFars = v; break;
                case ProfileField.Provider: p.provider = v; break;

                case ProfileField.VitsModel: p.vitsModel = v; break;
                case ProfileField.VitsTokens: p.vitsTokens = v; break;
                case ProfileField.VitsLexicon: p.vitsLexicon = v; break;
                case ProfileField.VitsDataDir: p.vitsDataDir = v; break;
                case ProfileField.VitsDictDir: p.vitsDictDir = v; break;

                case ProfileField.MatchaAcousticModel: p.matchaAcousticModel = v; break;
                case ProfileField.MatchaVocoder: p.matchaVocoder = v; break;
                case ProfileField.MatchaTokens: p.matchaTokens = v; break;
                case ProfileField.MatchaLexicon: p.matchaLexicon = v; break;
                case ProfileField.MatchaDataDir: p.matchaDataDir = v; break;
                case ProfileField.MatchaDictDir: p.matchaDictDir = v; break;

                case ProfileField.KokoroModel: p.kokoroModel = v; break;
                case ProfileField.KokoroVoices: p.kokoroVoices = v; break;
                case ProfileField.KokoroTokens: p.kokoroTokens = v; break;
                case ProfileField.KokoroDataDir: p.kokoroDataDir = v; break;
                case ProfileField.KokoroDictDir: p.kokoroDictDir = v; break;
                case ProfileField.KokoroLexicon: p.kokoroLexicon = v; break;
                case ProfileField.KokoroLang: p.kokoroLang = v; break;

                case ProfileField.KittenModel: p.kittenModel = v; break;
                case ProfileField.KittenVoices: p.kittenVoices = v; break;
                case ProfileField.KittenTokens: p.kittenTokens = v; break;
                case ProfileField.KittenDataDir: p.kittenDataDir = v; break;

                case ProfileField.ZipVoiceTokens: p.zipVoiceTokens = v; break;
                case ProfileField.ZipVoiceEncoder: p.zipVoiceEncoder = v; break;
                case ProfileField.ZipVoiceDecoder: p.zipVoiceDecoder = v; break;
                case ProfileField.ZipVoiceVocoder: p.zipVoiceVocoder = v; break;
                case ProfileField.ZipVoiceDataDir: p.zipVoiceDataDir = v; break;
                case ProfileField.ZipVoiceLexicon: p.zipVoiceLexicon = v; break;

                case ProfileField.PocketLmFlow: p.pocketLmFlow = v; break;
                case ProfileField.PocketLmMain: p.pocketLmMain = v; break;
                case ProfileField.PocketEncoder: p.pocketEncoder = v; break;
                case ProfileField.PocketDecoder: p.pocketDecoder = v; break;
                case ProfileField.PocketTextConditioner: p.pocketTextConditioner = v; break;
                case ProfileField.PocketVocabJson: p.pocketVocabJson = v; break;
                case ProfileField.PocketTokenScoresJson: p.pocketTokenScoresJson = v; break;

                case ProfileField.RemoteBaseUrl: p.remoteBaseUrl = v; break;
            }
        }

        internal static void SetFloat(TtsProfile p, ProfileField f, float v)
        {
            switch (f)
            {
                case ProfileField.Speed: p.speed = v; break;
                case ProfileField.SilenceScale: p.silenceScale = v; break;

                case ProfileField.VitsNoiseScale: p.vitsNoiseScale = v; break;
                case ProfileField.VitsNoiseScaleW: p.vitsNoiseScaleW = v; break;
                case ProfileField.VitsLengthScale: p.vitsLengthScale = v; break;

                case ProfileField.MatchaNoiseScale: p.matchaNoiseScale = v; break;
                case ProfileField.MatchaLengthScale: p.matchaLengthScale = v; break;

                case ProfileField.KokoroLengthScale: p.kokoroLengthScale = v; break;
                case ProfileField.KittenLengthScale: p.kittenLengthScale = v; break;

                case ProfileField.ZipVoiceFeatScale: p.zipVoiceFeatScale = v; break;
                case ProfileField.ZipVoiceTshift: p.zipVoiceTshift = v; break;
                case ProfileField.ZipVoiceTargetRms: p.zipVoiceTargetRms = v; break;
                case ProfileField.ZipVoiceGuidanceScale: p.zipVoiceGuidanceScale = v; break;
            }
        }

        internal static void SetInt(TtsProfile p, ProfileField f, int v)
        {
            switch (f)
            {
                case ProfileField.SpeakerId: p.speakerId = v; break;
                case ProfileField.MaxNumSentences: p.maxNumSentences = v; break;
                case ProfileField.NumThreads: p.numThreads = v; break;
            }
        }
    }
}
