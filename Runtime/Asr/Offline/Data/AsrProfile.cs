using System;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Asr.Offline.Data
{
    /// <summary>
    /// Flat serializable ASR model profile.
    /// Contains fields for all model types; unused fields are ignored at runtime.
    /// Mirrors sherpa-onnx OfflineRecognizerConfig + OfflineModelConfig sub-configs.
    /// </summary>
    [Serializable]
    public sealed class AsrProfile : IProfileData
    {
        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        // ── Identity ──

        public string profileName = "New Profile";
        public AsrModelType modelType = AsrModelType.Whisper;

        // ── Common (OfflineModelConfig) ──

        public int numThreads = 1;
        public string provider = "cpu";
        public string tokens = "";

        // ── FeatureConfig ──

        public int sampleRate = 16000;
        public int featureDim = 80;

        // ── OfflineRecognizerConfig ──

        public string decodingMethod = "greedy_search";
        public int maxActivePaths = 4;
        public string hotwordsFile = "";
        public float hotwordsScore = 1.5f;
        public string ruleFsts = "";
        public string ruleFars = "";
        public float blankPenalty;

        // ── LM Config ──

        public string lmModel = "";
        public float lmScale = 0.5f;

        // ── Transducer (Zipformer / Conformer) ──

        public string transducerEncoder = "";
        public string transducerDecoder = "";
        public string transducerJoiner = "";

        // ── Paraformer ──

        public string paraformerModel = "";

        // ── Whisper ──

        public string whisperEncoder = "";
        public string whisperDecoder = "";
        public string whisperLanguage = "";
        public string whisperTask = "transcribe";
        public int whisperTailPaddings = -1;
        public bool whisperEnableTokenTimestamps;
        public bool whisperEnableSegmentTimestamps;

        // ── SenseVoice ──

        public string senseVoiceModel = "";
        public string senseVoiceLanguage = "";
        public bool senseVoiceUseInverseTextNormalization = true;

        // ── Moonshine ──

        public string moonshinePreprocessor = "";
        public string moonshineEncoder = "";
        public string moonshineUncachedDecoder = "";
        public string moonshineCachedDecoder = "";

        // ── NemoCtc ──

        public string nemoCtcModel = "";

        // ── ZipformerCtc ──

        public string zipformerCtcModel = "";

        // ── Tdnn ──

        public string tdnnModel = "";

        // ── FireRedAsr ──

        public string fireRedAsrEncoder = "";
        public string fireRedAsrDecoder = "";

        // ── Dolphin ──

        public string dolphinModel = "";

        // ── Canary ──

        public string canaryEncoder = "";
        public string canaryDecoder = "";
        public string canarySrcLang = "en";
        public string canaryTgtLang = "en";
        public bool canaryUsePnc = true;

        // ── WenetCtc ──

        public string wenetCtcModel = "";

        // ── Omnilingual ──

        public string omnilingualModel = "";

        // ── MedAsr ──

        public string medAsrModel = "";

        // ── FunAsrNano ──

        public string funAsrNanoEncoderAdaptor = "";
        public string funAsrNanoLlm = "";
        public string funAsrNanoEmbedding = "";
        public string funAsrNanoTokenizer = "";
        public string funAsrNanoSystemPrompt = "You are a helpful assistant.";
        public string funAsrNanoUserPrompt = "";
        public int funAsrNanoMaxNewTokens = 512;
        public float funAsrNanoTemperature = 1e-6f;
        public float funAsrNanoTopP = 0.8f;
        public int funAsrNanoSeed = 42;
        public string funAsrNanoLanguage = "";
        public bool funAsrNanoItn;
        public string funAsrNanoHotwords = "";
    }
}
