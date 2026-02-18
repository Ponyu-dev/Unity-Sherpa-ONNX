using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Vad;
using PonyuDev.SherpaOnnx.Vad.Data;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="IVadService"/> stub for pipeline tests.
    /// Simulates speech detection and segment emission.
    /// </summary>
    public sealed class StubVadService : IVadService
    {
        public bool IsReady { get; set; } = true;
        public VadProfile ActiveProfile { get; set; }
        public VadSettingsData Settings { get; set; }
        public int WindowSize { get; set; } = 512;

        public event Action<VadSegment> OnSegment;
        public event Action OnSpeechStart;
        public event Action OnSpeechEnd;

        // ── Call counters ──

        public int AcceptWaveformCallCount { get; private set; }
        public int FlushCallCount { get; private set; }
        public int ResetCallCount { get; private set; }

        public void Initialize() { }

        public UniTask InitializeAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            return UniTask.CompletedTask;
        }

        public void LoadProfile(VadProfile profile)
        {
            ActiveProfile = profile;
        }

        public void SwitchProfile(int index) { }
        public void SwitchProfile(string profileName) { }

        public void AcceptWaveform(float[] samples)
        {
            AcceptWaveformCallCount++;
        }

        public bool IsSpeechDetected() => false;

        public List<VadSegment> DrainSegments()
        {
            return new List<VadSegment>();
        }

        public void Flush()
        {
            FlushCallCount++;
        }

        public void Reset()
        {
            ResetCallCount++;
        }

        public void Dispose() { }

        // ── Test helpers ──

        public void SimulateSpeechStart()
        {
            OnSpeechStart?.Invoke();
        }

        public void SimulateSpeechEnd()
        {
            OnSpeechEnd?.Invoke();
        }

        public void SimulateSegment(VadSegment segment)
        {
            OnSegment?.Invoke(segment);
        }
    }
}
