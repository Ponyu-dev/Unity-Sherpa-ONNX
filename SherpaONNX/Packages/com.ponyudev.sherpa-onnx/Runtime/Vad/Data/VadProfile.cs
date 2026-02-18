using System;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Vad.Data
{
    /// <summary>
    /// Flat serializable VAD model profile.
    /// Contains fields for all model types; unused fields are ignored.
    /// Mirrors sherpa-onnx <c>VadModelConfig</c> sub-configs.
    /// </summary>
    [Serializable]
    public sealed class VadProfile : IProfileData
    {
        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        // ── Identity ──

        public string profileName = "New VAD Profile";
        public VadModelType modelType = VadModelType.SileroVad;

        // ── Common ──

        public int sampleRate = 16000;
        public int numThreads = 1;
        public string provider = "cpu";

        // ── VAD thresholds ──

        public float threshold = 0.5f;
        public float minSilenceDuration = 0.5f;
        public float minSpeechDuration = 0.25f;
        public float maxSpeechDuration = 5.0f;

        // ── Model file ──

        public string model = "";

        // ── Window size (model-specific default) ──

        public int windowSize = 512;

        // ── Buffer ──

        public float bufferSizeInSeconds = 60f;
    }
}
