using System;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tts.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class TtsResultTests
    {
        // ── Constructor ──

        [Test]
        public void Ctor_NullSamples_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TtsResult(null, 22050));
        }

        [Test]
        public void Ctor_ValidArgs_SetsProperties()
        {
            var samples = new float[] { 0.1f, 0.2f, 0.3f };
            var result = new TtsResult(samples, 16000);

            Assert.AreSame(samples, result.Samples);
            Assert.AreEqual(16000, result.SampleRate);
        }

        // ── NumSamples ──

        [Test]
        public void NumSamples_ReturnsArrayLength()
        {
            var result = new TtsResult(new float[100], 22050);
            Assert.AreEqual(100, result.NumSamples);
        }

        [Test]
        public void NumSamples_AfterDispose_ReturnsZero()
        {
            var result = new TtsResult(new float[10], 22050);
            result.Dispose();
            Assert.AreEqual(0, result.NumSamples);
        }

        // ── IsValid ──

        [Test]
        public void IsValid_WithSamples_ReturnsTrue()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void IsValid_EmptyArray_ReturnsFalse()
        {
            var result = new TtsResult(new float[0], 22050);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void IsValid_AfterDispose_ReturnsFalse()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);
            result.Dispose();
            Assert.IsFalse(result.IsValid);
        }

        // ── DurationSeconds ──

        [Test]
        public void DurationSeconds_CalculatesCorrectly()
        {
            // 22050 samples at 22050 Hz = 1 second
            var result = new TtsResult(new float[22050], 22050);
            Assert.AreEqual(1f, result.DurationSeconds, 0.001f);
        }

        [Test]
        public void DurationSeconds_ZeroSampleRate_ReturnsZero()
        {
            var result = new TtsResult(new float[100], 0);
            Assert.AreEqual(0f, result.DurationSeconds);
        }

        [Test]
        public void DurationSeconds_HalfSecond()
        {
            // 8000 samples at 16000 Hz = 0.5 seconds
            var result = new TtsResult(new float[8000], 16000);
            Assert.AreEqual(0.5f, result.DurationSeconds, 0.001f);
        }

        // ── Clone ──

        [Test]
        public void Clone_ReturnsCopyWithSameData()
        {
            var original = new TtsResult(
                new float[] { 0.1f, 0.2f, 0.3f }, 22050);
            var clone = original.Clone();

            Assert.IsNotNull(clone);
            Assert.AreEqual(original.SampleRate, clone.SampleRate);
            Assert.AreEqual(original.Samples, clone.Samples);
        }

        [Test]
        public void Clone_ReturnsIndependentArray()
        {
            var samples = new float[] { 0.1f, 0.2f, 0.3f };
            var original = new TtsResult(samples, 22050);
            var clone = original.Clone();

            // Modify original — clone must be unaffected.
            samples[0] = 999f;
            Assert.AreEqual(0.1f, clone.Samples[0], 0.0001f);
        }

        [Test]
        public void Clone_SamplesArrayIsNotSameReference()
        {
            var original = new TtsResult(
                new float[] { 0.1f, 0.2f }, 22050);
            var clone = original.Clone();

            Assert.AreNotSame(original.Samples, clone.Samples);
        }

        [Test]
        public void Clone_Invalid_ReturnsNull()
        {
            var result = new TtsResult(new float[0], 22050);
            Assert.IsNull(result.Clone());
        }

        [Test]
        public void Clone_AfterDispose_ReturnsNull()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);
            result.Dispose();
            Assert.IsNull(result.Clone());
        }

        // ── Dispose ──

        [Test]
        public void Dispose_NullifiesSamples()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);
            result.Dispose();

            Assert.IsNull(result.Samples);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);
            result.Dispose();
            Assert.DoesNotThrow(() => result.Dispose());
        }
    }
}
