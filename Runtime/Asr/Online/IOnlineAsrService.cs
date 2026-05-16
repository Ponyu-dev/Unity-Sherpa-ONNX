using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Asr.Online
{
    /// <summary>
    /// Public contract for streaming (online) ASR operations.
    /// The default implementation is <see cref="OnlineAsrService"/>.
    /// </summary>
    public interface IOnlineAsrService : IDisposable, IModelDiskUsage
    {
        bool IsReady { get; }
        bool IsSessionActive { get; }
        OnlineAsrProfile ActiveProfile { get; }
        OnlineAsrSettingsData Settings { get; }

        void Initialize();

        UniTask InitializeAsync(
            Action<ProfileReadyEvent> onEvent = null,
            CancellationToken ct = default);

        void LoadProfile(OnlineAsrProfile profile);
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

        void StartSession();
        void StopSession();

        void AcceptSamples(float[] samples, int sampleRate);
        void ProcessAvailableFrames();
        void ResetStream();

        event Action<OnlineAsrResult> PartialResultReady;
        event Action<OnlineAsrResult> FinalResultReady;
        event Action EndpointDetected;
    }
}
