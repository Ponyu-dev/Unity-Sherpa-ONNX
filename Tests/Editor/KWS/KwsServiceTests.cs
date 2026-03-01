using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PonyuDev.SherpaOnnx.Kws;
using PonyuDev.SherpaOnnx.Kws.Data;
using PonyuDev.SherpaOnnx.Kws.Engine;
using PonyuDev.SherpaOnnx.Tests.Stubs;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class KwsServiceTests
    {
        private StubKwsEngine _engine;
        private KwsService _service;

        [SetUp]
        public void SetUp()
        {
            _engine = new StubKwsEngine();
            _service = new KwsService(_engine);
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
        public void IsSessionActive_BeforeStart_ReturnsFalse()
        {
            Assert.IsFalse(_service.IsSessionActive);
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
            var profile = CreateProfile("my-kws");

            _service.LoadProfile(profile);

            Assert.AreSame(profile, _engine.LastProfile);
            Assert.IsNotNull(_engine.LastModelDir);
            Assert.IsTrue(_engine.LastModelDir.Contains("my-kws"), "Model dir should contain profile name");
        }

        [Test]
        public void LoadProfile_SubscribesEngineEvents()
        {
            KwsResult received = null;
            _service.OnKeywordDetected += HandleReceived;
            _service.LoadProfile(CreateProfile("test"));

            var expected = new KwsResult("hello");
            _engine.PendingKeywordResult = expected;
            _engine.ProcessAvailableFrames();

            Assert.AreSame(expected, received);

            _service.OnKeywordDetected -= HandleReceived;

            void HandleReceived(KwsResult r) { received = r; }
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
            var profileA = CreateProfile("zipformer");
            var profileB = CreateProfile("transducer");
            SetSettingsWithProfiles(profileA, profileB);

            _service.SwitchProfile("transducer");

            Assert.AreSame(profileB, _engine.LastProfile);
        }

        [Test]
        public void SwitchProfile_ByName_NotFound_DoesNotLoad()
        {
            SetSettingsWithProfiles(CreateProfile("zipformer"));
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

        // ── Session ──

        [Test]
        public void StartSession_WhenReady_CallsEngine()
        {
            _service.LoadProfile(CreateProfile("test"));

            _service.StartSession();

            Assert.AreEqual(1, _engine.StartSessionCallCount);
        }

        [Test]
        public void StartSession_WhenNotReady_DoesNotCallEngine()
        {
            _engine.IsLoaded = false;
            LogAssert.Expect(LogType.Error, new Regex("not initialized"));

            _service.StartSession();

            Assert.AreEqual(0, _engine.StartSessionCallCount);
        }

        [Test]
        public void StopSession_CallsEngine()
        {
            _service.LoadProfile(CreateProfile("test"));
            _service.StartSession();

            _service.StopSession();

            Assert.AreEqual(1, _engine.StopSessionCallCount);
        }

        // ── Audio ──

        [Test]
        public void AcceptSamples_DelegatesToEngine()
        {
            _service.AcceptSamples(new float[] { 0.1f }, 16000);

            Assert.AreEqual(1, _engine.AcceptSamplesCallCount);
        }

        [Test]
        public void ProcessAvailableFrames_DelegatesToEngine()
        {
            _service.ProcessAvailableFrames();

            Assert.AreEqual(1, _engine.ProcessFramesCallCount);
        }

        // ── Event forwarding ──

        [Test]
        public void KeywordDetected_ForwardedFromEngine()
        {
            KwsResult received = null;
            _service.OnKeywordDetected += HandleReceived;
            _service.LoadProfile(CreateProfile("test"));

            var expected = new KwsResult("hey sherpa");
            _engine.PendingKeywordResult = expected;
            _service.ProcessAvailableFrames();

            Assert.AreSame(expected, received);

            _service.OnKeywordDetected -= HandleReceived;

            void HandleReceived(KwsResult r) { received = r; }
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

        [Test]
        public void Dispose_UnsubscribesEvents()
        {
            KwsResult received = null;
            _service.OnKeywordDetected += HandleReceived;
            _service.LoadProfile(CreateProfile("test"));

            _service.Dispose();

            // Old engine events should not reach service subscribers.
            var pending = new KwsResult("late");
            _engine.PendingKeywordResult = pending;
            _engine.ProcessAvailableFrames();

            Assert.IsNull(received);

            _service.OnKeywordDetected -= HandleReceived;

            void HandleReceived(KwsResult r) { received = r; }
        }

        // ── Helpers ──

        private static KwsProfile CreateProfile(string name)
        {
            return new KwsProfile { profileName = name };
        }

        private void SetSettingsWithProfiles(params KwsProfile[] profiles)
        {
            var settings = new KwsSettingsData
            {
                profiles = new List<KwsProfile>(profiles)
            };
            _service.SetSettings(settings);
        }
    }
}
