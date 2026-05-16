using System;
using System.Threading;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common.Extractors;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class ArchiveExtractorTests
    {
        [Test]
        public void ExtractAsync_UnsupportedFormat_ThrowsNotSupported()
        {
            using var extractor = new ArchiveExtractor();

            var ex = Assert.ThrowsAsync<NotSupportedException>(
                () => extractor.ExtractAsync(
                    "model.7z", "/tmp/out", CancellationToken.None));

            StringAssert.Contains("Supported formats:", ex.Message);
        }

        [Test]
        public void ExtractAsync_TgzFormat_DoesNotThrowNotSupported()
        {
            using var extractor = new ArchiveExtractor();

            // .tgz is a supported format, so CreateExtractor should
            // succeed. The subsequent file read will fail with a
            // different exception (not NotSupportedException).
            Assert.ThrowsAsync(
                Is.Not.TypeOf<NotSupportedException>(),
                () => extractor.ExtractAsync(
                    "model.tgz", "/tmp/out", CancellationToken.None));
        }

        [Test]
        public void ExtractAsync_NullPath_ThrowsArgument()
        {
            using var extractor = new ArchiveExtractor();

            Assert.ThrowsAsync<ArgumentException>(
                () => extractor.ExtractAsync(
                    null, "/tmp/out", CancellationToken.None));
        }
    }
}
