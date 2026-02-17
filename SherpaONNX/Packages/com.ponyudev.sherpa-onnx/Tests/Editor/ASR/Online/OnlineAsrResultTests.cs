using NUnit.Framework;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class OnlineAsrResultTests
    {
        [Test]
        public void Ctor_SetsAllProperties()
        {
            string[] tokens = { "he", "llo" };
            float[] timestamps = { 0.0f, 0.2f };

            var result = new OnlineAsrResult("hello", tokens, timestamps, true);

            Assert.AreEqual("hello", result.Text);
            Assert.AreSame(tokens, result.Tokens);
            Assert.AreSame(timestamps, result.Timestamps);
            Assert.IsTrue(result.IsFinal);
        }

        [Test]
        public void IsFinal_True_WhenSetInCtor()
        {
            var result = new OnlineAsrResult("text", null, null, true);

            Assert.IsTrue(result.IsFinal);
        }

        [Test]
        public void IsFinal_False_WhenSetInCtor()
        {
            var result = new OnlineAsrResult("text", null, null, false);

            Assert.IsFalse(result.IsFinal);
        }

        [Test]
        public void IsValid_NonEmptyText_ReturnsTrue()
        {
            var result = new OnlineAsrResult("hello", null, null, false);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void IsValid_EmptyText_ReturnsFalse()
        {
            var result = new OnlineAsrResult("", null, null, false);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void IsValid_NullText_ReturnsFalse()
        {
            var result = new OnlineAsrResult(null, null, null, false);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void NullableArrays_CanBeNull()
        {
            var result = new OnlineAsrResult("text", null, null, false);

            Assert.IsNull(result.Tokens);
            Assert.IsNull(result.Timestamps);
            Assert.IsTrue(result.IsValid);
        }
    }
}
