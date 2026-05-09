using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Asr.Offline
{
    /// <summary>
    /// Public contract for ASR operations.
    /// Implement or mock for testing; the default implementation
    /// is <see cref="AsrService"/>.
    /// </summary>
    public interface IAsrService : IDisposable, IModelDiskUsage
    {
        bool IsReady { get; }
        AsrProfile ActiveProfile { get; }
        AsrSettingsData Settings { get; }

        /// <summary>Number of concurrent native engine instances.</summary>
        int EnginePoolSize { get; set; }

        void Initialize();

        /// <summary>
        /// Async initialization: stages files on Android, loads settings,
        /// and starts the engine. Reports semantic
        /// <see cref="ProfileReadyEvent"/>s via <paramref name="onEvent"/>.
        /// </summary>
        UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
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
