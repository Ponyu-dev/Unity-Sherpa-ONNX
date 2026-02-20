using System;
using System.Collections;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class TtsResultPlayModeTests
    {
        [UnityTest]
        public IEnumerator ToAudioClip_CreatesValidClip()
        {
            var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
            var result = new TtsResult(samples, 22050);

            var clip = result.ToAudioClip("test-clip");
            yield return null;

            Assert.IsNotNull(clip);
            Assert.AreEqual("test-clip", clip.name);
            Assert.AreEqual(4, clip.samples);
            Assert.AreEqual(22050, clip.frequency);
            Assert.AreEqual(1, clip.channels);

            Object.Destroy(clip);
            result.Dispose();
        }

        [UnityTest]
        public IEnumerator ToAudioClip_CopiesSampleData()
        {
            var samples = new float[] { 0.1f, 0.2f, 0.3f };
            var result = new TtsResult(samples, 16000);

            var clip = result.ToAudioClip();
            yield return null;

            var data = new float[3];
            clip.GetData(data, 0);

            Assert.AreEqual(0.1f, data[0], 0.0001f);
            Assert.AreEqual(0.2f, data[1], 0.0001f);
            Assert.AreEqual(0.3f, data[2], 0.0001f);

            Object.Destroy(clip);
            result.Dispose();
        }

        [UnityTest]
        public IEnumerator ToAudioClip_DefaultName()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);

            var clip = result.ToAudioClip();
            yield return null;

            Assert.AreEqual("tts", clip.name);

            Object.Destroy(clip);
            result.Dispose();
        }

        [Test]
        public void ToAudioClip_Invalid_Throws()
        {
            var result = new TtsResult(new float[0], 22050);

            Assert.Throws<InvalidOperationException>(
                () => result.ToAudioClip());

            result.Dispose();
        }

        [Test]
        public void ToAudioClip_Disposed_Throws()
        {
            var result = new TtsResult(new float[] { 0.5f }, 22050);
            result.Dispose();

            Assert.Throws<InvalidOperationException>(
                () => result.ToAudioClip());
        }
    }
}
