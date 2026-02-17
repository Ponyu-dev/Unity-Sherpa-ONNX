using System;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common.Audio;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class AudioResamplerTests
    {
        // ── Resample ──

        [Test]
        public void Resample_SameRate_ReturnsSameReference()
        {
            float[] source = { 0.1f, 0.2f, 0.3f };

            float[] result = AudioResampler.Resample(source, 16000, 16000);

            Assert.AreSame(source, result);
        }

        [Test]
        public void Resample_NullInput_ReturnsEmptyArray()
        {
            float[] result = AudioResampler.Resample(null, 16000, 8000);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Resample_EmptyInput_ReturnsEmptyArray()
        {
            float[] result = AudioResampler.Resample(
                Array.Empty<float>(), 16000, 8000);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void Resample_Downsample_HalvesLength()
        {
            float[] source = CreateConstantSignal(16000, 0.5f);

            float[] result = AudioResampler.Resample(source, 16000, 8000);

            Assert.AreEqual(8000, result.Length);
        }

        [Test]
        public void Resample_Upsample_DoublesLength()
        {
            float[] source = CreateConstantSignal(8000, 0.5f);

            float[] result = AudioResampler.Resample(source, 8000, 16000);

            Assert.AreEqual(16000, result.Length);
        }

        [Test]
        public void Resample_Downsample_PreservesConstantSignal()
        {
            float[] source = CreateConstantSignal(16000, 0.5f);

            float[] result = AudioResampler.Resample(source, 16000, 8000);

            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(0.5f, result[i], 0.001f,
                    $"Sample {i} deviated from constant signal");
            }
        }

        // ── ResampleMono ──

        [Test]
        public void ResampleMono_SingleChannel_DelegatesToResample()
        {
            float[] source = CreateConstantSignal(16000, 0.5f);

            float[] result = AudioResampler.ResampleMono(
                source, 1, 16000, 8000);

            Assert.AreEqual(8000, result.Length);
        }

        [Test]
        public void ResampleMono_StereoInput_DownmixesToMono()
        {
            // Stereo: L=1.0, R=0.0 → mono average = 0.5
            float[] stereo = new float[200];
            for (int i = 0; i < stereo.Length; i += 2)
            {
                stereo[i] = 1.0f;     // L
                stereo[i + 1] = 0.0f; // R
            }

            float[] result = AudioResampler.ResampleMono(
                stereo, 2, 16000, 16000);

            Assert.AreEqual(100, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.AreEqual(0.5f, result[i], 0.001f,
                    $"Mono sample {i} should be average of L and R");
            }
        }

        [Test]
        public void ResampleMono_NullInput_ReturnsEmptyArray()
        {
            float[] result = AudioResampler.ResampleMono(
                null, 2, 16000, 8000);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void ResampleMono_StereoAndResample_CorrectLength()
        {
            // 96000 stereo samples at 48kHz = 1 second of audio
            float[] stereo = CreateConstantSignal(96000, 0.5f);

            float[] result = AudioResampler.ResampleMono(
                stereo, 2, 48000, 16000);

            // 96000 / 2 channels = 48000 mono, then 48000 → 16000
            Assert.AreEqual(16000, result.Length);
        }

        // ── Helpers ──

        private static float[] CreateConstantSignal(int length, float value)
        {
            float[] signal = new float[length];
            for (int i = 0; i < length; i++)
                signal[i] = value;
            return signal;
        }
    }
}
