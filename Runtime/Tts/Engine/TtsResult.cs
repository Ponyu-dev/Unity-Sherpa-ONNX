using System;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Managed wrapper for TTS-generated audio data.
    /// Copies float[] samples from the native result so the native handle
    /// can be disposed immediately after generation.
    /// </summary>
    public sealed class TtsResult : IDisposable
    {
        /// <summary>Raw PCM samples (mono, float32).</summary>
        public float[] Samples { get; private set; }

        /// <summary>Sample rate in Hz (e.g. 22050).</summary>
        public int SampleRate { get; }

        public int NumSamples => Samples?.Length ?? 0;
        public bool IsValid => Samples != null && Samples.Length > 0;

        /// <summary>Duration of the generated audio in seconds.</summary>
        public float DurationSeconds =>
            SampleRate > 0 ? NumSamples / (float)SampleRate : 0f;

        internal TtsResult(float[] samples, int sampleRate)
        {
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Creates a Unity AudioClip from the generated samples.
        /// Must be called on the main thread.
        /// </summary>
        public AudioClip ToAudioClip(string clipName = "tts")
        {
            if (!IsValid)
                throw new InvalidOperationException("TtsResult has no valid samples.");

            var clip = AudioClip.Create(clipName, NumSamples, 1, SampleRate, false);
            clip.SetData(Samples, 0);
            return clip;
        }

        /// <summary>
        /// Creates an independent copy of this result (clones the sample array).
        /// </summary>
        public TtsResult Clone()
        {
            if (!IsValid)
                return null;

            var copy = new float[Samples.Length];
            Array.Copy(Samples, copy, Samples.Length);
            return new TtsResult(copy, SampleRate);
        }

        public void Dispose()
        {
            Samples = null;
        }
    }
}
