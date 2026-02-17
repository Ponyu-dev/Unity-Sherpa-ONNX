using System;
using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Tts.Data
{
    /// <summary>
    /// Managed generation config for advanced TTS generation.
    /// Maps to native <c>OfflineTtsGenerationConfig</c> at engine level.
    /// </summary>
    [Serializable]
    public sealed class TtsGenerationConfig
    {
        /// <summary>Scale for silence duration between sentences.</summary>
        public float SilenceScale { get; set; }

        /// <summary>Speech speed factor. 1.0 = normal.</summary>
        public float Speed { get; set; } = 1.0f;

        /// <summary>Speaker ID for multi-speaker models.</summary>
        public int SpeakerId { get; set; }

        /// <summary>Reference audio samples (mono, float32) for voice cloning.</summary>
        public float[] ReferenceAudio { get; set; }

        /// <summary>Sample rate of the reference audio.</summary>
        public int ReferenceSampleRate { get; set; }

        /// <summary>Reference text matching the reference audio.</summary>
        public string ReferenceText { get; set; } = "";

        /// <summary>Number of generation steps (model-specific).</summary>
        public int NumSteps { get; set; }

        /// <summary>Extra key-value parameters for model-specific settings.</summary>
        public Dictionary<string, string> Extra { get; set; }
    }
}
