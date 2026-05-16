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
        /// Same as <see cref="SwitchProfile(int)"/> but runs the
        /// native engine load on the thread pool — the UI thread is
        /// not blocked. Re-fires <c>ProfileReadyEvent</c> through the
        /// callback that was passed to the most recent
        /// <see cref="InitializeAsync"/>.
        /// </summary>
        UniTask SwitchProfileAsync(int index, CancellationToken ct = default);

        /// <summary>
        /// Same as <see cref="SwitchProfile(string)"/> but runs the
        /// native engine load on the thread pool — the UI thread is
        /// not blocked.
        /// </summary>
        UniTask SwitchProfileAsync(string profileName, CancellationToken ct = default);

        /// <summary>
        /// True when <paramref name="profileName"/> can be switched to
        /// without breaking — its model files are reachable on disk.
        /// Always true for the currently-active profile and for
        /// <see cref="Common.Data.ModelSource.Remote"/> profiles with a
        /// non-empty source URL. False for
        /// <see cref="Common.Data.ModelSource.Local"/> /
        /// <see cref="Common.Data.ModelSource.LocalZip"/> profiles
        /// whose model directory is missing.
        /// </summary>
        bool IsProfileAvailable(string profileName);

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
