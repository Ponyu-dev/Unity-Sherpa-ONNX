using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Vad.Data;

namespace PonyuDev.SherpaOnnx.Vad.Engine
{
    /// <summary>
    /// Abstraction over the native VAD engine.
    /// Allows mocking for tests and swapping implementations.
    /// </summary>
    public interface IVadEngine : IDisposable
    {
        bool IsLoaded { get; }

        /// <summary>Window size in samples for the current model.</summary>
        int WindowSize { get; }

        void Load(VadProfile profile, string modelDir);
        void Unload();

        /// <summary>
        /// Feed a window of samples to the detector.
        /// Must be exactly <see cref="WindowSize"/> samples.
        /// </summary>
        void AcceptWaveform(float[] samples);

        /// <summary>True if speech is currently detected.</summary>
        bool IsSpeechDetected();

        /// <summary>
        /// Drains all completed speech segments from the internal queue.
        /// </summary>
        List<VadSegment> DrainSegments();

        /// <summary>
        /// Flushes the internal buffer, finalizing any pending speech.
        /// Call when recording stops.
        /// </summary>
        void Flush();

        /// <summary>Resets the detector state.</summary>
        void Reset();
    }
}
