using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Tests.Common
{
    [TestFixture]
    internal sealed class NativePathSanitizerTests
    {
        // ── HasNonAsciiCharacters ──

        [Test]
        public void HasNonAsciiCharacters_AsciiPath_ReturnsFalse()
        {
            Assert.IsFalse(NativePathSanitizer.HasNonAsciiCharacters("/Users/user/Documents/Unity/Assets"));
        }

        [Test]
        public void HasNonAsciiCharacters_PathWithSpaces_ReturnsFalse()
        {
            Assert.IsFalse(NativePathSanitizer.HasNonAsciiCharacters("/Users/user/My Project/Assets"));
        }

        [Test]
        public void HasNonAsciiCharacters_CyrillicPath_ReturnsTrue()
        {
            Assert.IsTrue(NativePathSanitizer.HasNonAsciiCharacters("/Users/user/Мои Проекты/Assets"));
        }

        [Test]
        public void HasNonAsciiCharacters_JapanesePath_ReturnsTrue()
        {
            Assert.IsTrue(NativePathSanitizer.HasNonAsciiCharacters("/Users/user/プロジェクト/Assets"));
        }

        [Test]
        public void HasNonAsciiCharacters_ChinesePath_ReturnsTrue()
        {
            Assert.IsTrue(NativePathSanitizer.HasNonAsciiCharacters("C:\\用户\\项目\\Assets"));
        }

        [Test]
        public void HasNonAsciiCharacters_WindowsAsciiPath_ReturnsFalse()
        {
            Assert.IsFalse(NativePathSanitizer.HasNonAsciiCharacters("C:\\Users\\user\\Documents\\Unity\\Assets"));
        }

        [Test]
        public void HasNonAsciiCharacters_NullPath_ReturnsFalse()
        {
            Assert.IsFalse(NativePathSanitizer.HasNonAsciiCharacters(null));
        }

        [Test]
        public void HasNonAsciiCharacters_EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(NativePathSanitizer.HasNonAsciiCharacters(""));
        }

        [Test]
        public void HasNonAsciiCharacters_MixedAsciiAndNonAscii_ReturnsTrue()
        {
            Assert.IsTrue(NativePathSanitizer.HasNonAsciiCharacters("/Users/user/Проект-v2/Assets"));
        }

        // ── Sanitize ──

        [Test]
        public void Sanitize_NullPath_ReturnsNull()
        {
            Assert.IsNull(NativePathSanitizer.Sanitize(null));
        }

        [Test]
        public void Sanitize_EmptyPath_ReturnsEmpty()
        {
            Assert.AreEqual("", NativePathSanitizer.Sanitize(""));
        }

        [Test]
        public void Sanitize_AsciiPath_ReturnsUnchanged()
        {
            const string path = "/Users/user/Documents/Unity/Assets/StreamingAssets";
            Assert.AreEqual(path, NativePathSanitizer.Sanitize(path));
        }

        [Test]
        public void Sanitize_PathWithSpaces_ReturnsUnchanged()
        {
            const string path = "/Users/user/My Unity Project/Assets";
            Assert.AreEqual(path, NativePathSanitizer.Sanitize(path));
        }

#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
        [Test]
        public void Sanitize_NonAsciiOnNonWindows_ReturnsUnchanged()
        {
            const string path = "/Users/user/Мои Проекты/Assets";
            Assert.AreEqual(path, NativePathSanitizer.Sanitize(path));
        }
#endif
    }
}
