#if SHERPA_ONNX
using System.Collections;
using PonyuDev.SherpaOnnx.Tts.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Tts.Config
{
    /// <summary>
    /// Maps managed <see cref="TtsGenerationConfig"/> to native
    /// <see cref="OfflineTtsGenerationConfig"/>.
    /// </summary>
    public static class TtsGenerationConfigMapper
    {
        public static OfflineTtsGenerationConfig ToNative(TtsGenerationConfig config)
        {
            var native = new OfflineTtsGenerationConfig
            {
                SilenceScale = config.SilenceScale,
                Speed = config.Speed,
                Sid = config.SpeakerId,
                ReferenceAudio = config.ReferenceAudio,
                ReferenceSampleRate = config.ReferenceSampleRate,
                ReferenceText = config.ReferenceText ?? "",
                NumSteps = config.NumSteps,
            };

            if (config.Extra != null && config.Extra.Count > 0)
            {
                var table = new Hashtable();
                foreach (var kvp in config.Extra)
                {
                    table[kvp.Key] = kvp.Value;
                }
                native.Extra = table;
            }

            return native;
        }
    }
}
#endif
