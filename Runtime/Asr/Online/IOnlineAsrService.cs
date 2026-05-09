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
