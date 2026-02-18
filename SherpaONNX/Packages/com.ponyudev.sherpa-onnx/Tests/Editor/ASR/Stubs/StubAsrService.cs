using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="IAsrService"/> stub for pipeline tests.
    /// Returns configurable results and tracks call counts.
    /// </summary>
    public sealed class StubAsrService : IAsrService
    {
        public bool IsReady { get; set; } = true;
        public AsrProfile ActiveProfile { get; set; }
        public AsrSettingsData Settings { get; set; }
        public int EnginePoolSize { get; set; } = 1;

        // ── Call counters ──

        public int RecognizeCallCount { get; private set; }

        // ── Last arguments ──

        public float[] LastSamples { get; private set; }
        public int LastSampleRate { get; private set; }

        // ── Configurable output ──

        public AsrResult ResultToReturn { get; set; }

        // ── IAsrService ──

        public void Initialize() { }

        public UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            return UniTask.CompletedTask;
        }

        public void LoadProfile(AsrProfile profile)
        {
            ActiveProfile = profile;
        }

        public void SwitchProfile(int index) { }
        public void SwitchProfile(string profileName) { }

        public AsrResult Recognize(float[] samples, int sampleRate)
        {
            RecognizeCallCount++;
            LastSamples = samples;
            LastSampleRate = sampleRate;
            return ResultToReturn;
        }

        public Task<AsrResult> RecognizeAsync(
            float[] samples, int sampleRate)
        {
            var result = Recognize(samples, sampleRate);
            return Task.FromResult(result);
        }

        public void Dispose() { }
    }
}
