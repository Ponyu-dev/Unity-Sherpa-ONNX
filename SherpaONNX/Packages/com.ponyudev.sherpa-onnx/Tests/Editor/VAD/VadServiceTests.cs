using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PonyuDev.SherpaOnnx.Vad;
using PonyuDev.SherpaOnnx.Vad.Data;
using PonyuDev.SherpaOnnx.Tests.Stubs;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class VadServiceTests
    {
        private StubVadEngine _engine;
        private VadService _service;

        [SetUp]
        public void SetUp()
        {
            _engine = new StubVadEngine();
            _service = new VadService(_engine);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        // ── Lifecycle ──

        [Test]
        public void IsReady_BeforeLoad_ReturnsFalse()
        {
            Assert.IsFalse(_service.IsReady);
        }

        [Test]
        public void IsReady_AfterLoad_ReturnsTrue()
        {
            _service.LoadProfile(CreateProfile("test"));

            Assert.IsTrue(_service.IsReady);
        }

        [Test]
        public void ActiveProfile_BeforeLoad_ReturnsNull()
        {
            Assert.IsNull(_service.ActiveProfile);
        }

        [Test]
        public void Settings_BeforeSetSettings_ReturnsNull()
        {
            Assert.IsNull(_service.Settings);
        }

        [Test]
        public void WindowSize_DelegatesToEngine()
        {
            _engine.WindowSize = 256;

            Assert.AreEqual(256, _service.WindowSize);
        }

        // ── LoadProfile ──

        [Test]
        public void LoadProfile_ValidProfile_CallsEngineLoad()
        {
            _service.LoadProfile(CreateProfile("test"));

            Assert.AreEqual(1, _engine.LoadCallCount);
        }

        [Test]
        public void LoadProfile_ValidProfile_SetsActiveProfile()
        {
            var profile = CreateProfile("test");

            _service.LoadProfile(profile);

            Assert.AreSame(profile, _service.ActiveProfile);
        }

        [Test]
        public void LoadProfile_NullProfile_DoesNotCallEngine()
        {
            LogAssert.Expect(LogType.Error, new Regex("profile is null"));

            _service.LoadProfile(null);

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        [Test]
        public void LoadProfile_PassesProfileToEngine()
        {
            var profile = CreateProfile("my-vad");

            _service.LoadProfile(profile);

            Assert.AreSame(profile, _engine.LastProfile);
            Assert.IsNotNull(_engine.LastModelDir);
            Assert.IsTrue(
                _engine.LastModelDir.Contains("my-vad"),
                "Model dir should contain profile name");
        }

        // ── SwitchProfile by index ──

        [Test]
        public void SwitchProfile_ValidIndex_LoadsProfile()
        {
            var profileA = CreateProfile("A");
            var profileB = CreateProfile("B");
            SetSettingsWithProfiles(profileA, profileB);

            _service.SwitchProfile(1);

            Assert.AreSame(profileB, _engine.LastProfile);
        }

        [Test]
        public void SwitchProfile_OutOfRange_DoesNotLoad()
        {
            SetSettingsWithProfiles(CreateProfile("only"));
            LogAssert.Expect(LogType.Error, new Regex("out of range"));

            _service.SwitchProfile(5);

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        [Test]
        public void SwitchProfile_NegativeIndex_DoesNotLoad()
        {
            SetSettingsWithProfiles(CreateProfile("only"));
            LogAssert.Expect(LogType.Error, new Regex("out of range"));

            _service.SwitchProfile(-1);

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        [Test]
        public void SwitchProfile_NoSettings_DoesNotLoad()
        {
            LogAssert.Expect(LogType.Error, new Regex("no profiles loaded"));

            _service.SwitchProfile(0);

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        // ── SwitchProfile by name ──

        [Test]
        public void SwitchProfile_ByName_Valid_LoadsProfile()
        {
            var profileA = CreateProfile("silero");
            var profileB = CreateProfile("ten-vad");
            SetSettingsWithProfiles(profileA, profileB);

            _service.SwitchProfile("ten-vad");

            Assert.AreSame(profileB, _engine.LastProfile);
        }

        [Test]
        public void SwitchProfile_ByName_NotFound_DoesNotLoad()
        {
            SetSettingsWithProfiles(CreateProfile("silero"));
            LogAssert.Expect(LogType.Error, new Regex("not found"));

            _service.SwitchProfile("nonexistent");

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        [Test]
        public void SwitchProfile_ByName_NoSettings_DoesNotLoad()
        {
            LogAssert.Expect(LogType.Error, new Regex("no profiles loaded"));

            _service.SwitchProfile("any");

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        // ── AcceptWaveform ──

        [Test]
        public void AcceptWaveform_WhenReady_DelegatesToEngine()
        {
            _service.LoadProfile(CreateProfile("test"));

            _service.AcceptWaveform(new float[512]);

            Assert.AreEqual(1, _engine.AcceptWaveformCallCount);
        }

        [Test]
        public void AcceptWaveform_WhenNotReady_DoesNotCallEngine()
        {
            LogAssert.Expect(LogType.Error, new Regex("not initialized"));

            _service.AcceptWaveform(new float[512]);

            Assert.AreEqual(0, _engine.AcceptWaveformCallCount);
        }

        // ── IsSpeechDetected ──

        [Test]
        public void IsSpeechDetected_DelegatesToEngine()
        {
            _engine.SpeechDetected = true;

            Assert.IsTrue(_service.IsSpeechDetected());
        }

        [Test]
        public void IsSpeechDetected_WhenNoEngine_ReturnsFalse()
        {
            _service.Dispose();

            var freshService = new VadService();
            Assert.IsFalse(freshService.IsSpeechDetected());
            freshService.Dispose();
        }

        // ── DrainSegments ──

        [Test]
        public void DrainSegments_WhenReady_ReturnsEngineSegments()
        {
            _service.LoadProfile(CreateProfile("test"));
            var segment = CreateSegment(100, 16000);
            _engine.SegmentsToReturn.Add(segment);

            var result = _service.DrainSegments();

            Assert.AreEqual(1, result.Count);
            Assert.AreSame(segment, result[0]);
        }

        [Test]
        public void DrainSegments_WhenNotReady_ReturnsEmptyList()
        {
            LogAssert.Expect(LogType.Error, new Regex("not initialized"));

            var result = _service.DrainSegments();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        // ── Events ──

        [Test]
        public void OnSegment_FiredOnDrain()
        {
            _service.LoadProfile(CreateProfile("test"));
            var segment = CreateSegment(100, 16000);
            _engine.SegmentsToReturn.Add(segment);

            VadSegment received = null;
            _service.OnSegment += s => received = s;

            _service.DrainSegments();

            Assert.AreSame(segment, received);
        }

        [Test]
        public void OnSpeechStart_FiredOnTransition()
        {
            _service.LoadProfile(CreateProfile("test"));

            bool fired = false;
            _service.OnSpeechStart += () => fired = true;

            _engine.SpeechDetected = true;
            _service.AcceptWaveform(new float[512]);

            Assert.IsTrue(fired);
        }

        [Test]
        public void OnSpeechEnd_FiredOnTransition()
        {
            _service.LoadProfile(CreateProfile("test"));

            // First: start speech
            _engine.SpeechDetected = true;
            _service.AcceptWaveform(new float[512]);

            bool fired = false;
            _service.OnSpeechEnd += () => fired = true;

            // Then: end speech
            _engine.SpeechDetected = false;
            _service.AcceptWaveform(new float[512]);

            Assert.IsTrue(fired);
        }

        // ── Flush & Reset ──

        [Test]
        public void Flush_DelegatesToEngine()
        {
            _service.LoadProfile(CreateProfile("test"));

            _service.Flush();

            Assert.AreEqual(1, _engine.FlushCallCount);
        }

        [Test]
        public void Reset_DelegatesToEngine()
        {
            _service.LoadProfile(CreateProfile("test"));

            _service.Reset();

            Assert.AreEqual(1, _engine.ResetCallCount);
        }

        // ── Dispose ──

        [Test]
        public void Dispose_DisposesEngine()
        {
            _service.Dispose();

            Assert.IsTrue(_engine.Disposed);
        }

        [Test]
        public void Dispose_ClearsActiveProfile()
        {
            _service.LoadProfile(CreateProfile("test"));

            _service.Dispose();

            Assert.IsNull(_service.ActiveProfile);
        }

        [Test]
        public void Dispose_ClearsSettings()
        {
            SetSettingsWithProfiles(CreateProfile("test"));

            _service.Dispose();

            Assert.IsNull(_service.Settings);
        }

        // ── Helpers ──

        private static VadProfile CreateProfile(string name)
        {
            return new VadProfile { profileName = name };
        }

        private static VadSegment CreateSegment(int numSamples, int sampleRate)
        {
            return new VadSegment(0, new float[numSamples], sampleRate);
        }

        private void SetSettingsWithProfiles(params VadProfile[] profiles)
        {
            var settings = new VadSettingsData
            {
                profiles = new List<VadProfile>(profiles)
            };
            _service.SetSettings(settings);
        }
    }
}
