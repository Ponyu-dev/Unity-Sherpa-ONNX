using PonyuDev.SherpaOnnx.Vad.Data;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    /// <summary>
    /// Applies a value to the correct field on <see cref="VadProfile"/>
    /// based on <see cref="VadProfileField"/> enum.
    /// </summary>
    internal static class VadProfileFieldSetter
    {
        internal static void SetString(VadProfile p, VadProfileField f, string v)
        {
            switch (f)
            {
                case VadProfileField.ProfileName: p.profileName = v; break;
                case VadProfileField.Provider: p.provider = v; break;
                case VadProfileField.Model: p.model = v; break;
            }
        }

        internal static void SetFloat(VadProfile p, VadProfileField f, float v)
        {
            switch (f)
            {
                case VadProfileField.Threshold: p.threshold = v; break;
                case VadProfileField.MinSilenceDuration: p.minSilenceDuration = v; break;
                case VadProfileField.MinSpeechDuration: p.minSpeechDuration = v; break;
                case VadProfileField.MaxSpeechDuration: p.maxSpeechDuration = v; break;
                case VadProfileField.BufferSizeInSeconds: p.bufferSizeInSeconds = v; break;
            }
        }

        internal static void SetInt(VadProfile p, VadProfileField f, int v)
        {
            switch (f)
            {
                case VadProfileField.SampleRate: p.sampleRate = v; break;
                case VadProfileField.NumThreads: p.numThreads = v; break;
                case VadProfileField.WindowSize: p.windowSize = v; break;
            }
        }
    }
}
