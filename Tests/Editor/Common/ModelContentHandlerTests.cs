using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.Common.Import;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class ModelContentHandlerTests
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

        // ── FindModelRoot ──

        [Test]
        public void FindModelRoot_FilesAtTop_ReturnsTopDir()
        {
            File.WriteAllBytes(
                Path.Combine(_tempDir, "model.onnx"), new byte[] { 0 });

            string result = ModelContentHandler.FindModelRoot(_tempDir);

            Assert.AreEqual(_tempDir, result);
        }

        [Test]
        public void FindModelRoot_SingleWrapper_UnwrapsOneLevel()
        {
            string inner = Path.Combine(_tempDir, "wrapper");
            Directory.CreateDirectory(inner);
            File.WriteAllBytes(
                Path.Combine(inner, "model.onnx"), new byte[] { 0 });

            string result = ModelContentHandler.FindModelRoot(_tempDir);

            Assert.AreEqual(inner, result);
        }

        [Test]
        public void FindModelRoot_ThreeLevelNesting_UnwrapsAll()
        {
            string level1 = Path.Combine(_tempDir, "a");
            string level2 = Path.Combine(level1, "b");
            string level3 = Path.Combine(level2, "c");
            Directory.CreateDirectory(level3);
            File.WriteAllBytes(
                Path.Combine(level3, "tokens.txt"), new byte[] { 0 });

            string result = ModelContentHandler.FindModelRoot(_tempDir);

            Assert.AreEqual(level3, result);
        }

        [Test]
        public void FindModelRoot_FourLevelNesting_StopsAtThree()
        {
            string level1 = Path.Combine(_tempDir, "a");
            string level2 = Path.Combine(level1, "b");
            string level3 = Path.Combine(level2, "c");
            string level4 = Path.Combine(level3, "d");
            Directory.CreateDirectory(level4);
            File.WriteAllBytes(
                Path.Combine(level4, "model.onnx"), new byte[] { 0 });

            string result = ModelContentHandler.FindModelRoot(_tempDir);

            // Stops at level3 (maxDepth=3 unwraps), not level4.
            Assert.AreEqual(level3, result);
        }

        [Test]
        public void FindModelRoot_MultipleDirsAtTop_ReturnsTopDir()
        {
            Directory.CreateDirectory(Path.Combine(_tempDir, "dir1"));
            Directory.CreateDirectory(Path.Combine(_tempDir, "dir2"));

            string result = ModelContentHandler.FindModelRoot(_tempDir);

            Assert.AreEqual(_tempDir, result);
        }

        [Test]
        public void FindModelRoot_EmptyDir_ReturnsTopDir()
        {
            string result = ModelContentHandler.FindModelRoot(_tempDir);

            Assert.AreEqual(_tempDir, result);
        }
    }
}
