using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Tests.Stubs;
using PonyuDev.SherpaOnnx.Vad;
using PonyuDev.SherpaOnnx.Vad.Data;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class VadAsrPipelineTests
    {
        private StubVadService _vad;
        private StubAsrService _asr;
        private VadAsrPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _vad = new StubVadService
            {
                IsReady = true,
                WindowSize = 4,
                ActiveProfile = new VadProfile { sampleRate = 16000 }
            };

            _asr = new StubAsrService { IsReady = true };

            _pipeline = new VadAsrPipeline(_vad, _asr);
        }

        [TearDown]
        public void TearDown()
        {
            _pipeline?.Dispose();
        }

        // ── IsReady ──

        [Test]
        public void IsReady_BothReady_ReturnsTrue()
        {
            Assert.IsTrue(_pipeline.IsReady);
        }

        [Test]
        public void IsReady_VadNotReady_ReturnsFalse()
        {
            _vad.IsReady = false;

            Assert.IsFalse(_pipeline.IsReady);
        }

        [Test]
        public void IsReady_AsrNotReady_ReturnsFalse()
        {
            _asr.IsReady = false;

            Assert.IsFalse(_pipeline.IsReady);
        }

        // ── WindowSize ──

        [Test]
        public void WindowSize_DelegatesToVad()
        {
            _vad.WindowSize = 256;

            Assert.AreEqual(256, _pipeline.WindowSize);
        }

        // ── AcceptSamples — ring buffer ──

        [Test]
        public void AcceptSamples_ExactWindow_SendsOneChunk()
        {
            var samples = new float[] { 1f, 2f, 3f, 4f };

            _pipeline.AcceptSamples(samples);

            Assert.AreEqual(1, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_TwoWindows_SendsTwoChunks()
        {
            var samples = new float[] { 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f };

            _pipeline.AcceptSamples(samples);

            Assert.AreEqual(2, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_LessThanWindow_SendsNothing()
        {
            var samples = new float[] { 1f, 2f };

            _pipeline.AcceptSamples(samples);

            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_AccumulatesAcrossCalls()
        {
            _pipeline.AcceptSamples(new float[] { 1f, 2f });
            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);

            _pipeline.AcceptSamples(new float[] { 3f, 4f });
            Assert.AreEqual(1, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_PartialRemainder_Buffered()
        {
            // window=4, send 5 samples → 1 chunk + 1 buffered
            var samples = new float[] { 1f, 2f, 3f, 4f, 5f };

            _pipeline.AcceptSamples(samples);

            Assert.AreEqual(1, _vad.AcceptWaveformCallCount);

            // Send 3 more → completes second window
            _pipeline.AcceptSamples(new float[] { 6f, 7f, 8f });

            Assert.AreEqual(2, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_NullArray_DoesNothing()
        {
            _pipeline.AcceptSamples(null);

            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_EmptyArray_DoesNothing()
        {
            _pipeline.AcceptSamples(new float[0]);

            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptSamples_WhenNotReady_LogsError()
        {
            _vad.IsReady = false;
            LogAssert.Expect(LogType.Error, new Regex("not ready"));

            _pipeline.AcceptSamples(new float[] { 1f, 2f, 3f, 4f });

            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);
        }

        // ── Event passthrough ──

        [Test]
        public void OnSpeechStart_PassesThrough()
        {
            bool fired = false;
            _pipeline.OnSpeechStart += () => fired = true;

            _vad.SimulateSpeechStart();

            Assert.IsTrue(fired);
        }

        [Test]
        public void OnSpeechEnd_PassesThrough()
        {
            bool fired = false;
            _pipeline.OnSpeechEnd += () => fired = true;

            _vad.SimulateSpeechEnd();

            Assert.IsTrue(fired);
        }

        // ── Segment → ASR recognition ──

        [Test]
        public void OnSegment_CallsAsrRecognize()
        {
            var segmentSamples = new float[] { 0.1f, 0.2f, 0.3f };
            var segment = new VadSegment(0, segmentSamples, 16000);

            _vad.SimulateSegment(segment);

            Assert.AreEqual(1, _asr.RecognizeCallCount);
            Assert.AreSame(segmentSamples, _asr.LastSamples);
            Assert.AreEqual(16000, _asr.LastSampleRate);
        }

        [Test]
        public void OnSegment_ValidResult_FiresOnResult()
        {
            AsrResult received = null;
            _pipeline.OnResult += r => received = r;

            var expected = new AsrResult("hello world", null, null, null);
            _asr.ResultToReturn = expected;

            var segment = new VadSegment(0, new float[] { 0.1f }, 16000);
            _vad.SimulateSegment(segment);

            Assert.AreSame(expected, received);
        }

        [Test]
        public void OnSegment_NullResult_DoesNotFireOnResult()
        {
            bool fired = false;
            _pipeline.OnResult += _ => fired = true;

            _asr.ResultToReturn = null;

            var segment = new VadSegment(0, new float[] { 0.1f }, 16000);
            _vad.SimulateSegment(segment);

            Assert.IsFalse(fired);
        }

        [Test]
        public void OnSegment_EmptyTextResult_DoesNotFireOnResult()
        {
            bool fired = false;
            _pipeline.OnResult += _ => fired = true;

            _asr.ResultToReturn = new AsrResult("", null, null, null);

            var segment = new VadSegment(0, new float[] { 0.1f }, 16000);
            _vad.SimulateSegment(segment);

            Assert.IsFalse(fired);
        }

        [Test]
        public void OnSegment_NullSegment_DoesNotCallAsr()
        {
            _vad.SimulateSegment(null);

            Assert.AreEqual(0, _asr.RecognizeCallCount);
        }

        [Test]
        public void OnSegment_EmptySamples_DoesNotCallAsr()
        {
            var segment = new VadSegment(0, new float[0], 16000);

            _vad.SimulateSegment(segment);

            Assert.AreEqual(0, _asr.RecognizeCallCount);
        }

        // ── Flush ──

        [Test]
        public void Flush_DelegatesToVad()
        {
            _pipeline.Flush();

            Assert.AreEqual(1, _vad.FlushCallCount);
        }

        // ── Reset ──

        [Test]
        public void Reset_DelegatesToVad()
        {
            _pipeline.Reset();

            Assert.AreEqual(1, _vad.ResetCallCount);
        }

        [Test]
        public void Reset_ClearsRingBuffer()
        {
            // Partially fill ring buffer
            _pipeline.AcceptSamples(new float[] { 1f, 2f });
            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);

            _pipeline.Reset();

            // After reset, need full window again
            _pipeline.AcceptSamples(new float[] { 3f, 4f });
            Assert.AreEqual(0, _vad.AcceptWaveformCallCount);
        }

        // ── Dispose ──

        [Test]
        public void Dispose_UnsubscribesFromVadEvents()
        {
            bool speechStartFired = false;
            bool speechEndFired = false;
            AsrResult resultReceived = null;

            _pipeline.OnSpeechStart += () => speechStartFired = true;
            _pipeline.OnSpeechEnd += () => speechEndFired = true;
            _pipeline.OnResult += r => resultReceived = r;

            _pipeline.Dispose();

            _asr.ResultToReturn = new AsrResult("test", null, null, null);

            _vad.SimulateSpeechStart();
            _vad.SimulateSpeechEnd();
            _vad.SimulateSegment(
                new VadSegment(0, new float[] { 0.1f }, 16000));

            Assert.IsFalse(speechStartFired);
            Assert.IsFalse(speechEndFired);
            Assert.IsNull(resultReceived);
        }

        // ── Constructor ──

        [Test]
        public void Constructor_NullVad_Throws()
        {
            Assert.That(
                () => new VadAsrPipeline(null, _asr),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_NullAsr_Throws()
        {
            Assert.That(
                () => new VadAsrPipeline(_vad, null),
                Throws.ArgumentNullException);
        }
    }
}
