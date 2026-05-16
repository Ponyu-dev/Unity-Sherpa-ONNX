using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.Common;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class ModelFileServiceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "sherpa_test_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ── IsProfileMissing ──

        [Test]
        public void IsProfileMissing_NullName_ReturnsFalse()
        {
            Assert.IsFalse(ModelFileService.IsProfileMissing(null, _ => _tempDir));
        }

        [Test]
        public void IsProfileMissing_EmptyName_ReturnsFalse()
        {
            Assert.IsFalse(ModelFileService.IsProfileMissing("", _ => _tempDir));
        }

        [Test]
        public void IsProfileMissing_ExistingDir_ReturnsFalse()
        {
            string name = "test-model";
            string modelDir = Path.Combine(_tempDir, name);
            Directory.CreateDirectory(modelDir);

            Assert.IsFalse(ModelFileService.IsProfileMissing(name, n => Path.Combine(_tempDir, n)));
        }

        [Test]
        public void IsProfileMissing_MissingDir_ReturnsTrue()
        {
            Assert.IsTrue(ModelFileService.IsProfileMissing("nonexistent-model", n => Path.Combine(_tempDir, n)));
        }
    }
}
