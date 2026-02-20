using NUnit.Framework;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class AsrProfileTests
    {
        [Test]
        public void DefaultValues_AreCorrect()
        {
            var profile = new AsrProfile();

            Assert.AreEqual("New Profile", profile.profileName);
            Assert.AreEqual(AsrModelType.Whisper, profile.modelType);
            Assert.AreEqual(1, profile.numThreads);
            Assert.AreEqual("cpu", profile.provider);
            Assert.AreEqual(16000, profile.sampleRate);
            Assert.AreEqual(80, profile.featureDim);
            Assert.AreEqual("greedy_search", profile.decodingMethod);
            Assert.AreEqual(4, profile.maxActivePaths);
        }

        [Test]
        public void IProfileData_ProfileName_GetSetWork()
        {
            IProfileData profile = new AsrProfile();

            profile.ProfileName = "test-profile";

            Assert.AreEqual("test-profile", profile.ProfileName);
            Assert.AreEqual("test-profile", ((AsrProfile)profile).profileName);
        }
    }
}
