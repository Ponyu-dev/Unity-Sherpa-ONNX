using NUnit.Framework;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class AsrResultTests
    {
        [Test]
        public void Ctor_SetsAllProperties()
        {
            string[] tokens = { "hello", "world" };
            float[] timestamps = { 0.1f, 0.5f };
            float[] durations = { 0.4f, 0.3f };

            var result = new AsrResult("hello world", tokens, timestamps, durations);

            Assert.AreEqual("hello world", result.Text);
            Assert.AreSame(tokens, result.Tokens);
            Assert.AreSame(timestamps, result.Timestamps);
            Assert.AreSame(durations, result.Durations);
        }

        [Test]
        public void IsValid_NonEmptyText_ReturnsTrue()
        {
            var result = new AsrResult("hello", null, null, null);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void IsValid_EmptyText_ReturnsFalse()
        {
            var result = new AsrResult("", null, null, null);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void IsValid_NullText_ReturnsFalse()
        {
            var result = new AsrResult(null, null, null, null);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void NullableArrays_CanBeNull()
        {
            var result = new AsrResult("text", null, null, null);

            Assert.IsNull(result.Tokens);
            Assert.IsNull(result.Timestamps);
            Assert.IsNull(result.Durations);
            Assert.IsTrue(result.IsValid);
        }
    }
}
