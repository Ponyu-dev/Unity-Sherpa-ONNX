#if SHERPA_ONNX
using System;
using System.Runtime.InteropServices;
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

        // ── Lifecycle ──

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

        // ── Simple generation ──

        public TtsResult Generate(string text, float speed, int speakerId)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            var audio = _tts.Generate(text, speed, speakerId);
            return WrapAudio(audio);
        }

        // ── Callback generation ──

        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            OfflineTtsCallback nativeCallback = (IntPtr samples, int n) =>
            {
                var managed = CopySamplesFromNative(samples, n);
                return callback(managed, n);
            };

            var audio = _tts.GenerateWithCallback(
                text, speed, speakerId, nativeCallback);
            GC.KeepAlive(nativeCallback);
            return WrapAudio(audio);
        }

        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            LogGenerationStart(text, speed, speakerId);

            OfflineTtsCallbackProgress nativeCallback =
                (IntPtr samples, int n, float progress) =>
                {
                    var managed = CopySamplesFromNative(samples, n);
                    return callback(managed, n, progress);
                };

            var audio = _tts.GenerateWithCallbackProgress(
                text, speed, speakerId, nativeCallback);
            GC.KeepAlive(nativeCallback);
            return WrapAudio(audio);
        }

        public TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            if (!ValidateBeforeGenerate(text))
                return null;

            if (config == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine.GenerateWithConfig: config is null.");
                return null;
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generating with config: " +
                $"\"{Truncate(text, 60)}\" " +
                $"(speed={config.Speed}, sid={config.SpeakerId})");

            var nativeConfig = TtsGenerationConfigMapper.ToNative(config);

            OfflineTtsCallbackProgressWithArg nativeCallback =
                (IntPtr samples, int n, float progress, IntPtr arg) =>
                {
                    var managed = CopySamplesFromNative(samples, n);
                    return callback(managed, n, progress);
                };

            var audio = _tts.GenerateWithConfig(
                text, nativeConfig, nativeCallback);
            GC.KeepAlive(nativeCallback);

            var result = WrapAudio(audio);
            if (result != null)
                return result;

            // Fallback: model may not support GenerateWithConfig.
            // Retry with GenerateWithCallbackProgress using config params.
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] GenerateWithConfig failed — " +
                "falling back to GenerateWithCallbackProgress.");

            return GenerateWithCallbackProgress(
                text, config.Speed, config.SpeakerId, callback);
        }

        // ── Private helpers ──

        private bool ValidateBeforeGenerate(string text)
        {
            if (_tts == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine: engine not loaded.");
                return false;
            }

            if (string.IsNullOrEmpty(text))
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsEngine: text is empty.");
                return false;
            }

            return true;
        }

        private static void LogGenerationStart(
            string text, float speed, int speakerId)
        {
            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generating: \"{Truncate(text, 60)}\" " +
                $"(speed={speed}, speakerId={speakerId})");
        }

        private static TtsResult WrapAudio(OfflineTtsGeneratedAudio audio)
        {
            if (audio == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] TtsEngine: native returned null audio.");
                return null;
            }

            float[] samples;
            int sampleRate;

            try
            {
                samples = audio.Samples;
                sampleRate = audio.SampleRate;
            }
            catch (Exception)
            {
                // Expected for models that don't support GenerateWithConfig.
                // Caller handles fallback.
                return null;
            }

            if (samples == null || samples.Length == 0)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] TtsEngine: native returned empty samples.");
                return null;
            }

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS generated: {samples.Length} samples, " +
                $"{sampleRate}Hz, {samples.Length / (float)sampleRate:F2}s");

            return new TtsResult(samples, sampleRate);
        }

        private static float[] CopySamplesFromNative(IntPtr ptr, int count)
        {
            if (ptr == IntPtr.Zero || count <= 0)
                return Array.Empty<float>();

            var managed = new float[count];
            Marshal.Copy(ptr, managed, 0, count);
            return managed;
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
