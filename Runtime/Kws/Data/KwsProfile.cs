using System;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Kws.Data
{
    /// <summary>
    /// Flat serializable KWS model profile.
    /// Mirrors sherpa-onnx <c>KeywordSpotterConfig</c> sub-configs.
    /// Uses Transducer models (encoder/decoder/joiner) + keywords file.
    /// </summary>
    [Serializable]
    public sealed class KwsProfile : IModelProfile
    {
        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        // ── Identity ──

        public string profileName = "New KWS Profile";
        public KwsModelType modelType = KwsModelType.Transducer;
        public ModelSource modelSource = ModelSource.Local;
        public string sourceUrl = "";

        ModelSource IModelProfile.ModelSource
        {
            get => modelSource;
            set => modelSource = value;
        }

        string IModelProfile.SourceUrl => sourceUrl;
        string IModelProfile.RemoteBaseUrl => remoteBaseUrl;

        // ── Common ──

        public int sampleRate = 16000;
        public int featureDim = 80;
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

        // ── Transducer model files ──

        public string tokens = "";
        public string encoder = "";
        public string decoder = "";
        public string joiner = "";

        // ── Keywords ──

        public string keywordsFile = "";

        /// <summary>
        /// Additional keywords passed via <c>keywords_buf</c> at runtime.
        /// Each line: space-separated tokens + optional <c>@TAG :boost #threshold</c>.
        /// These are merged with keywords from <see cref="keywordsFile"/>.
        /// <para>
        /// Format per line: <c>token1 token2 @PHRASE_TAG :1.5 #0.3</c>
        /// </para>
        /// <para>
        /// For per-stream dynamic keywords at runtime, use
        /// <c>KeywordSpotter.CreateStream(keywords)</c> with <c>/</c> as separator.
        /// </para>
        /// </summary>
        public string customKeywords = "";

        public int maxActivePaths = 4;
        public int numTrailingBlanks = 1;
        public float keywordsScore = 1.0f;
        public float keywordsThreshold = 0.25f;

        // ── Remote ──

        public string remoteBaseUrl = "";
    }
}
