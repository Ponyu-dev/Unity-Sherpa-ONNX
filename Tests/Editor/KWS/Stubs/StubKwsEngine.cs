using System;
using PonyuDev.SherpaOnnx.Kws.Data;
using PonyuDev.SherpaOnnx.Kws.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="IKwsEngine"/> stub for unit tests.
    /// Tracks call counts, fires pending events on
    /// <see cref="ProcessAvailableFrames"/>.
    /// </summary>
    public sealed class StubKwsEngine : IKwsEngine
    {
        public bool IsLoaded { get; set; }
        public bool IsSessionActive { get; set; }

        // ── Call counters ──

        public int LoadCallCount { get; private set; }
        public int StartSessionCallCount { get; private set; }
        public int StopSessionCallCount { get; private set; }
        public int AcceptSamplesCallCount { get; private set; }
        public int ProcessFramesCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }

        // ── Last arguments ──

        public KwsProfile LastProfile { get; private set; }
        public string LastModelDir { get; private set; }

        // ── Pending results (fired on ProcessAvailableFrames) ──

        public KwsResult PendingKeywordResult { get; set; }

        // ── State ──

        public bool Disposed { get; private set; }

        // ── Events ──

        public event Action<KwsResult> KeywordDetected;

        // ── IKwsEngine ──

        public void Load(KwsProfile profile, string modelDir)
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
            IsSessionActive = false;
        }

        public void StartSession()
        {
            StartSessionCallCount++;
            IsSessionActive = true;
        }

        public void StopSession()
        {
            StopSessionCallCount++;
            IsSessionActive = false;
        }

        public void AcceptSamples(float[] samples, int sampleRate)
        {
            AcceptSamplesCallCount++;
        }

        public void ProcessAvailableFrames()
        {
            ProcessFramesCallCount++;
            FirePendingEvents();
        }

        public void Dispose()
        {
            Disposed = true;
            Unload();
        }

        // ── Helpers ──

        private void FirePendingEvents()
        {
            if (PendingKeywordResult == null)
                return;

            KeywordDetected?.Invoke(PendingKeywordResult);
            PendingKeywordResult = null;
        }
    }
}
