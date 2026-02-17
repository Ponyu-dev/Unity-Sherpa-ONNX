using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class TtsResultCacheTests
    {
        private const int SampleRate = 22050;

        private static TtsResult MakeResult(float value = 0.5f)
        {
            return new TtsResult(new[] { value }, SampleRate);
        }

        private static TtsCacheKey MakeKey(string text = "hello")
        {
            return new TtsCacheKey(text, 1.0f, 0);
        }

        // ── Constructor ──

        [Test]
        public void Ctor_MaxSizeClampedToOne()
        {
            var cache = new TtsResultCache(0);
            Assert.AreEqual(1, cache.MaxSize);
        }

        [Test]
        public void Ctor_NegativeMaxSize_ClampedToOne()
        {
            var cache = new TtsResultCache(-5);
            Assert.AreEqual(1, cache.MaxSize);
        }

        // ── Add & TryGet ──

        [Test]
        public void Add_ThenTryGet_ReturnsCachedResult()
        {
            var cache = new TtsResultCache(4);
            var key = MakeKey();
            var result = MakeResult(0.42f);

            cache.Add(key, result);

            var cached = cache.TryGet(key);
            Assert.IsNotNull(cached);
            Assert.AreEqual(0.42f, cached.Samples[0], 0.0001f);
            Assert.AreEqual(SampleRate, cached.SampleRate);
        }

        [Test]
        public void TryGet_MissingKey_ReturnsNull()
        {
            var cache = new TtsResultCache(4);
            Assert.IsNull(cache.TryGet(MakeKey("missing")));
        }

        [Test]
        public void Add_NullResult_DoesNotStore()
        {
            var cache = new TtsResultCache(4);
            cache.Add(MakeKey(), null);

            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void Add_InvalidResult_DoesNotStore()
        {
            var cache = new TtsResultCache(4);
            var invalid = new TtsResult(new float[0], SampleRate);

            cache.Add(MakeKey(), invalid);
            Assert.AreEqual(0, cache.Count);
        }

        // ── Cloning ──

        [Test]
        public void TryGet_ReturnedSamplesAreClone()
        {
            var cache = new TtsResultCache(4);
            var key = MakeKey();
            cache.Add(key, MakeResult(0.5f));

            var a = cache.TryGet(key);
            var b = cache.TryGet(key);

            Assert.AreNotSame(a.Samples, b.Samples);
        }

        [Test]
        public void Add_StoresClonedSamples()
        {
            var cache = new TtsResultCache(4);
            var key = MakeKey();
            var original = MakeResult(0.5f);

            cache.Add(key, original);

            // Modify original samples — cache must be unaffected.
            original.Samples[0] = 999f;

            var cached = cache.TryGet(key);
            Assert.AreEqual(0.5f, cached.Samples[0], 0.0001f);
        }

        // ── Count ──

        [Test]
        public void Count_TracksEntries()
        {
            var cache = new TtsResultCache(4);
            Assert.AreEqual(0, cache.Count);

            cache.Add(MakeKey("a"), MakeResult());
            Assert.AreEqual(1, cache.Count);

            cache.Add(MakeKey("b"), MakeResult());
            Assert.AreEqual(2, cache.Count);
        }

        // ── LRU eviction ──

        [Test]
        public void Add_EvictsLRU_WhenAtCapacity()
        {
            var cache = new TtsResultCache(2);

            cache.Add(MakeKey("first"), MakeResult(1f));
            cache.Add(MakeKey("second"), MakeResult(2f));
            cache.Add(MakeKey("third"), MakeResult(3f));

            // "first" was LRU — should be evicted.
            Assert.IsNull(cache.TryGet(MakeKey("first")));
            Assert.IsNotNull(cache.TryGet(MakeKey("second")));
            Assert.IsNotNull(cache.TryGet(MakeKey("third")));
            Assert.AreEqual(2, cache.Count);
        }

        [Test]
        public void TryGet_PromotesEntry_PreventsEviction()
        {
            var cache = new TtsResultCache(2);

            cache.Add(MakeKey("a"), MakeResult(1f));
            cache.Add(MakeKey("b"), MakeResult(2f));

            // Access "a" to promote it to MRU.
            cache.TryGet(MakeKey("a"));

            // Now add "c" — should evict "b" (LRU), not "a".
            cache.Add(MakeKey("c"), MakeResult(3f));

            Assert.IsNotNull(cache.TryGet(MakeKey("a")));
            Assert.IsNull(cache.TryGet(MakeKey("b")));
            Assert.IsNotNull(cache.TryGet(MakeKey("c")));
        }

        [Test]
        public void Add_UpdateExisting_PromotesToMRU()
        {
            var cache = new TtsResultCache(2);

            cache.Add(MakeKey("a"), MakeResult(1f));
            cache.Add(MakeKey("b"), MakeResult(2f));

            // Update "a" with new value — should promote to MRU.
            cache.Add(MakeKey("a"), MakeResult(10f));

            // Add "c" — should evict "b", not "a".
            cache.Add(MakeKey("c"), MakeResult(3f));

            var a = cache.TryGet(MakeKey("a"));
            Assert.IsNotNull(a);
            Assert.AreEqual(10f, a.Samples[0], 0.0001f);
            Assert.IsNull(cache.TryGet(MakeKey("b")));
        }

        // ── MaxSize setter ──

        [Test]
        public void MaxSize_Shrink_EvictsLRUEntries()
        {
            var cache = new TtsResultCache(4);

            cache.Add(MakeKey("a"), MakeResult());
            cache.Add(MakeKey("b"), MakeResult());
            cache.Add(MakeKey("c"), MakeResult());
            cache.Add(MakeKey("d"), MakeResult());

            Assert.AreEqual(4, cache.Count);

            cache.MaxSize = 2;

            Assert.AreEqual(2, cache.Count);
            // "a" and "b" were LRU — evicted.
            Assert.IsNull(cache.TryGet(MakeKey("a")));
            Assert.IsNull(cache.TryGet(MakeKey("b")));
            Assert.IsNotNull(cache.TryGet(MakeKey("c")));
            Assert.IsNotNull(cache.TryGet(MakeKey("d")));
        }

        [Test]
        public void MaxSize_SetBelowOne_ClampedToOne()
        {
            var cache = new TtsResultCache(4);
            cache.MaxSize = -10;
            Assert.AreEqual(1, cache.MaxSize);
        }

        // ── Clear ──

        [Test]
        public void Clear_RemovesAllEntries()
        {
            var cache = new TtsResultCache(4);
            cache.Add(MakeKey("a"), MakeResult());
            cache.Add(MakeKey("b"), MakeResult());

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
            Assert.IsNull(cache.TryGet(MakeKey("a")));
        }

        // ── Dispose ──

        [Test]
        public void Dispose_ClearsCache()
        {
            var cache = new TtsResultCache(4);
            cache.Add(MakeKey("a"), MakeResult());

            cache.Dispose();

            Assert.AreEqual(0, cache.Count);
        }

        // ── Capacity one ──

        [Test]
        public void CapacityOne_ReplacesOnAdd()
        {
            var cache = new TtsResultCache(1);

            cache.Add(MakeKey("a"), MakeResult(1f));
            cache.Add(MakeKey("b"), MakeResult(2f));

            Assert.AreEqual(1, cache.Count);
            Assert.IsNull(cache.TryGet(MakeKey("a")));

            var b = cache.TryGet(MakeKey("b"));
            Assert.IsNotNull(b);
            Assert.AreEqual(2f, b.Samples[0], 0.0001f);
        }
    }
}
