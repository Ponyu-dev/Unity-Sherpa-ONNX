using System;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;

namespace PonyuDev.SherpaOnnx.Asr.Offline.Engine
{
    /// <summary>
    /// Abstraction over the native ASR engine.
    /// Allows mocking for tests and swapping implementations.
    /// </summary>
    public interface IAsrEngine : IDisposable
    {
        bool IsLoaded { get; }

        /// <summary>Number of native engine instances in the pool.</summary>
        int PoolSize { get; }

        void Load(AsrProfile profile, string modelDir, int poolSize = 1);

        /// <summary>Resize the engine pool at runtime.</summary>
        void Resize(int newPoolSize);

        /// <summary>
        /// Recognizes speech from PCM audio samples.
        /// Thread-safe via internal pool.
        /// </summary>
        AsrResult Recognize(float[] samples, int sampleRate);

        void Unload();
    }
}
