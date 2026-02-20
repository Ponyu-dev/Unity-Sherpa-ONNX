using System.Collections;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Tests.Stubs;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;
using UnityEngine.TestTools;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class TtsServiceExtensionsTests
    {
        private StubTtsServicePlayMode _stub;
        private GameObject _go;
        private AudioSource _source;

        [SetUp]
        public void SetUp()
        {
            _stub = new StubTtsServicePlayMode();
            _go = new GameObject("TestSource");
            _source = _go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.Destroy(_go);
        }

        // ── GenerateAndPlay (simple, no pool) ──

        [UnityTest]
        public IEnumerator GenerateAndPlay_ReturnsResultAndPlays()
        {
            var result = _stub.GenerateAndPlay("hello", _source);
            yield return null;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(_source.isPlaying);

            result.Dispose();
        }

        [UnityTest]
        public IEnumerator GenerateAndPlay_NullService_ReturnsNull()
        {
            ITtsService nullService = null;
            var result = nullService.GenerateAndPlay("hello", _source);
            yield return null;

            Assert.IsNull(result);
        }

        [UnityTest]
        public IEnumerator GenerateAndPlay_NotReady_ReturnsNull()
        {
            _stub.IsReady = false;
            var result = _stub.GenerateAndPlay("hello", _source);
            yield return null;

            Assert.IsNull(result);
        }

        [UnityTest]
        public IEnumerator GenerateAndPlay_EmptyText_ReturnsNull()
        {
            var result = _stub.GenerateAndPlay("", _source);
            yield return null;

            Assert.IsNull(result);
        }

        [UnityTest]
        public IEnumerator GenerateAndPlay_NullText_ReturnsNull()
        {
            var result = _stub.GenerateAndPlay(null, _source);
            yield return null;

            Assert.IsNull(result);
        }

        [UnityTest]
        public IEnumerator GenerateAndPlay_NullSource_ReturnsNull()
        {
            var result = _stub.GenerateAndPlay("hello", (AudioSource)null);
            yield return null;

            Assert.IsNull(result);
        }

        // ── GenerateAndPlayAsync (simple, no pool) ──

        [UnityTest]
        public IEnumerator GenerateAndPlayAsync_ReturnsResultAndPlays()
        {
            TtsResult result = null;
            var task = _stub.GenerateAndPlayAsync("hello", _source);

            while (!task.IsCompleted)
                yield return null;

            result = task.Result;

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(_source.isPlaying);

            result.Dispose();
        }

        [UnityTest]
        public IEnumerator GenerateAndPlayAsync_NotReady_ReturnsNull()
        {
            _stub.IsReady = false;
            var task = _stub.GenerateAndPlayAsync("hello", _source);

            while (!task.IsCompleted)
                yield return null;

            Assert.IsNull(task.Result);
        }

        // ── GenerateAndPlay with invalid result ──

        [UnityTest]
        public IEnumerator GenerateAndPlay_InvalidResult_DoesNotPlay()
        {
            _stub.ResultFactory = _ =>
                new TtsResult(new float[0], 22050);

            var result = _stub.GenerateAndPlay("hello", _source);
            yield return null;

            // Result is returned but playback should not start.
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsFalse(_source.isPlaying);
        }
    }
}
