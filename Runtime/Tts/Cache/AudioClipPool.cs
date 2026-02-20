using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Cache
{
    /// <summary>
    /// Object pool for <see cref="AudioClip"/> instances.
    /// Reuses clips when sample count matches; creates new otherwise.
    /// Must be used from the main thread only.
    /// </summary>
    public sealed class AudioClipPool : IDisposable
    {
        private int _maxSize;
        private readonly Queue<AudioClip> _pool;

        public AudioClipPool(int maxSize)
        {
            _maxSize = Math.Max(1, maxSize);
            _pool = new Queue<AudioClip>(_maxSize);
        }

        /// <summary>Number of clips currently available in the pool.</summary>
        public int AvailableCount => _pool.Count;

        /// <summary>Maximum pool capacity. Destroys excess clips on shrink.</summary>
        public int MaxSize
        {
            get => _maxSize;
            set
            {
                _maxSize = Math.Max(1, value);
                TrimExcess();
            }
        }

        /// <summary>
        /// Gets a clip sized for the given sample data.
        /// Reuses a pooled clip if sample count matches; creates new otherwise.
        /// </summary>
        public AudioClip Rent(int numSamples, int sampleRate)
        {
            if (numSamples <= 0 || sampleRate <= 0)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] AudioClipPool.Rent: invalid parameters.");
                return null;
            }

            // Try to find a matching clip in the pool.
            int count = _pool.Count;
            for (int i = 0; i < count; i++)
            {
                var clip = _pool.Dequeue();
                if (clip != null
                    && clip.samples == numSamples
                    && clip.frequency == sampleRate)
                {
                    return clip;
                }

                // Not a match — destroy it.
                if (clip != null)
                    UnityEngine.Object.Destroy(clip);
            }

            // No match found — create new.
            return AudioClip.Create("tts-pooled", numSamples, 1, sampleRate, false);
        }

        /// <summary>
        /// Returns a clip to the pool. Destroyed if pool is full.
        /// </summary>
        public void Return(AudioClip clip)
        {
            if (clip == null)
                return;

            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(clip);
            }
            else
            {
                UnityEngine.Object.Destroy(clip);
            }
        }

        /// <summary>Destroys all pooled clips.</summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var clip = _pool.Dequeue();
                if (clip != null)
                    UnityEngine.Object.Destroy(clip);
            }

            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] AudioClipPool cleared.");
        }

        public void Dispose()
        {
            Clear();
        }

        // ── Private ──

        private void TrimExcess()
        {
            while (_pool.Count > _maxSize)
            {
                var clip = _pool.Dequeue();
                if (clip != null)
                    UnityEngine.Object.Destroy(clip);
            }
        }
    }
}
