using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class CustomKeywordsValidatorTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ckv_test_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        [Test]
        public void EmptyText_ReturnsNoWarnings()
        {
            var warnings = CustomKeywordsValidator.Validate("");

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void WhitespaceOnly_ReturnsNoWarnings()
        {
            var warnings = CustomKeywordsValidator.Validate("   \n  ");

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidTokens_ReturnsNoWarnings()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o");

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidTokens_WithTag_ReturnsNoWarnings()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o @HELLO");

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidTokens_WithBoostAndThreshold_ReturnsNoWarnings()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o @HI :1.5 #0.3");

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void MultipleValidLines_ReturnsNoWarnings()
        {
            string text = "h e l l o @HELLO :1.5\ns h e r p a @SHERPA #0.25";

            var warnings = CustomKeywordsValidator.Validate(text);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void EmptyLinesSkipped_ReturnsNoWarnings()
        {
            string text = "h e l l o\n\n\ns h e r p a";

            var warnings = CustomKeywordsValidator.Validate(text);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void NoTokens_OnlyTag_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("@TAG");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("no tokens", warnings[0]);
        }

        [Test]
        public void InvalidBoost_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o :abc");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("invalid boost", warnings[0]);
        }

        [Test]
        public void InvalidThreshold_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o #xyz");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("invalid threshold", warnings[0]);
        }

        [Test]
        public void EmptyBoost_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o :");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("empty boost", warnings[0]);
        }

        [Test]
        public void EmptyThreshold_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o #");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("empty threshold", warnings[0]);
        }

        [Test]
        public void MultipleTags_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o @TAG1 @TAG2");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("multiple @TAG", warnings[0]);
        }

        [Test]
        public void EmptyTag_ReturnsWarning()
        {
            var warnings = CustomKeywordsValidator.Validate("h e l l o @");

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("empty @TAG", warnings[0]);
        }

        // ── Vocabulary validation ──

        [Test]
        public void Validate_WithVocabulary_ValidTokens_NoExtraWarnings()
        {
            string tokensPath = WriteTokensFile("HH 0\nAH0 1\nL 2\nOW1 3\n");

            var warnings = CustomKeywordsValidator.Validate("HH AH0 L OW1 @HELLO", tokensPath);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void Validate_WithVocabulary_InvalidTokens_AddsWarnings()
        {
            string tokensPath = WriteTokensFile("HH 0\nAH0 1\n");

            var warnings = CustomKeywordsValidator.Validate("HH AH0 BADTOKEN", tokensPath);

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("BADTOKEN", warnings[0]);
            StringAssert.Contains("not found", warnings[0]);
        }

        [Test]
        public void Validate_WithVocabulary_NullTokensPath_FormatOnly()
        {
            var warnings = CustomKeywordsValidator.Validate("HH AH0 ANYTHING", null);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void Validate_WithVocabulary_EmptyTokensPath_FormatOnly()
        {
            var warnings = CustomKeywordsValidator.Validate("HH AH0 ANYTHING", "");

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void Validate_WithVocabulary_FormatAndVocabularyErrors()
        {
            string tokensPath = WriteTokensFile("HH 0\nAH0 1\n");

            // Line 1: format OK but has bad token. Line 2: format error (no tokens).
            var warnings = CustomKeywordsValidator.Validate("HH BAD\n@ONLY_TAG", tokensPath);

            // Should have: "no tokens" from format + "BAD not found" from vocabulary.
            Assert.GreaterOrEqual(warnings.Count, 2);
        }

        // ── Helpers ──

        private string WriteTokensFile(string content)
        {
            string path = Path.Combine(_tempDir, "tokens.txt");
            File.WriteAllText(path, content);
            return path;
        }
    }
}
