using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Vad.Data;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Vad
{
    /// <summary>
    /// Public contract for VAD operations.
    /// Implement or mock for testing; the default implementation
    /// is <see cref="VadService"/>.
    /// </summary>
    public interface IVadService : IDisposable, IModelDiskUsage
    {
        bool IsReady { get; }
        VadProfile ActiveProfile { get; }
        VadSettingsData Settings { get; }

        /// <summary>Window size in samples for the current model.</summary>
        int WindowSize { get; }

        void Initialize();

        /// <summary>
        /// Async initialization: stages files on Android, loads settings,
        /// and starts the engine. Reports semantic
        /// <see cref="ProfileReadyEvent"/>s via <paramref name="onEvent"/>.
        /// </summary>
        UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default);

        void LoadProfile(VadProfile profile);
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
        /// Always true for the active profile and for
        /// <see cref="Common.Data.ModelSource.Remote"/> profiles with a
        /// non-empty source URL.
        /// </summary>
        bool IsProfileAvailable(string profileName);

        /// <summary>
        /// Feed a window of samples to the detector.
        /// Must be exactly <see cref="WindowSize"/> samples.
        /// </summary>
        void AcceptWaveform(float[] samples);

        /// <summary>True if speech is currently detected.</summary>
        bool IsSpeechDetected();

        /// <summary>
        /// Drains all completed speech segments from the internal queue.
        /// </summary>
        List<VadSegment> DrainSegments();

        /// <summary>
        /// Flushes the internal buffer, finalizing any pending speech.
        /// Call when recording stops.
        /// </summary>
        void Flush();

        /// <summary>Resets the detector state.</summary>
        void Reset();

        /// <summary>Fired when a speech segment becomes available.</summary>
        event Action<VadSegment> OnSegment;

        /// <summary>Fired when speech starts.</summary>
        event Action OnSpeechStart;

        /// <summary>Fired when speech ends.</summary>
        event Action OnSpeechEnd;
    }
}
