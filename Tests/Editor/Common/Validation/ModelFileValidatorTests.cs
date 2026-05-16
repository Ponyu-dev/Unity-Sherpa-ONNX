using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Validation;

namespace PonyuDev.SherpaOnnx.Tests.Common.Validation
{
    [TestFixture]
    internal sealed class ModelFileValidatorTests
    {
        private string _tempDir;
        private bool _previousRuntimeEnabled;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "sherpa_int8_test_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);

            // Suppress Debug.Log* — the validator emits a warning that
            // would otherwise surface in the test runner output.
            _previousRuntimeEnabled = SherpaOnnxLog.RuntimeEnabled;
            SherpaOnnxLog.RuntimeEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            SherpaOnnxLog.RuntimeEnabled = _previousRuntimeEnabled;

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ── LogIfInt8 — smoke tests, no exceptions ──

        [Test]
        public void LogIfInt8_NoOnnxFiles_DoesNotThrow()
        {
            File.WriteAllText(Path.Combine(_tempDir, "tokens.txt"), "");

            Assert.DoesNotThrow(() => ModelFileValidator.LogIfInt8(_tempDir, "ASR"));
        }

        [Test]
        public void LogIfInt8_Int8FilePresent_DoesNotThrow()
        {
            File.WriteAllText(Path.Combine(_tempDir, "encoder.int8.onnx"), "");

            Assert.DoesNotThrow(() => ModelFileValidator.LogIfInt8(_tempDir, "ASR"));
        }

        [Test]
        public void LogIfInt8_MissingDirectory_DoesNotThrow()
        {
            string missing = Path.Combine(_tempDir, "does-not-exist");

            Assert.DoesNotThrow(() => ModelFileValidator.LogIfInt8(missing, "ASR"));
        }

        [Test]
        public void LogIfInt8_NullDirectory_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ModelFileValidator.LogIfInt8(null, "ASR"));
        }

        // ── ContainsInt8Marker ──

        [TestCase("model.int8.onnx", true)]
        [TestCase("model-int8.onnx", true)]
        [TestCase("model_int8.onnx", true)]
        [TestCase("Model.INT8.onnx", true)]
        [TestCase("model.onnx", false)]
        [TestCase("intent8.onnx", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void ContainsInt8Marker_VariousNames(string fileName, bool expected)
        {
            Assert.AreEqual(expected, ModelFileValidator.ContainsInt8Marker(fileName));
        }
    }
}
