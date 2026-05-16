using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common.Extractors;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class ArchiveMagicValidatorTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "sherpa_test_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ── Validate (file-based) ──

        [Test]
        public void Validate_GzipFile_ReturnsNull()
        {
            string path = CreateFile(0x1F, 0x8B, 0x08, 0x00);

            Assert.IsNull(ArchiveMagicValidator.Validate(path));
        }

        [Test]
        public void Validate_Bzip2File_ReturnsNull()
        {
            string path = CreateFile(0x42, 0x5A, 0x68, 0x39);

            Assert.IsNull(ArchiveMagicValidator.Validate(path));
        }

        [Test]
        public void Validate_ZipFile_ReturnsNull()
        {
            string path = CreateFile(0x50, 0x4B, 0x03, 0x04);

            Assert.IsNull(ArchiveMagicValidator.Validate(path));
        }

        [Test]
        public void Validate_HtmlFile_ReturnsHtmlError()
        {
            string path = WriteTempFile("<!DOCTYPE html><html>");

            string result = ArchiveMagicValidator.Validate(path);

            Assert.IsNotNull(result);
            StringAssert.Contains("HTML", result);
        }

        [Test]
        public void Validate_EmptyFile_ReturnsError()
        {
            string path = CreateFile();

            string result = ArchiveMagicValidator.Validate(path);

            Assert.IsNotNull(result);
            StringAssert.Contains("empty", result);
        }

        [Test]
        public void Validate_NonexistentFile_ReturnsError()
        {
            string result = ArchiveMagicValidator.Validate(
                Path.Combine(_tempDir, "missing.tar.gz"));

            Assert.IsNotNull(result);
            StringAssert.Contains("not found", result);
        }

        [Test]
        public void Validate_UnknownBinaryFile_ReturnsError()
        {
            string path = CreateFile(0xFF, 0xFE, 0x00, 0x01);

            string result = ArchiveMagicValidator.Validate(path);

            Assert.IsNotNull(result);
            StringAssert.Contains("Supported formats", result);
        }

        // ── Magic byte helpers ──

        [Test]
        public void IsGzip_CorrectBytes_ReturnsTrue()
        {
            Assert.IsTrue(ArchiveMagicValidator.IsGzip(
                new byte[] { 0x1F, 0x8B }));
        }

        [Test]
        public void IsGzip_WrongBytes_ReturnsFalse()
        {
            Assert.IsFalse(ArchiveMagicValidator.IsGzip(
                new byte[] { 0x50, 0x4B }));
        }

        [Test]
        public void IsBzip2_CorrectBytes_ReturnsTrue()
        {
            Assert.IsTrue(ArchiveMagicValidator.IsBzip2(
                new byte[] { 0x42, 0x5A }));
        }

        [Test]
        public void IsZip_CorrectBytes_ReturnsTrue()
        {
            Assert.IsTrue(ArchiveMagicValidator.IsZip(
                new byte[] { 0x50, 0x4B }));
        }

        [Test]
        public void LooksLikeHtml_AngleBracket_ReturnsTrue()
        {
            Assert.IsTrue(ArchiveMagicValidator.LooksLikeHtml(
                new byte[] { 0x3C }));
        }

        [Test]
        public void LooksLikeHtml_GzipBytes_ReturnsFalse()
        {
            Assert.IsFalse(ArchiveMagicValidator.LooksLikeHtml(
                new byte[] { 0x1F, 0x8B }));
        }

        // ── Helpers ──

        private string CreateFile(params byte[] content)
        {
            string path = Path.Combine(_tempDir, Path.GetRandomFileName());
            File.WriteAllBytes(path, content);
            return path;
        }

        private string WriteTempFile(string text)
        {
            string path = Path.Combine(_tempDir, Path.GetRandomFileName());
            File.WriteAllText(path, text);
            return path;
        }
    }
}
