using System.Collections.Generic;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Asr.Offline.Config;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Config;
using PonyuDev.SherpaOnnx.Asr.Online.Data;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class AsrSettingsLoaderTests
    {
        // ── AsrSettingsLoader.GetActiveProfile ──

        [Test]
        public void GetActiveProfile_NullData_ReturnsNull()
        {
            AsrProfile result = AsrSettingsLoader.GetActiveProfile(null);

            Assert.IsNull(result);
        }

        [Test]
        public void GetActiveProfile_EmptyProfiles_ReturnsNull()
        {
            var data = new AsrSettingsData { profiles = new List<AsrProfile>() };

            AsrProfile result = AsrSettingsLoader.GetActiveProfile(data);

            Assert.IsNull(result);
        }

        [Test]
        public void GetActiveProfile_NullProfiles_ReturnsNull()
        {
            var data = new AsrSettingsData { profiles = null };

            AsrProfile result = AsrSettingsLoader.GetActiveProfile(data);

            Assert.IsNull(result);
        }

        [Test]
        public void GetActiveProfile_ValidIndex_ReturnsCorrectProfile()
        {
            var profileA = new AsrProfile { profileName = "A" };
            var profileB = new AsrProfile { profileName = "B" };
            var data = new AsrSettingsData
            {
                activeProfileIndex = 1,
                profiles = new List<AsrProfile> { profileA, profileB }
            };

            AsrProfile result = AsrSettingsLoader.GetActiveProfile(data);

            Assert.AreSame(profileB, result);
        }

        [Test]
        public void GetActiveProfile_NegativeIndex_ClampsToZero()
        {
            var profile = new AsrProfile { profileName = "first" };
            var data = new AsrSettingsData
            {
                activeProfileIndex = -1,
                profiles = new List<AsrProfile> { profile }
            };

            AsrProfile result = AsrSettingsLoader.GetActiveProfile(data);

            Assert.AreSame(profile, result);
        }

        [Test]
        public void GetActiveProfile_IndexBeyondRange_ClampsToLast()
        {
            var profileA = new AsrProfile { profileName = "A" };
            var profileB = new AsrProfile { profileName = "B" };
            var profileC = new AsrProfile { profileName = "C" };
            var data = new AsrSettingsData
            {
                activeProfileIndex = 99,
                profiles = new List<AsrProfile> { profileA, profileB, profileC }
            };

            AsrProfile result = AsrSettingsLoader.GetActiveProfile(data);

            Assert.AreSame(profileC, result);
        }

        // ── OnlineAsrSettingsLoader.GetActiveProfile ──

        [Test]
        public void Online_GetActiveProfile_ValidIndex_ReturnsCorrectProfile()
        {
            var profile = new OnlineAsrProfile { profileName = "online" };
            var data = new OnlineAsrSettingsData
            {
                activeProfileIndex = 0,
                profiles = new List<OnlineAsrProfile> { profile }
            };

            OnlineAsrProfile result =
                OnlineAsrSettingsLoader.GetActiveProfile(data);

            Assert.AreSame(profile, result);
        }

        [Test]
        public void Online_GetActiveProfile_NullData_ReturnsNull()
        {
            OnlineAsrProfile result =
                OnlineAsrSettingsLoader.GetActiveProfile(null);

            Assert.IsNull(result);
        }

        [Test]
        public void Online_GetActiveProfile_EmptyProfiles_ReturnsNull()
        {
            var data = new OnlineAsrSettingsData
            {
                profiles = new List<OnlineAsrProfile>()
            };

            OnlineAsrProfile result =
                OnlineAsrSettingsLoader.GetActiveProfile(data);

            Assert.IsNull(result);
        }
    }
}
