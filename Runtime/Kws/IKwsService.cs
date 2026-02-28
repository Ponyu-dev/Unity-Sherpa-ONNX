using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Kws.Data;
using PonyuDev.SherpaOnnx.Kws.Engine;

namespace PonyuDev.SherpaOnnx.Kws
{
    /// <summary>
    /// Public contract for keyword spotting operations.
    /// Designed for always-on background listening.
    /// The default implementation is <see cref="KwsService"/>.
    /// </summary>
    public interface IKwsService : IDisposable
    {
        bool IsReady { get; }
        bool IsSessionActive { get; }
        KwsProfile ActiveProfile { get; }
        KwsSettingsData Settings { get; }

        void Initialize();

        UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default);

        void LoadProfile(KwsProfile profile);
        void SwitchProfile(int index);
        void SwitchProfile(string profileName);

        void StartSession();
        void StopSession();

        void AcceptSamples(float[] samples, int sampleRate);
        void ProcessAvailableFrames();

        /// <summary>
        /// Fires when a keyword is detected.
        /// The stream is automatically reset after detection.
        /// </summary>
        event Action<KwsResult> OnKeywordDetected;
    }
}
