using System;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Asr.Online.Data
{
    /// <summary>
    /// Flat serializable online (streaming) ASR model profile.
    /// Contains fields for all streaming model types;
    /// unused fields are ignored at runtime.
    /// </summary>
    [Serializable]
    public sealed class OnlineAsrProfile : IModelProfile
    {
        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        // ── Identity ──

        public string profileName = "New Online Profile";
        public OnlineAsrModelType modelType = OnlineAsrModelType.Transducer;
        public ModelSource modelSource = ModelSource.Local;
        public string sourceUrl = "";

        ModelSource IModelProfile.ModelSource
        {
            get => modelSource;
            set => modelSource = value;
        }

        string IModelProfile.SourceUrl => sourceUrl;
        string IModelProfile.RemoteBaseUrl => remoteBaseUrl;

        // ── Common (OnlineModelConfig) ──

        public int numThreads = 1;
        public string provider = "cpu";
        public string tokens = "";

        // ── Safety ──

        /// <summary>
        /// Allow loading INT8 quantized models. Disabled by default
        /// because INT8 models crash on devices without INT8 ONNX
        /// operator support (segfault inside the native constructor).
        /// Enable only if you are certain your target platform
        /// supports INT8 inference.
        /// </summary>
        public bool allowInt8;

        // ── FeatureConfig ──

        public int sampleRate = 16000;
        public int featureDim = 80;

        // ── OnlineRecognizerConfig ──

        public string decodingMethod = "greedy_search";
        public int maxActivePaths = 4;
        public string hotwordsFile = "";
        public float hotwordsScore = 1.5f;
        public string ruleFsts = "";
        public string ruleFars = "";
        public float blankPenalty;

        // ── Endpoint detection ──

        public bool enableEndpoint = true;
        public float rule1MinTrailingSilence = 1.2f;
        public float rule2MinTrailingSilence = 2.4f;
        public float rule3MinUtteranceLength = 20.0f;

        // ── CtcFstDecoderConfig ──

        public string ctcFstDecoderGraph = "";
        public int ctcFstDecoderMaxActive = 3000;

        // ── Transducer ──

        public string transducerEncoder = "";
        public string transducerDecoder = "";
        public string transducerJoiner = "";

        // ── Paraformer ──

        public string paraformerEncoder = "";
        public string paraformerDecoder = "";

        // ── Zipformer2Ctc ──

        public string zipformer2CtcModel = "";

        // ── NemoCtc ──

        public string nemoCtcModel = "";

        // ── ToneCtc ──

        public string toneCtcModel = "";

        // ── Remote ──

        public string remoteBaseUrl = "";
    }
}
