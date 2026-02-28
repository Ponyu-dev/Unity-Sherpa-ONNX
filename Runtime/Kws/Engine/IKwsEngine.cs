using System;
using PonyuDev.SherpaOnnx.Kws.Data;

namespace PonyuDev.SherpaOnnx.Kws.Engine
{
    /// <summary>
    /// Abstraction over the native <c>KeywordSpotter</c> +
    /// <c>OnlineStream</c> pair. Manages session lifecycle,
    /// audio input, decode polling, and keyword detection events.
    /// </summary>
    public interface IKwsEngine : IDisposable
    {
        bool IsLoaded { get; }
        bool IsSessionActive { get; }

        void Load(KwsProfile profile, string modelDir);
        void Unload();

        void StartSession();
        void StopSession();

        void AcceptSamples(float[] samples, int sampleRate);
        void ProcessAvailableFrames();

        /// <summary>
        /// Fires when a keyword is detected.
        /// The stream is automatically reset after detection.
        /// </summary>
        event Action<KwsResult> KeywordDetected;
    }
}
