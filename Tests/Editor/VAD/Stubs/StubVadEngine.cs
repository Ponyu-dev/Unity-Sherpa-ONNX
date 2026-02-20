using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Vad.Data;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="IVadEngine"/> stub for unit tests.
    /// Tracks call counts and stores last arguments for assertion.
    /// </summary>
    public sealed class StubVadEngine : IVadEngine
    {
        public bool IsLoaded { get; set; }
        public int WindowSize { get; set; } = 512;

        // ── Call counters ──

        public int LoadCallCount { get; private set; }
        public int AcceptWaveformCallCount { get; private set; }
        public int FlushCallCount { get; private set; }
        public int ResetCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }

        // ── Last arguments ──

        public VadProfile LastProfile { get; private set; }
        public string LastModelDir { get; private set; }
        public float[] LastSamples { get; private set; }

        // ── Configurable output ──

        public bool SpeechDetected { get; set; }
        public List<VadSegment> SegmentsToReturn { get; set; } = new();

        // ── State ──

        public bool Disposed { get; private set; }

        // ── IVadEngine ──

        public void Load(VadProfile profile, string modelDir)
        {
            LoadCallCount++;
            LastProfile = profile;
            LastModelDir = modelDir;
            IsLoaded = true;
        }

        public void Unload()
        {
            UnloadCallCount++;
            IsLoaded = false;
        }

        public void AcceptWaveform(float[] samples)
        {
            AcceptWaveformCallCount++;
            LastSamples = samples;
        }

        public bool IsSpeechDetected()
        {
            return SpeechDetected;
        }

        public List<VadSegment> DrainSegments()
        {
            var result = new List<VadSegment>(SegmentsToReturn);
            SegmentsToReturn.Clear();
            return result;
        }

        public void Flush()
        {
            FlushCallCount++;
        }

        public void Reset()
        {
            ResetCallCount++;
        }

        public void Dispose()
        {
            Disposed = true;
            Unload();
        }
    }
}
