using NUnit.Framework;
using PonyuDev.SherpaOnnx.Kws.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class KwsResultTests
    {
        [Test]
        public void Keyword_ReturnsValue()
        {
            var result = new KwsResult("hello");

            Assert.AreEqual("hello", result.Keyword);
        }

        [Test]
        public void IsValid_WhenKeywordNotEmpty_ReturnsTrue()
        {
            var result = new KwsResult("hey sherpa");

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void IsValid_WhenKeywordEmpty_ReturnsFalse()
        {
            var result = new KwsResult("");

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void IsValid_WhenKeywordNull_ReturnsFalse()
        {
            var result = new KwsResult(null);

            Assert.IsFalse(result.IsValid);
        }
    }
}
