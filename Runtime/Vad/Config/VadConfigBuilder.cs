#if SHERPA_ONNX
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Vad.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Vad.Config
{
    /// <summary>
    /// Maps a <see cref="VadProfile"/> to a native
    /// <see cref="VadModelConfig"/> struct ready for engine creation.
    /// </summary>
    public static class VadConfigBuilder
    {
        public static VadModelConfig Build(VadProfile profile, string modelDir)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] VadConfigBuilder.Build: " +
                $"profile='{profile.profileName}', " +
                $"modelType={profile.modelType}, " +
                $"modelDir='{modelDir}'");

            var config = new VadModelConfig();

            config.SampleRate = profile.sampleRate;
            config.NumThreads = profile.numThreads;
            config.Provider = profile.provider ?? "cpu";

            string resolvedModel = R(modelDir, profile.model);

            switch (profile.modelType)
            {
                case VadModelType.SileroVad:
                    BuildSileroVad(ref config, profile, resolvedModel);
                    break;
                case VadModelType.TenVad:
                    BuildTenVad(ref config, profile, resolvedModel);
                    break;
            }

            return config;
        }

        // ── Per-model builders ──

        private static void BuildSileroVad(ref VadModelConfig c, VadProfile p, string model)
        {
            c.SileroVad.Model = model;
            c.SileroVad.Threshold = p.threshold;
            c.SileroVad.MinSilenceDuration = p.minSilenceDuration;
            c.SileroVad.MinSpeechDuration = p.minSpeechDuration;
            c.SileroVad.MaxSpeechDuration = p.maxSpeechDuration;
            c.SileroVad.WindowSize = p.windowSize;

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] SileroVad config: Model='{model}', Threshold={p.threshold}");
        }

        private static void BuildTenVad(ref VadModelConfig c, VadProfile p, string model)
        {
            c.TenVad.Model = model;
            c.TenVad.Threshold = p.threshold;
            c.TenVad.MinSilenceDuration = p.minSilenceDuration;
            c.TenVad.MinSpeechDuration = p.minSpeechDuration;
            c.TenVad.MaxSpeechDuration = p.maxSpeechDuration;
            c.TenVad.WindowSize = p.windowSize;

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] TenVad config: Model='{model}', Threshold={p.threshold}");
        }

        // ── Shorthand ──

        private static string R(string modelDir, string relativePath)
        {
            return VadModelPathResolver.Resolve(modelDir, relativePath);
        }
    }
}
#endif
