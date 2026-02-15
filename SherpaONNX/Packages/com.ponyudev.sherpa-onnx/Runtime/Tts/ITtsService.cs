using System;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Public contract for TTS operations.
    /// Implement or mock for testing; the default implementation
    /// is <see cref="TtsService"/>.
    /// </summary>
    public interface ITtsService : IDisposable
    {
        bool IsReady { get; }
        TtsProfile ActiveProfile { get; }
        TtsSettingsData Settings { get; }

        void Initialize();
        void LoadProfile(TtsProfile profile);
        void SwitchProfile(int index);
        void SwitchProfile(string profileName);

        TtsResult Generate(string text);
        TtsResult Generate(string text, float speed, int speakerId);
        Task<TtsResult> GenerateAsync(string text);
        Task<TtsResult> GenerateAsync(string text, float speed, int speakerId);
    }
}
