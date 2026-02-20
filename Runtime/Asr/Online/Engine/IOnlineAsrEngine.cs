using System;
using PonyuDev.SherpaOnnx.Asr.Online.Data;

namespace PonyuDev.SherpaOnnx.Asr.Online.Engine
{
    /// <summary>
    /// Abstraction over the native <c>OnlineRecognizer</c> +
    /// <c>OnlineStream</c> pair. Manages session lifecycle,
    /// audio input, decode polling, and result events.
    /// </summary>
    public interface IOnlineAsrEngine : IDisposable
    {
        bool IsLoaded { get; }
        bool IsSessionActive { get; }

        void Load(OnlineAsrProfile profile, string modelDir);
        void Unload();

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
