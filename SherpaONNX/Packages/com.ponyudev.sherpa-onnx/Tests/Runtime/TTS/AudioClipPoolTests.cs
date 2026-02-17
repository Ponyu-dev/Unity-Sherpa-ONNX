using System.Collections;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tts.Cache;
using UnityEngine;
using UnityEngine.TestTools;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class AudioClipPoolTests
    {
        private AudioClipPool _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new AudioClipPool(4);
        }

        [TearDown]
        public void TearDown()
        {
            _pool?.Dispose();
        }

        // ── Rent ──

        [UnityTest]
        public IEnumerator Rent_CreatesClipWithCorrectParams()
        {
            var clip = _pool.Rent(1000, 22050);
            yield return null;

            Assert.IsNotNull(clip);
            Assert.AreEqual(1000, clip.samples);
            Assert.AreEqual(22050, clip.frequency);
            Assert.AreEqual(1, clip.channels);

            Object.Destroy(clip);
        }

        [UnityTest]
        public IEnumerator Rent_InvalidParams_ReturnsNull()
        {
            Assert.IsNull(_pool.Rent(0, 22050));
            Assert.IsNull(_pool.Rent(-1, 22050));
            Assert.IsNull(_pool.Rent(100, 0));
            Assert.IsNull(_pool.Rent(100, -1));
            yield return null;
        }

        // ── Return & Reuse ──

        [UnityTest]
        public IEnumerator Return_ThenRent_ReusesMatchingClip()
        {
            var clip = _pool.Rent(1000, 22050);
            _pool.Return(clip);

            Assert.AreEqual(1, _pool.AvailableCount);

            var reused = _pool.Rent(1000, 22050);
            yield return null;

            Assert.AreSame(clip, reused);
            Assert.AreEqual(0, _pool.AvailableCount);

            Object.Destroy(reused);
        }

        [UnityTest]
        public IEnumerator Rent_NoMatchingClip_CreatesNew()
        {
            var clip = _pool.Rent(1000, 22050);
            _pool.Return(clip);

            // Different sample count — should create new.
            var newClip = _pool.Rent(2000, 22050);
            yield return null;

            Assert.IsNotNull(newClip);
            Assert.AreNotSame(clip, newClip);
            Assert.AreEqual(2000, newClip.samples);

            Object.Destroy(newClip);
            // Original was destroyed during Rent (no match).
            yield return null;
        }

        // ── AvailableCount ──

        [UnityTest]
        public IEnumerator AvailableCount_TracksPoolState()
        {
            Assert.AreEqual(0, _pool.AvailableCount);

            var c1 = _pool.Rent(100, 22050);
            var c2 = _pool.Rent(100, 22050);
            _pool.Return(c1);
            _pool.Return(c2);
            yield return null;

            Assert.AreEqual(2, _pool.AvailableCount);

            Object.Destroy(c1);
            Object.Destroy(c2);
        }

        // ── Return when full ──

        [UnityTest]
        public IEnumerator Return_PoolFull_DestroysClip()
        {
            var pool = new AudioClipPool(1);
            var c1 = pool.Rent(100, 22050);
            pool.Return(c1);

            var c2 = pool.Rent(100, 22050);
            pool.Return(c2);

            // Pool is full (1) — this clip should be destroyed.
            var extra = AudioClip.Create("extra", 100, 1, 22050, false);
            pool.Return(extra);
            yield return null;

            Assert.AreEqual(1, pool.AvailableCount);

            pool.Dispose();
            yield return null;
        }

        // ── MaxSize ──

        [UnityTest]
        public IEnumerator MaxSize_Shrink_TrimsExcessClips()
        {
            var c1 = _pool.Rent(100, 22050);
            var c2 = _pool.Rent(100, 22050);
            var c3 = _pool.Rent(100, 22050);
            _pool.Return(c1);
            _pool.Return(c2);
            _pool.Return(c3);
            yield return null;

            Assert.AreEqual(3, _pool.AvailableCount);

            _pool.MaxSize = 1;
            yield return null;

            Assert.AreEqual(1, _pool.AvailableCount);
        }

        // ── Clear ──

        [UnityTest]
        public IEnumerator Clear_DestroysAllPooledClips()
        {
            var c1 = _pool.Rent(100, 22050);
            var c2 = _pool.Rent(100, 22050);
            _pool.Return(c1);
            _pool.Return(c2);

            _pool.Clear();
            yield return null;

            Assert.AreEqual(0, _pool.AvailableCount);
        }

        // ── Dispose ──

        [UnityTest]
        public IEnumerator Dispose_ClearsPool()
        {
            var clip = _pool.Rent(100, 22050);
            _pool.Return(clip);

            _pool.Dispose();
            yield return null;

            Assert.AreEqual(0, _pool.AvailableCount);
            _pool = null; // Prevent double dispose.
        }

        // ── Return null ──

        [Test]
        public void Return_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _pool.Return(null));
        }
    }
}
