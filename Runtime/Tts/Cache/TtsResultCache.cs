using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tts.Cache
{
    /// <summary>
    /// Thread-safe LRU cache for TtsResult memoization.
    /// Stores cloned float[] samples — callers get independent copies.
    /// </summary>
    public sealed class TtsResultCache : IDisposable
    {
        private int _maxSize;
        private readonly object _lock = new();

        private readonly Dictionary<TtsCacheKey, LinkedListNode<CacheEntry>> _map;
        private readonly LinkedList<CacheEntry> _order;

        public TtsResultCache(int maxSize)
        {
            _maxSize = Math.Max(1, maxSize);
            _map = new Dictionary<TtsCacheKey, LinkedListNode<CacheEntry>>(_maxSize);
            _order = new LinkedList<CacheEntry>();
        }

        /// <summary>Number of cached entries.</summary>
        public int Count
        {
            get { lock (_lock) return _map.Count; }
        }

        /// <summary>Maximum cache capacity. Evicts LRU entries on shrink.</summary>
        public int MaxSize
        {
            get { lock (_lock) return _maxSize; }
            set
            {
                lock (_lock)
                {
                    _maxSize = Math.Max(1, value);
                    while (_map.Count > _maxSize)
                        Evict();
                }
            }
        }

        /// <summary>
        /// Returns a cloned TtsResult if the key exists; null otherwise.
        /// Promotes the entry to the head (most recently used).
        /// </summary>
        public TtsResult TryGet(TtsCacheKey key)
        {
            lock (_lock)
            {
                if (!_map.TryGetValue(key, out var node))
                    return null;

                // Promote to head.
                _order.Remove(node);
                _order.AddFirst(node);

                return CloneEntry(node.Value);
            }
        }

        /// <summary>
        /// Stores a clone of the result. Evicts the LRU entry if full.
        /// </summary>
        public void Add(TtsCacheKey key, TtsResult result)
        {
            if (result == null || !result.IsValid)
                return;

            lock (_lock)
            {
                // Update existing entry.
                if (_map.TryGetValue(key, out var existing))
                {
                    existing.Value = CreateEntry(key, result);
                    _order.Remove(existing);
                    _order.AddFirst(existing);
                    return;
                }

                // Evict if at capacity.
                while (_map.Count >= _maxSize)
                    Evict();

                var entry = CreateEntry(key, result);
                var node = _order.AddFirst(entry);
                _map[key] = node;
            }
        }

        /// <summary>Removes all cached entries.</summary>
        public void Clear()
        {
            lock (_lock)
            {
                _map.Clear();
                _order.Clear();

                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] TtsResultCache cleared.");
            }
        }

        public void Dispose()
        {
            Clear();
        }

        // ── Private ──

        private void Evict()
        {
            var tail = _order.Last;
            if (tail == null)
                return;

            _map.Remove(tail.Value.Key);
            _order.RemoveLast();
        }

        private static CacheEntry CreateEntry(TtsCacheKey key, TtsResult result)
        {
            var copy = new float[result.Samples.Length];
            Array.Copy(result.Samples, copy, copy.Length);
            return new CacheEntry(key, copy, result.SampleRate);
        }

        private static TtsResult CloneEntry(CacheEntry entry)
        {
            var copy = new float[entry.Samples.Length];
            Array.Copy(entry.Samples, copy, copy.Length);
            return new TtsResult(copy, entry.SampleRate);
        }

        // ── Inner type ──

        private struct CacheEntry
        {
            public TtsCacheKey Key;
            public float[] Samples;
            public int SampleRate;

            public CacheEntry(TtsCacheKey key, float[] samples, int sampleRate)
            {
                Key = key;
                Samples = samples;
                SampleRate = sampleRate;
            }
        }
    }
}
