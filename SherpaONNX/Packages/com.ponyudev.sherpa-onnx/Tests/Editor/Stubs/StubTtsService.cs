using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="ITtsService"/> stub for unit tests.
    /// Returns a fixed <see cref="TtsResult"/> on Generate calls.
    /// Tracks call counts for assertion.
    /// </summary>
    public sealed class StubTtsService : ITtsService
    {
        public int GenerateCallCount { get; private set; }
        public int GenerateAsyncCallCount { get; private set; }
        public bool Disposed { get; private set; }

        public bool IsReady { get; set; } = true;

        public TtsProfile ActiveProfile { get; set; } = new TtsProfile
        {
            profileName = "stub",
            speed = 1.0f,
            speakerId = 0
        };

        public TtsSettingsData Settings { get; set; } = new TtsSettingsData();

        public int EnginePoolSize { get; set; } = 1;

        public void Initialize() { }

        public UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            return UniTask.CompletedTask;
        }

        public void LoadProfile(TtsProfile profile)
        {
            ActiveProfile = profile;
        }

        public void SwitchProfile(int index) { }
        public void SwitchProfile(string profileName) { }

        // ── Generation ──

        /// <summary>
        /// Factory for generated results.
        /// Default: 1-sample array with value 0.5f at 22050 Hz.
        /// </summary>
        public Func<string, TtsResult> ResultFactory { get; set; } =
            _ => new TtsResult(new[] { 0.5f }, 22050);

        public TtsResult Generate(string text)
        {
            GenerateCallCount++;
            return ResultFactory(text);
        }

        public TtsResult Generate(string text, float speed, int speakerId)
        {
            GenerateCallCount++;
            return ResultFactory(text);
        }

        public Task<TtsResult> GenerateAsync(string text)
        {
            GenerateAsyncCallCount++;
            return Task.FromResult(ResultFactory(text));
        }

        public Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId)
        {
            GenerateAsyncCallCount++;
            return Task.FromResult(ResultFactory(text));
        }

        // ── Callback methods (minimal stubs) ──

        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            return ResultFactory(text);
        }

        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            return ResultFactory(text);
        }

        public TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            return ResultFactory(text);
        }

        public Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback callback)
        {
            return Task.FromResult(ResultFactory(text));
        }

        public Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId,
            TtsCallbackProgress callback)
        {
            return Task.FromResult(ResultFactory(text));
        }

        public Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config,
            TtsCallbackProgress callback)
        {
            return Task.FromResult(ResultFactory(text));
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
