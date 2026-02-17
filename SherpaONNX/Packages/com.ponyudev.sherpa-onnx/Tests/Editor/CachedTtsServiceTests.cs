using System.Threading.Tasks;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tests.Stubs;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class CachedTtsServiceTests
    {
        private StubTtsService _stub;
        private CachedTtsService _cached;

        private static TtsCacheSettings DefaultSettings()
        {
            return new TtsCacheSettings
            {
                resultCacheEnabled = true,
                resultCacheSize = 4,
                audioClipEnabled = false,
                audioSourceEnabled = false
            };
        }

        [SetUp]
        public void SetUp()
        {
            _stub = new StubTtsService();
            _cached = new CachedTtsService(
                _stub, DefaultSettings(), sourceParent: null);
        }

        [TearDown]
        public void TearDown()
        {
            _cached?.Dispose();
        }

        // ── ITtsService delegation ──

        [Test]
        public void IsReady_DelegatesToInner()
        {
            _stub.IsReady = true;
            Assert.IsTrue(_cached.IsReady);

            _stub.IsReady = false;
            Assert.IsFalse(_cached.IsReady);
        }

        [Test]
        public void ActiveProfile_DelegatesToInner()
        {
            Assert.AreEqual("stub", _cached.ActiveProfile.profileName);
        }

        // ── Cached generation (sync) ──

        [Test]
        public void Generate_CachesResult()
        {
            var r1 = _cached.Generate("hello");
            var r2 = _cached.Generate("hello");

            // Second call should use cache, not the inner service.
            Assert.AreEqual(1, _stub.GenerateCallCount);
            Assert.IsNotNull(r2);
            Assert.IsTrue(r2.IsValid);
        }

        [Test]
        public void Generate_DifferentText_NoCacheHit()
        {
            _cached.Generate("hello");
            _cached.Generate("world");

            Assert.AreEqual(2, _stub.GenerateCallCount);
        }

        [Test]
        public void Generate_CacheDisabled_AlwaysCallsInner()
        {
            _cached.ResultCacheEnabled = false;

            _cached.Generate("hello");
            _cached.Generate("hello");

            Assert.AreEqual(2, _stub.GenerateCallCount);
        }

        [Test]
        public void Generate_WithSpeedAndSpeaker_CachesCorrectly()
        {
            _cached.Generate("hi", 1.0f, 0);
            _cached.Generate("hi", 1.0f, 0);

            Assert.AreEqual(1, _stub.GenerateCallCount);
        }

        [Test]
        public void Generate_DifferentSpeed_NoCacheHit()
        {
            _cached.Generate("hi", 1.0f, 0);
            _cached.Generate("hi", 2.0f, 0);

            Assert.AreEqual(2, _stub.GenerateCallCount);
        }

        // ── Cached generation (async) ──

        [Test]
        public async Task GenerateAsync_CachesResult()
        {
            await _cached.GenerateAsync("hello");
            await _cached.GenerateAsync("hello");

            Assert.AreEqual(1, _stub.GenerateAsyncCallCount);
        }

        [Test]
        public async Task GenerateAsync_CacheDisabled_AlwaysCallsInner()
        {
            _cached.ResultCacheEnabled = false;

            await _cached.GenerateAsync("hello");
            await _cached.GenerateAsync("hello");

            Assert.AreEqual(2, _stub.GenerateAsyncCallCount);
        }

        // ── Cache returns cloned data ──

        [Test]
        public void Generate_CachedResult_IsClone()
        {
            _cached.Generate("hello");
            var r1 = _cached.Generate("hello");
            var r2 = _cached.Generate("hello");

            // Different array references.
            Assert.AreNotSame(r1.Samples, r2.Samples);
        }

        // ── Profile switch clears cache ──

        [Test]
        public void SwitchProfile_ByIndex_ClearsCache()
        {
            _cached.Generate("hello");
            Assert.AreEqual(1, _cached.ResultCacheCount);

            _cached.SwitchProfile(0);
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }

        [Test]
        public void SwitchProfile_ByName_ClearsCache()
        {
            _cached.Generate("hello");
            _cached.SwitchProfile("other");
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }

        [Test]
        public void LoadProfile_ClearsCache()
        {
            _cached.Generate("hello");
            _cached.LoadProfile(new TtsProfile());
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }

        // ── ITtsCacheControl: enable/disable ──

        [Test]
        public void ResultCacheEnabled_DisableClears()
        {
            _cached.Generate("hello");
            Assert.AreEqual(1, _cached.ResultCacheCount);

            _cached.ResultCacheEnabled = false;
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }

        // ── ITtsCacheControl: sizes ──

        [Test]
        public void ResultCacheMaxSize_CanBeChanged()
        {
            Assert.AreEqual(4, _cached.ResultCacheMaxSize);
            _cached.ResultCacheMaxSize = 2;
            Assert.AreEqual(2, _cached.ResultCacheMaxSize);
        }

        // ── ITtsCacheControl: counts ──

        [Test]
        public void ResultCacheCount_Tracks()
        {
            Assert.AreEqual(0, _cached.ResultCacheCount);
            _cached.Generate("a");
            Assert.AreEqual(1, _cached.ResultCacheCount);
            _cached.Generate("b");
            Assert.AreEqual(2, _cached.ResultCacheCount);
        }

        // ── ITtsCacheControl: clear ──

        [Test]
        public void ClearResultCache_ClearsOnlyResults()
        {
            _cached.Generate("a");
            _cached.Generate("b");
            Assert.AreEqual(2, _cached.ResultCacheCount);

            _cached.ClearResultCache();
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }

        [Test]
        public void ClearAll_ClearsEverything()
        {
            _cached.Generate("a");
            _cached.ClearAll();
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }

        // ── Dispose ──

        [Test]
        public void Dispose_DisposesInner()
        {
            _cached.Dispose();
            Assert.IsTrue(_stub.Disposed);
            _cached = null; // Prevent double dispose in TearDown.
        }

        // ── Not-ready fallback ──

        [Test]
        public void Generate_WhenNotReady_DelegatesToInner()
        {
            _stub.IsReady = false;
            _stub.ResultFactory = _ =>
                new TtsResult(new[] { 0.1f }, 22050);

            var result = _cached.Generate("hello");

            Assert.IsNotNull(result);
            Assert.AreEqual(1, _stub.GenerateCallCount);
            // Should NOT be cached.
            Assert.AreEqual(0, _cached.ResultCacheCount);
        }
    }
}
