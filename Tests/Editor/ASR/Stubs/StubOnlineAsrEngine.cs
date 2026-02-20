using System;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;

namespace PonyuDev.SherpaOnnx.Tests.Stubs
{
    /// <summary>
    /// Minimal <see cref="IOnlineAsrEngine"/> stub for unit tests.
    /// Tracks call counts, fires pending events on
    /// <see cref="ProcessAvailableFrames"/>.
    /// </summary>
    public sealed class StubOnlineAsrEngine : IOnlineAsrEngine
    {
        public bool IsLoaded { get; set; }
        public bool IsSessionActive { get; set; }

        // ── Call counters ──

        public int LoadCallCount { get; private set; }
        public int StartSessionCallCount { get; private set; }
        public int StopSessionCallCount { get; private set; }
        public int AcceptSamplesCallCount { get; private set; }
        public int ProcessFramesCallCount { get; private set; }
        public int ResetStreamCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }

        // ── Last arguments ──

        public OnlineAsrProfile LastProfile { get; private set; }
        public string LastModelDir { get; private set; }

        // ── Pending results (fired on ProcessAvailableFrames) ──

        public OnlineAsrResult PendingPartialResult { get; set; }
        public OnlineAsrResult PendingFinalResult { get; set; }
        public bool PendingEndpoint { get; set; }

        // ── State ──

        public bool Disposed { get; private set; }

        // ── Events ──

        public event Action<OnlineAsrResult> PartialResultReady;
        public event Action<OnlineAsrResult> FinalResultReady;
        public event Action EndpointDetected;

        // ── IOnlineAsrEngine ──

        public void Load(OnlineAsrProfile profile, string modelDir)
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

        public void ResetStream()
        {
            ResetStreamCallCount++;
        }

        public void Dispose()
        {
            Disposed = true;
            Unload();
        }

        // ── Helpers ──

        private void FirePendingEvents()
        {
            if (PendingPartialResult != null)
            {
                PartialResultReady?.Invoke(PendingPartialResult);
                PendingPartialResult = null;
            }

            if (PendingFinalResult != null)
            {
                FinalResultReady?.Invoke(PendingFinalResult);
                PendingFinalResult = null;
            }

            if (PendingEndpoint)
            {
                EndpointDetected?.Invoke();
                PendingEndpoint = false;
            }
        }
    }
}
