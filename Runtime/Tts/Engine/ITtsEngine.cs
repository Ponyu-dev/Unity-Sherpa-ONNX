using System;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Callback invoked for each audio chunk during generation.
    /// Receives samples and their count.
    /// Return 1 to continue, 0 to stop.
    /// </summary>
    public delegate int TtsCallback(float[] samples, int count);

    /// <summary>
    /// Callback invoked for each audio chunk with generation progress.
    /// Receives samples, count and progress (0..1).
    /// Return 1 to continue, 0 to stop.
    /// </summary>
    public delegate int TtsCallbackProgress(float[] samples, int count, float progress);

    /// <summary>
    /// Abstraction over the native TTS engine.
    /// Allows mocking for tests and swapping implementations.
    /// </summary>
    public interface ITtsEngine : IDisposable
    {
        int SampleRate { get; }
        int NumSpeakers { get; }
        bool IsLoaded { get; }

        /// <summary>Number of native engine instances in the pool.</summary>
        int PoolSize { get; }

        void Load(TtsProfile profile, string modelDir, int poolSize = 1);

        /// <summary>Resize the engine pool at runtime.</summary>
        void Resize(int newPoolSize);

        TtsResult Generate(string text, float speed, int speakerId);

        /// <summary>
        /// Generates speech, invoking the callback for each audio chunk.
        /// </summary>
        TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback);

        /// <summary>
        /// Generates speech, invoking the callback with progress for each chunk.
        /// </summary>
        TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId, TtsCallbackProgress callback);

        /// <summary>
        /// Generates speech using an advanced generation config with progress callback.
        /// </summary>
        TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config, TtsCallbackProgress callback);

        void Unload();
    }
}
