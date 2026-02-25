using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;

namespace PonyuDev.SherpaOnnx.Tests.Asr
{
    [TestFixture]
    internal sealed class AsrImportMismatchValidatorTests
    {
        // ── CheckOfflineImport ──

        [Test]
        public void CheckOfflineImport_StreamingModel_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOfflineImport("sherpa-onnx-streaming-zipformer-en-2023-06-26");
            Assert.IsNotNull(result);
            StringAssert.Contains("streaming", result.ToLowerInvariant());
        }

        [Test]
        public void CheckOfflineImport_StreamingParaformer_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOfflineImport("sherpa-onnx-streaming-paraformer-bilingual-zh-en");
            Assert.IsNotNull(result);
        }

        [Test]
        public void CheckOfflineImport_OfflineWhisper_ReturnsNull()
        {
            string result = AsrImportMismatchValidator.CheckOfflineImport("sherpa-onnx-whisper-tiny.en");
            Assert.IsNull(result);
        }

        [Test]
        public void CheckOfflineImport_OfflineTransducer_ReturnsNull()
        {
            string result = AsrImportMismatchValidator.CheckOfflineImport("sherpa-onnx-zipformer-en-2023-04-01");
            Assert.IsNull(result);
        }

        [Test]
        public void CheckOfflineImport_NullName_ReturnsNull()
        {
            Assert.IsNull(AsrImportMismatchValidator.CheckOfflineImport(null));
        }

        [Test]
        public void CheckOfflineImport_EmptyName_ReturnsNull()
        {
            Assert.IsNull(AsrImportMismatchValidator.CheckOfflineImport(""));
        }

        // ── CheckOnlineImport ──

        [Test]
        public void CheckOnlineImport_WhisperModel_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-whisper-tiny.en");
            Assert.IsNotNull(result);
            StringAssert.Contains("whisper", result);
        }

        [Test]
        public void CheckOnlineImport_MoonshineModel_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-moonshine-tiny-en-int8");
            Assert.IsNotNull(result);
            StringAssert.Contains("moonshine", result);
        }

        [Test]
        public void CheckOnlineImport_SenseVoiceModel_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-sense-voice-zh-en");
            Assert.IsNotNull(result);
            StringAssert.Contains("sense-voice", result);
        }

        [Test]
        public void CheckOnlineImport_TdnnModel_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-tdnn-yesno");
            Assert.IsNotNull(result);
            StringAssert.Contains("tdnn", result);
        }

        [Test]
        public void CheckOnlineImport_DolphinModel_ReturnsWarning()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-dolphin-base");
            Assert.IsNotNull(result);
            StringAssert.Contains("dolphin", result);
        }

        [Test]
        public void CheckOnlineImport_StreamingTransducer_ReturnsNull()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-streaming-zipformer-en-2023-06-26");
            Assert.IsNull(result);
        }

        [Test]
        public void CheckOnlineImport_TransducerModel_ReturnsNull()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-zipformer-en-2023-04-01");
            Assert.IsNull(result);
        }

        [Test]
        public void CheckOnlineImport_ParaformerModel_ReturnsNull()
        {
            string result = AsrImportMismatchValidator.CheckOnlineImport("sherpa-onnx-streaming-paraformer-bilingual-zh-en");
            Assert.IsNull(result);
        }

        [Test]
        public void CheckOnlineImport_NullName_ReturnsNull()
        {
            Assert.IsNull(AsrImportMismatchValidator.CheckOnlineImport(null));
        }

        [Test]
        public void CheckOnlineImport_EmptyName_ReturnsNull()
        {
            Assert.IsNull(AsrImportMismatchValidator.CheckOnlineImport(""));
        }
    }
}
