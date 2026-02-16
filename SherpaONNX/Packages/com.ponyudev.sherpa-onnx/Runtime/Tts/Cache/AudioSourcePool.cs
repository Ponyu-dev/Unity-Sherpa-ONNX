using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Cache
{
    /// <summary>
    /// Pool of <see cref="AudioSource"/> components for parallel playback.
    /// Creates sources as children of a parent Transform.
    /// Must be used from the main thread only.
    /// </summary>
    public sealed class AudioSourcePool : IDisposable
    {
        private readonly List<AudioSource> _sources;
        private readonly Transform _parent;
        private int _maxSize;

        /// <summary>
        /// Creates and pre-allocates AudioSource components.
        /// </summary>
        /// <param name="parent">Parent transform for the source GameObjects.</param>
        /// <param name="maxSize">Maximum number of sources in the pool.</param>
        public AudioSourcePool(Transform parent, int maxSize)
        {
            _maxSize = Math.Max(1, maxSize);
            _parent = parent;
            _sources = new List<AudioSource>(_maxSize);

            if (parent == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AudioSourcePool: parent is null.");
                return;
            }

            for (int i = 0; i < _maxSize; i++)
                CreateSource(i);
        }

        /// <summary>Total pool capacity.</summary>
        public int MaxSize
        {
            get => _maxSize;
            set
            {
                int newSize = Math.Max(1, value);
                if (newSize == _maxSize)
                    return;

                if (newSize > _maxSize)
                    Grow(newSize);
                else
                    Shrink(newSize);

                _maxSize = newSize;
            }
        }

        /// <summary>Total number of sources (busy + idle).</summary>
        public int TotalCount => _sources.Count;

        /// <summary>Number of idle (not playing) sources.</summary>
        public int AvailableCount
        {
            get
            {
                int count = 0;
                foreach (var s in _sources)
                {
                    if (s != null && !s.isPlaying)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Returns the first idle AudioSource.
        /// Returns null and logs a warning if all are busy.
        /// </summary>
        public AudioSource Rent()
        {
            foreach (var source in _sources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }

            SherpaOnnxLog.RuntimeWarning(
                $"[SherpaOnnx] AudioSourcePool: all {_maxSize} sources are busy.");
            return null;
        }

        /// <summary>
        /// Returns a source to idle state: stops playback, clears clip.
        /// </summary>
        public void Return(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
        }

        /// <summary>
        /// Stops all sources and clears clips, but keeps GameObjects alive.
        /// Pool remains at <see cref="MaxSize"/> and is immediately reusable.
        /// </summary>
        public void Clear()
        {
            foreach (var source in _sources)
            {
                if (source != null)
                {
                    source.Stop();
                    source.clip = null;
                }
            }

            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] AudioSourcePool cleared (sources reset).");
        }

        public void Dispose()
        {
            foreach (var source in _sources)
            {
                if (source != null)
                    UnityEngine.Object.Destroy(source.gameObject);
            }
            _sources.Clear();
        }

        // ── Private ──

        private void CreateSource(int index)
        {
            if (_parent == null)
                return;

            var go = new GameObject($"TtsAudioSource_{index}");
            go.transform.SetParent(_parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sources.Add(source);
        }

        private void Grow(int newSize)
        {
            int current = _sources.Count;
            for (int i = current; i < newSize; i++)
                CreateSource(i);
        }

        private void Shrink(int newSize)
        {
            // Remove idle sources first, then busy ones from the tail.
            while (_sources.Count > newSize)
            {
                int idleIdx = FindLastIdle();
                int removeIdx = idleIdx >= 0 ? idleIdx : _sources.Count - 1;

                var source = _sources[removeIdx];
                _sources.RemoveAt(removeIdx);

                if (source != null)
                {
                    source.Stop();
                    UnityEngine.Object.Destroy(source.gameObject);
                }
            }
        }

        private int FindLastIdle()
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i] != null && !_sources[i].isPlaying)
                    return i;
            }
            return -1;
        }
    }
}
