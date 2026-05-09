using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Public contract for TTS operations.
    /// Implement or mock for testing; the default implementation
    /// is <see cref="TtsService"/>.
    /// </summary>
    public interface ITtsService : IDisposable, IModelDiskUsage
    {
        bool IsReady { get; }
        TtsProfile ActiveProfile { get; }
        TtsSettingsData Settings { get; }

        /// <summary>
        /// Sample rate of the currently loaded engine in Hz (e.g. 22050).
        /// Returns 0 if the engine is not loaded. Required for streaming
        /// playback to size the AudioClip buffer before generation starts.
        /// </summary>
        int SampleRate { get; }

        /// <summary>Number of concurrent native engine instances.</summary>
        int EnginePoolSize { get; set; }

        void Initialize();

        /// <summary>
        /// Async initialization: stages files on Android, loads settings,
        /// and starts the engine. Reports semantic
        /// <see cref="ProfileReadyEvent"/>s through <paramref name="onEvent"/>:
        /// <see cref="ProfileReadyPhase.Download"/> /
        /// <see cref="ProfileReadyPhase.Extract"/> /
        /// <see cref="ProfileReadyPhase.Init"/> with their own 0..100
        /// percent, plus terminal <see cref="ProfileReadyPhase.Ready"/> /
        /// <see cref="ProfileReadyPhase.Failed"/>.
        /// </summary>
        UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default);

        void LoadProfile(TtsProfile profile);
        void SwitchProfile(int index);
        void SwitchProfile(string profileName);

        // ── Simple generation (sync — no cancellation) ──

        TtsResult Generate(string text);
        TtsResult Generate(string text, float speed, int speakerId);

        // ── Async generation (cancellable) ──

        /// <summary>
        /// Generates speech on a background thread.
        /// Throws <see cref="System.OperationCanceledException"/> if
        /// <paramref name="ct"/> is triggered before completion.
        /// </summary>
        Task<TtsResult> GenerateAsync(
            string text, CancellationToken ct = default);

        /// <summary>
        /// Generates speech with explicit speed/speakerId on a background thread.
        /// Throws <see cref="System.OperationCanceledException"/> if cancelled.
        /// </summary>
        Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId,
            CancellationToken ct = default);

        // ── Callback generation (sync) ──

        /// <summary>
        /// Generates speech, invoking the callback for each audio chunk.
        /// </summary>
        TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback);

        /// <summary>
        /// Generates speech with progress callback for each chunk.
        /// </summary>
        TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback);

        /// <summary>
        /// Generates speech using an advanced config (reference audio,
        /// numSteps, etc.) with progress callback.
        /// </summary>
        TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback);

        // ── Async callback generation (cancellable) ──

        /// <summary>
        /// <see cref="GenerateWithCallback"/> on a background thread.
        /// </summary>
        Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback,
            CancellationToken ct = default);

        /// <summary>
        /// <see cref="GenerateWithCallbackProgress"/> on a background thread.
        /// </summary>
        Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback,
            CancellationToken ct = default);

        /// <summary>
        /// <see cref="GenerateWithConfig"/> on a background thread.
        /// </summary>
        Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback,
            CancellationToken ct = default);
    }
}
