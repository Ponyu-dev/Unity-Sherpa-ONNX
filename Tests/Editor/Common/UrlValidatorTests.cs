using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class UrlValidatorTests
    {
        [Test]
        public void Validate_ValidHttpsUrl_ReturnsNull()
        {
            string result = UrlValidator.Validate(
                "https://github.com/k2-fsa/sherpa-onnx/releases/download/v1.0/model.tar.bz2");

            Assert.IsNull(result);
        }

        [Test]
        public void Validate_ValidHttpUrl_ReturnsNull()
        {
            string result = UrlValidator.Validate(
                "http://example.com/model.zip");

            Assert.IsNull(result);
        }

        [Test]
        public void Validate_EmptyString_ReturnsError()
        {
            string result = UrlValidator.Validate("");

            Assert.IsNotNull(result);
            StringAssert.Contains("empty", result);
        }

        [Test]
        public void Validate_NullString_ReturnsError()
        {
            string result = UrlValidator.Validate(null);

            Assert.IsNotNull(result);
        }

        [Test]
        public void Validate_WhitespaceOnly_ReturnsError()
        {
            string result = UrlValidator.Validate("   ");

            Assert.IsNotNull(result);
        }

        [Test]
        public void Validate_NotAUrl_ReturnsError()
        {
            string result = UrlValidator.Validate("not-a-url");

            Assert.IsNotNull(result);
            StringAssert.Contains("Invalid URL", result);
        }

        [Test]
        public void Validate_FtpScheme_ReturnsError()
        {
            string result = UrlValidator.Validate(
                "ftp://files.example.com/model.tar.bz2");

            Assert.IsNotNull(result);
            StringAssert.Contains("http", result);
        }

        [Test]
        public void Validate_FileScheme_ReturnsError()
        {
            string result = UrlValidator.Validate(
                "file:///home/user/model.tar.bz2");

            Assert.IsNotNull(result);
            StringAssert.Contains("http", result);
        }

        [Test]
        public void Validate_UrlWithQueryParams_ReturnsNull()
        {
            string result = UrlValidator.Validate(
                "https://huggingface.co/model.tar.gz?download=true");

            Assert.IsNull(result);
        }
    }
}
