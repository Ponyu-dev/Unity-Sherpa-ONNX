using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;

namespace PonyuDev.SherpaOnnx.Asr.Offline
{
    /// <summary>
    /// Public contract for ASR operations.
    /// Implement or mock for testing; the default implementation
    /// is <see cref="AsrService"/>.
    /// </summary>
    public interface IAsrService : IDisposable
    {
        bool IsReady { get; }
        AsrProfile ActiveProfile { get; }
        AsrSettingsData Settings { get; }

        /// <summary>Number of concurrent native engine instances.</summary>
        int EnginePoolSize { get; set; }

        void Initialize();

        /// <summary>
        /// Async initialization: extracts files on Android,
        /// loads settings, and starts the engine.
        /// </summary>
        UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default);

        void LoadProfile(AsrProfile profile);
        void SwitchProfile(int index);
        void SwitchProfile(string profileName);

        /// <summary>
        /// Recognizes speech from PCM audio samples.
        /// Returns null if the service is not ready.
        /// </summary>
        AsrResult Recognize(float[] samples, int sampleRate);

        /// <summary>
        /// Recognizes speech on a background thread.
        /// Returns null if the service is not ready.
        /// </summary>
        Task<AsrResult> RecognizeAsync(float[] samples, int sampleRate);
    }
}
