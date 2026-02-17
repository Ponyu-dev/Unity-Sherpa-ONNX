using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Tests.Stubs;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class AsrServiceTests
    {
        private StubAsrEngine _engine;
        private AsrService _service;

        [SetUp]
        public void SetUp()
        {
            _engine = new StubAsrEngine();
            _service = new AsrService(_engine);
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
            var profile = CreateProfile("test");

            _service.LoadProfile(profile);

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

        // ── LoadProfile ──

        [Test]
        public void LoadProfile_ValidProfile_CallsEngineLoad()
        {
            var profile = CreateProfile("test");

            _service.LoadProfile(profile);

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
            var profile = CreateProfile("my-model");

            _service.LoadProfile(profile);

            Assert.AreSame(profile, _engine.LastProfile);
            Assert.IsNotNull(_engine.LastModelDir);
            Assert.IsTrue(
                _engine.LastModelDir.Contains("my-model"),
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
            var profile = CreateProfile("only");
            SetSettingsWithProfiles(profile);
            LogAssert.Expect(LogType.Error, new Regex("out of range"));

            _service.SwitchProfile(5);

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        [Test]
        public void SwitchProfile_NegativeIndex_DoesNotLoad()
        {
            var profile = CreateProfile("only");
            SetSettingsWithProfiles(profile);
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
            var profileA = CreateProfile("alpha");
            var profileB = CreateProfile("beta");
            SetSettingsWithProfiles(profileA, profileB);

            _service.SwitchProfile("beta");

            Assert.AreSame(profileB, _engine.LastProfile);
        }

        [Test]
        public void SwitchProfile_ByName_NotFound_DoesNotLoad()
        {
            var profile = CreateProfile("alpha");
            SetSettingsWithProfiles(profile);
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

        // ── Recognition ──

        [Test]
        public void Recognize_WhenReady_ReturnsResult()
        {
            var profile = CreateProfile("test");
            _service.LoadProfile(profile);

            var expected = new AsrResult("hello", null, null, null);
            _engine.ResultToReturn = expected;

            AsrResult result = _service.Recognize(
                new float[] { 0.1f, 0.2f }, 16000);

            Assert.AreSame(expected, result);
        }

        [Test]
        public void Recognize_WhenReady_DelegatesToEngine()
        {
            var profile = CreateProfile("test");
            _service.LoadProfile(profile);
            _engine.ResultToReturn = new AsrResult("ok", null, null, null);

            _service.Recognize(new float[] { 0.1f }, 16000);

            Assert.AreEqual(1, _engine.RecognizeCallCount);
        }

        [Test]
        public void Recognize_WhenNotReady_ReturnsNull()
        {
            _engine.IsLoaded = false;
            LogAssert.Expect(LogType.Error, new Regex("not initialized"));

            AsrResult result = _service.Recognize(
                new float[] { 0.1f }, 16000);

            Assert.IsNull(result);
        }

        // ── EnginePoolSize ──

        [Test]
        public void EnginePoolSize_Get_DelegatesToEngine()
        {
            _engine.PoolSize = 3;

            Assert.AreEqual(3, _service.EnginePoolSize);
        }

        [Test]
        public void EnginePoolSize_Set_CallsResize()
        {
            _service.EnginePoolSize = 4;

            Assert.AreEqual(1, _engine.ResizeCallCount);
            Assert.AreEqual(4, _engine.PoolSize);
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
            var profile = CreateProfile("test");
            _service.LoadProfile(profile);

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

        private static AsrProfile CreateProfile(string name)
        {
            return new AsrProfile { profileName = name };
        }

        private void SetSettingsWithProfiles(params AsrProfile[] profiles)
        {
            var settings = new AsrSettingsData
            {
                profiles = new List<AsrProfile>(profiles)
            };
            _service.SetSettings(settings);
        }
    }
}
