using System.Collections.Generic;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Tts.Data;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class ModelVersionRequirementsTests
    {
        // ── Lookup ──

        [Test]
        public void Lookup_UnknownModel_ReturnsNull()
        {
            var map = new Dictionary<TtsModelType, string>();

            string result = ModelVersionRequirements.Lookup(
                TtsModelType.Vits, map);

            Assert.IsNull(result);
        }

        [Test]
        public void Lookup_MappedModel_ReturnsMappedVersion()
        {
            var map = new Dictionary<TtsModelType, string>
            {
                { TtsModelType.Pocket, "1.12.24" }
            };

            string result = ModelVersionRequirements.Lookup(
                TtsModelType.Pocket, map);

            Assert.AreEqual("1.12.24", result);
        }

        // ── Check / IsSupported ──

        [Test]
        public void Check_ExactVersion_ReturnsTrue()
        {
            var map = new Dictionary<TtsModelType, string>
            {
                { TtsModelType.Vits, "1.9.30" }
            };

            bool result = ModelVersionRequirements.Check(
                TtsModelType.Vits, map, "1.9.30");

            Assert.IsTrue(result);
        }

        [Test]
        public void Check_AboveRequired_ReturnsTrue()
        {
            var map = new Dictionary<TtsModelType, string>
            {
                { TtsModelType.Pocket, "1.12.24" }
            };

            bool result = ModelVersionRequirements.Check(
                TtsModelType.Pocket, map, "1.13.0");

            Assert.IsTrue(result);
        }

        [Test]
        public void Check_BelowRequired_ReturnsFalse()
        {
            var map = new Dictionary<TtsModelType, string>
            {
                { TtsModelType.Pocket, "1.12.24" }
            };

            bool result = ModelVersionRequirements.Check(
                TtsModelType.Pocket, map, "1.10.0");

            Assert.IsFalse(result);
        }

        [Test]
        public void Check_EmptyVersion_ReturnsFalse()
        {
            var map = new Dictionary<TtsModelType, string>
            {
                { TtsModelType.Vits, "1.9.30" }
            };

            bool result = ModelVersionRequirements.Check(
                TtsModelType.Vits, map, "");

            Assert.IsFalse(result);
        }

        [Test]
        public void Check_UnknownModel_ReturnsFalse()
        {
            var map = new Dictionary<TtsModelType, string>();

            bool result = ModelVersionRequirements.Check(
                TtsModelType.Vits, map, "1.12.25");

            Assert.IsFalse(result);
        }
    }
}
