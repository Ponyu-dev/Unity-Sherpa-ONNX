using System;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Tts.Data
{
    /// <summary>
    /// Flat serializable TTS model profile.
    /// Contains fields for all model types; unused fields are ignored at runtime.
    /// Mirrors sherpa-onnx OfflineTtsModelConfig + per-architecture configs.
    /// </summary>
    [Serializable]
    public sealed class TtsProfile : IProfileData
    {
        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        // ── Identity ──

        public string profileName = "New Profile";
        public TtsModelType modelType = TtsModelType.Vits;
        public TtsModelSource modelSource = TtsModelSource.Local;
        public string sourceUrl = "";

        // ── Common (OfflineTtsModelConfig) ──

        public int numThreads = 1;
        public string provider = "cpu";

        // ── Safety ──

        /// <summary>
        /// Allow loading INT8 quantized models. Disabled by default
        /// because INT8 models crash on devices without INT8 ONNX
        /// operator support (segfault inside the native constructor).
        /// Enable only if you are certain your target platform
        /// supports INT8 inference.
        /// </summary>
        public bool allowInt8;

        // ── Generation parameters (OfflineTtsConfig) ──

        public int speakerId;
        public float speed = 1.0f;
        public string ruleFsts = "";
        public string ruleFars = "";
        public int maxNumSentences = 1;
        public float silenceScale = 0.2f;

        // ── VITS ──

        public string vitsModel = "";
        public string vitsLexicon = "";
        public string vitsTokens = "";
        public string vitsDataDir = "";
        public string vitsDictDir = "";
        public float vitsNoiseScale = 0.667f;
        public float vitsNoiseScaleW = 0.8f;
        public float vitsLengthScale = 1.0f;

        // ── Matcha ──

        public string matchaAcousticModel = "";
        public string matchaVocoder = "";
        public string matchaLexicon = "";
        public string matchaTokens = "";
        public string matchaDataDir = "";
        public string matchaDictDir = "";
        public float matchaNoiseScale = 0.667f;
        public float matchaLengthScale = 1.0f;

        // ── Kokoro ──

        public string kokoroModel = "";
        public string kokoroVoices = "";
        public string kokoroTokens = "";
        public string kokoroDataDir = "";
        public string kokoroDictDir = "";
        public string kokoroLexicon = "";
        public string kokoroLang = "";
        public float kokoroLengthScale = 1.0f;

        // ── Kitten ──

        public string kittenModel = "";
        public string kittenVoices = "";
        public string kittenTokens = "";
        public string kittenDataDir = "";
        public float kittenLengthScale = 1.0f;

        // ── ZipVoice ──

        public string zipVoiceTokens = "";
        public string zipVoiceEncoder = "";
        public string zipVoiceDecoder = "";
        public string zipVoiceVocoder = "";
        public string zipVoiceDataDir = "";
        public string zipVoiceLexicon = "";
        public float zipVoiceFeatScale = 0.1f;
        public float zipVoiceTshift = 0.5f;
        public float zipVoiceTargetRms = 0.1f;
        public float zipVoiceGuidanceScale = 1.0f;

        // ── Pocket ──

        public string pocketLmFlow = "";
        public string pocketLmMain = "";
        public string pocketEncoder = "";
        public string pocketDecoder = "";
        public string pocketTextConditioner = "";
        public string pocketVocabJson = "";
        public string pocketTokenScoresJson = "";

        // ── Remote ──

        public string remoteBaseUrl = "";
    }
}
