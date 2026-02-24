using System;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class ArchiveNameParserTests
    {
        // ── GetFileName ──

        [Test]
        public void GetFileName_TarBz2_ReturnsFileName()
        {
            string result = ArchiveNameParser.GetFileName(
                "https://github.com/k2-fsa/sherpa-onnx/releases/download/v1.0/vits-zh.tar.bz2");

            Assert.AreEqual("vits-zh.tar.bz2", result);
        }

        [Test]
        public void GetFileName_UrlWithQueryParams_StripsQuery()
        {
            string result = ArchiveNameParser.GetFileName(
                "https://huggingface.co/repo/resolve/main/model.tar.gz?download=true");

            Assert.AreEqual("model.tar.gz", result);
        }

        [Test]
        public void GetFileName_UrlWithFragment_StripsFragment()
        {
            string result = ArchiveNameParser.GetFileName(
                "https://example.com/files/model.zip#section");

            Assert.AreEqual("model.zip", result);
        }

        [Test]
        public void GetFileName_EmptyUrl_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => ArchiveNameParser.GetFileName(""));
        }

        [Test]
        public void GetFileName_NullUrl_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => ArchiveNameParser.GetFileName(null));
        }

        // ── GetArchiveName ──

        [Test]
        public void GetArchiveName_TarBz2_StripsExtension()
        {
            string result = ArchiveNameParser.GetArchiveName(
                "https://example.com/sherpa-onnx-streaming-zipformer.tar.bz2");

            Assert.AreEqual("sherpa-onnx-streaming-zipformer", result);
        }

        [Test]
        public void GetArchiveName_TarGz_StripsExtension()
        {
            string result = ArchiveNameParser.GetArchiveName(
                "https://example.com/vits-en.tar.gz");

            Assert.AreEqual("vits-en", result);
        }

        [Test]
        public void GetArchiveName_Tgz_StripsExtension()
        {
            string result = ArchiveNameParser.GetArchiveName(
                "https://example.com/kokoro-model.tgz");

            Assert.AreEqual("kokoro-model", result);
        }

        [Test]
        public void GetArchiveName_Zip_StripsExtension()
        {
            string result = ArchiveNameParser.GetArchiveName(
                "https://example.com/silero-vad.zip");

            Assert.AreEqual("silero-vad", result);
        }

        [Test]
        public void GetArchiveName_NoKnownExtension_ReturnsFullName()
        {
            string result = ArchiveNameParser.GetArchiveName(
                "https://example.com/model.onnx");

            Assert.AreEqual("model.onnx", result);
        }

        [Test]
        public void GetArchiveName_UrlWithQueryParams_StripsQueryAndExtension()
        {
            string result = ArchiveNameParser.GetArchiveName(
                "https://huggingface.co/repo/resolve/main/vits-zh.tar.gz?download=true");

            Assert.AreEqual("vits-zh", result);
        }
    }
}
