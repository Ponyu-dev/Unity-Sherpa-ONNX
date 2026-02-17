using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Tests.Stubs;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class OnlineAsrServiceTests
    {
        private StubOnlineAsrEngine _engine;
        private OnlineAsrService _service;

        [SetUp]
        public void SetUp()
        {
            _engine = new StubOnlineAsrEngine();
            _service = new OnlineAsrService(_engine);
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
        public void LoadProfile_SubscribesEngineEvents()
        {
            var profile = CreateProfile("test");
            OnlineAsrResult received = null;
            _service.PartialResultReady += HandlePartialReceived;

            _service.LoadProfile(profile);

            var pending = new OnlineAsrResult("partial", null, null, false);
            _engine.PendingPartialResult = pending;
            _engine.ProcessAvailableFrames();

            Assert.AreSame(pending, received);

            _service.PartialResultReady -= HandlePartialReceived;

            void HandlePartialReceived(OnlineAsrResult r) { received = r; }
        }

        // ── SwitchProfile ──

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
        public void SwitchProfile_NoSettings_DoesNotLoad()
        {
            LogAssert.Expect(LogType.Error, new Regex("no profiles loaded"));

            _service.SwitchProfile(0);

            Assert.AreEqual(0, _engine.LoadCallCount);
        }

        // ── Session ──

        [Test]
        public void StartSession_WhenReady_CallsEngine()
        {
            var profile = CreateProfile("test");
            _service.LoadProfile(profile);

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
            var profile = CreateProfile("test");
            _service.LoadProfile(profile);
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

        [Test]
        public void ResetStream_DelegatesToEngine()
        {
            _service.ResetStream();

            Assert.AreEqual(1, _engine.ResetStreamCallCount);
        }

        // ── Event forwarding ──

        [Test]
        public void PartialResultReady_ForwardedFromEngine()
        {
            OnlineAsrResult received = null;
            _service.PartialResultReady += HandleReceived;
            _service.LoadProfile(CreateProfile("test"));

            var expected = new OnlineAsrResult("partial", null, null, false);
            _engine.PendingPartialResult = expected;
            _service.ProcessAvailableFrames();

            Assert.AreSame(expected, received);

            _service.PartialResultReady -= HandleReceived;

            void HandleReceived(OnlineAsrResult r) { received = r; }
        }

        [Test]
        public void FinalResultReady_ForwardedFromEngine()
        {
            OnlineAsrResult received = null;
            _service.FinalResultReady += HandleReceived;
            _service.LoadProfile(CreateProfile("test"));

            var expected = new OnlineAsrResult("final", null, null, true);
            _engine.PendingFinalResult = expected;
            _service.ProcessAvailableFrames();

            Assert.AreSame(expected, received);

            _service.FinalResultReady -= HandleReceived;

            void HandleReceived(OnlineAsrResult r) { received = r; }
        }

        [Test]
        public void EndpointDetected_ForwardedFromEngine()
        {
            bool detected = false;
            _service.EndpointDetected += HandleDetected;
            _service.LoadProfile(CreateProfile("test"));

            _engine.PendingEndpoint = true;
            _service.ProcessAvailableFrames();

            Assert.IsTrue(detected);

            _service.EndpointDetected -= HandleDetected;

            void HandleDetected() { detected = true; }
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
        public void Dispose_UnsubscribesEvents()
        {
            OnlineAsrResult received = null;
            _service.PartialResultReady += HandleReceived;
            _service.LoadProfile(CreateProfile("test"));

            _service.Dispose();

            // Old engine events should not reach service subscribers.
            var pending = new OnlineAsrResult("late", null, null, false);
            _engine.PendingPartialResult = pending;
            _engine.ProcessAvailableFrames();

            Assert.IsNull(received);

            _service.PartialResultReady -= HandleReceived;

            void HandleReceived(OnlineAsrResult r) { received = r; }
        }

        // ── Helpers ──

        private static OnlineAsrProfile CreateProfile(string name)
        {
            return new OnlineAsrProfile { profileName = name };
        }

        private void SetSettingsWithProfiles(
            params OnlineAsrProfile[] profiles)
        {
            var settings = new OnlineAsrSettingsData
            {
                profiles = new List<OnlineAsrProfile>(profiles)
            };
            _service.SetSettings(settings);
        }
    }
}
