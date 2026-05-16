using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;

namespace PonyuDev.SherpaOnnx.Tests.LibraryInstall
{
    [TestFixture]
    internal sealed class LibraryInstallStatusTests
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

        [Test]
        public void IsAnyManagedDllPresent_NoFiles_ReturnsFalse()
        {
            bool result = LibraryInstallStatus
                .IsAnyManagedDllPresent(_tempDir, "sherpa-onnx.dll");

            Assert.IsFalse(result);
        }

        [Test]
        public void IsAnyManagedDllPresent_StandardOnly_ReturnsTrue()
        {
            File.WriteAllBytes(
                Path.Combine(_tempDir, "sherpa-onnx.dll"),
                new byte[] { 0 });

            bool result = LibraryInstallStatus
                .IsAnyManagedDllPresent(_tempDir, "sherpa-onnx.dll");

            Assert.IsTrue(result);
        }

        [Test]
        public void IsAnyManagedDllPresent_IosOnly_ReturnsTrue()
        {
            string iosDir = Path.Combine(
                _tempDir,
                ConstantsInstallerPaths.IosManagedDllSubDir);
            Directory.CreateDirectory(iosDir);
            File.WriteAllBytes(
                Path.Combine(iosDir, "sherpa-onnx.dll"),
                new byte[] { 0 });

            bool result = LibraryInstallStatus
                .IsAnyManagedDllPresent(_tempDir, "sherpa-onnx.dll");

            Assert.IsTrue(result);
        }

        [Test]
        public void IsAnyManagedDllPresent_Both_ReturnsTrue()
        {
            File.WriteAllBytes(
                Path.Combine(_tempDir, "sherpa-onnx.dll"),
                new byte[] { 0 });

            string iosDir = Path.Combine(
                _tempDir,
                ConstantsInstallerPaths.IosManagedDllSubDir);
            Directory.CreateDirectory(iosDir);
            File.WriteAllBytes(
                Path.Combine(iosDir, "sherpa-onnx.dll"),
                new byte[] { 0 });

            bool result = LibraryInstallStatus
                .IsAnyManagedDllPresent(_tempDir, "sherpa-onnx.dll");

            Assert.IsTrue(result);
        }
    }
}
