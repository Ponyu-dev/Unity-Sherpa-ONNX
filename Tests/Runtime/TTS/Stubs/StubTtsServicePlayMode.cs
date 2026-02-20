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
    /// Minimal <see cref="ITtsService"/> stub for Play Mode tests.
    /// </summary>
    public sealed class StubTtsServicePlayMode : ITtsService
    {
        public bool IsReady { get; set; } = true;

        public TtsProfile ActiveProfile { get; set; } = new TtsProfile
        {
            profileName = "stub",
            speed = 1.0f,
            speakerId = 0
        };

        public TtsSettingsData Settings { get; set; } = new TtsSettingsData();
        public int EnginePoolSize { get; set; } = 1;

        public Func<string, TtsResult> ResultFactory { get; set; } =
            _ => new TtsResult(new float[4410], 22050);

        public void Initialize() { }

        public UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            return UniTask.CompletedTask;
        }

        public void LoadProfile(TtsProfile profile) { }
        public void SwitchProfile(int index) { }
        public void SwitchProfile(string profileName) { }

        public TtsResult Generate(string text) => ResultFactory(text);

        public TtsResult Generate(string text, float speed, int speakerId)
            => ResultFactory(text);

        public Task<TtsResult> GenerateAsync(string text)
            => Task.FromResult(ResultFactory(text));

        public Task<TtsResult> GenerateAsync(
            string text, float speed, int speakerId)
            => Task.FromResult(ResultFactory(text));

        public TtsResult GenerateWithCallback(
            string text, float speed, int speakerId, TtsCallback cb)
            => ResultFactory(text);

        public TtsResult GenerateWithCallbackProgress(
            string text, float speed, int speakerId, TtsCallbackProgress cb)
            => ResultFactory(text);

        public TtsResult GenerateWithConfig(
            string text, TtsGenerationConfig config, TtsCallbackProgress cb)
            => ResultFactory(text);

        public Task<TtsResult> GenerateWithCallbackAsync(
            string text, float speed, int speakerId, TtsCallback cb)
            => Task.FromResult(ResultFactory(text));

        public Task<TtsResult> GenerateWithCallbackProgressAsync(
            string text, float speed, int speakerId, TtsCallbackProgress cb)
            => Task.FromResult(ResultFactory(text));

        public Task<TtsResult> GenerateWithConfigAsync(
            string text, TtsGenerationConfig config, TtsCallbackProgress cb)
            => Task.FromResult(ResultFactory(text));

        public void Dispose() { }
    }
}
