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

        // ── Simple generation ──

        TtsResult Generate(string text);
        TtsResult Generate(string text, float speed, int speakerId);
        Task<TtsResult> GenerateAsync(string text);
        Task<TtsResult> GenerateAsync(string text, float speed, int speakerId);

        // ── Callback generation ──

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

        // ── Async callback generation ──

        /// <summary>
        /// <see cref="GenerateWithCallback"/> on a background thread.
        /// </summary>
        Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback);

        /// <summary>
        /// <see cref="GenerateWithCallbackProgress"/> on a background thread.
        /// </summary>
        Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback);

        /// <summary>
        /// <see cref="GenerateWithConfig"/> on a background thread.
        /// </summary>
        Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback);
    }
}
