#if SHERPA_ONNX
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Config;
using PonyuDev.SherpaOnnx.Tts.Data;
using SherpaOnnx;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Wraps the native <see cref="OfflineTts"/> object.
    /// Handles creation, generation and disposal of the native handle.
    /// Never throws — logs errors instead.
    /// </summary>
    public sealed class TtsEngine : ITtsEngine
    {
        private OfflineTts _tts;

        public int SampleRate => _tts?.SampleRate ?? 0;
        public int NumSpeakers => _tts?.NumSpeakers ?? 0;
        public bool IsLoaded => _tts != null;

        public void Load(TtsProfile profile, string modelDir)
        {
            if (profile == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine.Load: profile is null.");
                return;
            }

            Unload();

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS engine loading: {profile.profileName}");

            var config = TtsConfigBuilder.Build(profile, modelDir);
            _tts = new OfflineTts(config);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS engine loaded: {profile.profileName} " +
                $"(sampleRate={SampleRate}, speakers={NumSpeakers})");
        }

        public TtsResult Generate(string text, float speed, int speakerId)
        {
            if (_tts == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine.Generate: engine not loaded.");
                return null;
            }

            if (string.IsNullOrEmpty(text))
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsEngine.Generate: text is empty.");
                return null;
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generating: \"{Truncate(text, 60)}\" " +
                $"(speed={speed}, speakerId={speakerId})");

            var audio = _tts.Generate(text, speed, speakerId);
            float[] samples = audio.Samples;
            int sampleRate = audio.SampleRate;

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generated: {samples.Length} samples, " +
                $"{sampleRate}Hz, {samples.Length / (float)sampleRate:F2}s");

            return new TtsResult(samples, sampleRate);
        }

        public void Unload()
        {
            if (_tts == null)
                return;

            _tts.Dispose();
            _tts = null;
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] TTS engine unloaded.");
        }

        public void Dispose()
        {
            Unload();
        }

        private static string Truncate(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }
    }
}
#endif
