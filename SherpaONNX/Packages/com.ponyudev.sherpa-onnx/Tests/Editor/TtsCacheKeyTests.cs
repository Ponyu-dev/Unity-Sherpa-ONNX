using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tts.Cache;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class TtsCacheKeyTests
    {
        // ── Constructor ──

        [Test]
        public void Ctor_NullText_BecomesEmpty()
        {
            var key = new TtsCacheKey(null, 1f, 0);
            Assert.AreEqual("", key.Text);
        }

        [Test]
        public void Ctor_SpeedRoundedToThreeDecimals()
        {
            var key = new TtsCacheKey("hi", 1.23456f, 0);
            Assert.AreEqual(1.235f, key.Speed, 0.0001f);
        }

        [Test]
        public void Ctor_PreservesFields()
        {
            var key = new TtsCacheKey("hello", 1.5f, 42);

            Assert.AreEqual("hello", key.Text);
            Assert.AreEqual(1.5f, key.Speed, 0.0001f);
            Assert.AreEqual(42, key.SpeakerId);
        }

        // ── Equality ──

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            var a = new TtsCacheKey("test", 1.0f, 0);
            var b = new TtsCacheKey("test", 1.0f, 0);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals((object)b));
        }

        [Test]
        public void Equals_DifferentText_ReturnsFalse()
        {
            var a = new TtsCacheKey("hello", 1.0f, 0);
            var b = new TtsCacheKey("world", 1.0f, 0);

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_DifferentSpeakerId_ReturnsFalse()
        {
            var a = new TtsCacheKey("test", 1.0f, 0);
            var b = new TtsCacheKey("test", 1.0f, 1);

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_SpeedWithinEpsilon_ReturnsTrue()
        {
            // 0.0004 difference < 0.0005 epsilon
            var a = new TtsCacheKey("test", 1.000f, 0);
            var b = new TtsCacheKey("test", 1.0004f, 0);

            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_SpeedBeyondEpsilon_ReturnsFalse()
        {
            // Speed values that round to different 3-decimal values
            var a = new TtsCacheKey("test", 1.000f, 0);
            var b = new TtsCacheKey("test", 1.001f, 0);

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_BoxedObject_NonKey_ReturnsFalse()
        {
            var key = new TtsCacheKey("test", 1.0f, 0);
            Assert.IsFalse(key.Equals("not a key"));
        }

        [Test]
        public void Equals_BoxedNull_ReturnsFalse()
        {
            var key = new TtsCacheKey("test", 1.0f, 0);
            Assert.IsFalse(key.Equals(null));
        }

        // ── GetHashCode ──

        [Test]
        public void GetHashCode_SameValues_SameHash()
        {
            var a = new TtsCacheKey("test", 1.0f, 0);
            var b = new TtsCacheKey("test", 1.0f, 0);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentValues_LikelyDifferentHash()
        {
            var a = new TtsCacheKey("hello", 1.0f, 0);
            var b = new TtsCacheKey("world", 2.0f, 1);

            // Hash collisions are possible but extremely unlikely here.
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
        }

        // ── ToString ──

        [Test]
        public void ToString_FormatsCorrectly()
        {
            var key = new TtsCacheKey("hello world", 1.5f, 3);
            Assert.AreEqual("[3|1.500] hello world", key.ToString());
        }

        // ── Dictionary usage ──

        [Test]
        public void DictionaryLookup_WorksCorrectly()
        {
            var dict = new System.Collections.Generic.Dictionary<TtsCacheKey, int>();
            var key = new TtsCacheKey("test", 1.0f, 0);
            dict[key] = 42;

            var lookup = new TtsCacheKey("test", 1.0f, 0);
            Assert.IsTrue(dict.ContainsKey(lookup));
            Assert.AreEqual(42, dict[lookup]);
        }
    }
}
