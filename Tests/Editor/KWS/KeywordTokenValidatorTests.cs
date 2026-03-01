using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common.Validation;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    internal sealed class KeywordTokenValidatorTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "kws_validator_test_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        // ── LoadVocabulary ──

        [Test]
        public void LoadVocabulary_ParsesTokensFile()
        {
            string path = WriteTokensFile("<blk> 0\n<unk> 1\nAA0 2\nHH 3\nAH0 4\n");

            var vocab = KeywordTokenValidator.LoadVocabulary(path);

            Assert.AreEqual(5, vocab.Count);
            Assert.IsTrue(vocab.Contains("<blk>"));
            Assert.IsTrue(vocab.Contains("<unk>"));
            Assert.IsTrue(vocab.Contains("AA0"));
            Assert.IsTrue(vocab.Contains("HH"));
            Assert.IsTrue(vocab.Contains("AH0"));
        }

        [Test]
        public void LoadVocabulary_EmptyFile_ReturnsEmpty()
        {
            string path = WriteTokensFile("");

            var vocab = KeywordTokenValidator.LoadVocabulary(path);

            Assert.AreEqual(0, vocab.Count);
        }

        [Test]
        public void LoadVocabulary_FileNotFound_ReturnsEmpty()
        {
            var vocab = KeywordTokenValidator.LoadVocabulary("/nonexistent/path/tokens.txt");

            Assert.AreEqual(0, vocab.Count);
        }

        [Test]
        public void LoadVocabulary_NullPath_ReturnsEmpty()
        {
            var vocab = KeywordTokenValidator.LoadVocabulary(null);

            Assert.AreEqual(0, vocab.Count);
        }

        [Test]
        public void LoadVocabulary_SkipsEmptyLines()
        {
            string path = WriteTokensFile("AA0 0\n\n\nHH 1\n");

            var vocab = KeywordTokenValidator.LoadVocabulary(path);

            Assert.AreEqual(2, vocab.Count);
        }

        [Test]
        public void LoadVocabulary_HandlesUnicodeTokens()
        {
            string path = WriteTokensFile("▁HE 0\nǐ 1\nǎo 2\n");

            var vocab = KeywordTokenValidator.LoadVocabulary(path);

            Assert.AreEqual(3, vocab.Count);
            Assert.IsTrue(vocab.Contains("▁HE"));
            Assert.IsTrue(vocab.Contains("ǐ"));
            Assert.IsTrue(vocab.Contains("ǎo"));
        }

        // ── ValidateKeywords ──

        [Test]
        public void ValidateKeywords_AllValid_NoWarnings()
        {
            var vocab = BuildVocabulary("HH", "AH0", "L", "OW1");

            var warnings = KeywordTokenValidator.ValidateKeywords("HH AH0 L OW1", vocab);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidateKeywords_InvalidToken_ReturnsWarning()
        {
            var vocab = BuildVocabulary("HH", "AH0");

            var warnings = KeywordTokenValidator.ValidateKeywords("HH AH0 INVALID", vocab);

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("INVALID", warnings[0]);
            StringAssert.Contains("not found", warnings[0]);
        }

        [Test]
        public void ValidateKeywords_SkipsTagBoostThreshold()
        {
            var vocab = BuildVocabulary("HH", "AH0", "L", "OW1");

            var warnings = KeywordTokenValidator.ValidateKeywords("HH AH0 L OW1 @HELLO :1.5 #0.3", vocab);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidateKeywords_EmptyLines_Skipped()
        {
            var vocab = BuildVocabulary("HH", "AH0");

            var warnings = KeywordTokenValidator.ValidateKeywords("HH AH0\n\n\n", vocab);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidateKeywords_MultipleInvalidTokens()
        {
            var vocab = BuildVocabulary("HH");

            var warnings = KeywordTokenValidator.ValidateKeywords("HH BAD1 BAD2", vocab);

            Assert.AreEqual(2, warnings.Count);
            StringAssert.Contains("BAD1", warnings[0]);
            StringAssert.Contains("BAD2", warnings[1]);
        }

        [Test]
        public void ValidateKeywords_MultipleLines_ValidatesEach()
        {
            var vocab = BuildVocabulary("HH", "AH0", "L", "OW1");

            var warnings = KeywordTokenValidator.ValidateKeywords("HH AH0\nBADTOKEN", vocab);

            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("Line 2", warnings[0]);
            StringAssert.Contains("BADTOKEN", warnings[0]);
        }

        [Test]
        public void ValidateKeywords_EmptyVocabulary_NoWarnings()
        {
            var vocab = new HashSet<string>();

            var warnings = KeywordTokenValidator.ValidateKeywords("HH AH0", vocab);

            Assert.AreEqual(0, warnings.Count);
        }

        [Test]
        public void ValidateKeywords_NullText_NoWarnings()
        {
            var vocab = BuildVocabulary("HH");

            var warnings = KeywordTokenValidator.ValidateKeywords(null, vocab);

            Assert.AreEqual(0, warnings.Count);
        }

        // ── FindInvalidTokensInLine ──

        [Test]
        public void FindInvalidTokensInLine_AllValid_ReturnsEmpty()
        {
            var vocab = BuildVocabulary("n", "ǐ", "h", "ǎo");

            var invalid = KeywordTokenValidator.FindInvalidTokensInLine("n ǐ h ǎo @你好", vocab);

            Assert.AreEqual(0, invalid.Count);
        }

        [Test]
        public void FindInvalidTokensInLine_SkipsMetadata()
        {
            var vocab = BuildVocabulary("HH", "AH0");

            var invalid = KeywordTokenValidator.FindInvalidTokensInLine("HH AH0 @TAG :1.5 #0.3", vocab);

            Assert.AreEqual(0, invalid.Count);
        }

        [Test]
        public void FindInvalidTokensInLine_BpeTokensInPhoneVocab_ReturnsInvalid()
        {
            // Simulates: BPE tokens from gigaspeech model used with zh-en (phone) model.
            var vocab = BuildVocabulary("HH", "AH0", "L", "OW1");

            var invalid = KeywordTokenValidator.FindInvalidTokensInLine("▁HE LL O ▁WORLD", vocab);

            Assert.AreEqual(4, invalid.Count);
            Assert.Contains("▁HE", invalid);
            Assert.Contains("LL", invalid);
            Assert.Contains("O", invalid);
            Assert.Contains("▁WORLD", invalid);
        }

        // ── BlockIfInvalidTokens ──

        [Test]
        public void BlockIfInvalidTokens_ValidTokens_ReturnsFalse()
        {
            string tokensPath = WriteTokensFile("HH 0\nAH0 1\nL 2\nOW1 3\n");

            bool block = KeywordTokenValidator.BlockIfInvalidTokens(tokensPath, "", "HH AH0 L OW1", "KWS");

            Assert.IsFalse(block);
        }

        [Test]
        public void BlockIfInvalidTokens_InvalidTokens_ReturnsTrue()
        {
            string tokensPath = WriteTokensFile("HH 0\nAH0 1\n");

            bool block = KeywordTokenValidator.BlockIfInvalidTokens(tokensPath, "", "▁HE LL O", "KWS");

            Assert.IsTrue(block);
        }

        [Test]
        public void BlockIfInvalidTokens_NoTokensFile_ReturnsFalse()
        {
            bool block = KeywordTokenValidator.BlockIfInvalidTokens("/nonexistent.txt", "", "▁HE LL O", "KWS");

            Assert.IsFalse(block);
        }

        [Test]
        public void BlockIfInvalidTokens_EmptyCustomKeywords_ReturnsFalse()
        {
            string tokensPath = WriteTokensFile("HH 0\n");

            bool block = KeywordTokenValidator.BlockIfInvalidTokens(tokensPath, "", "", "KWS");

            Assert.IsFalse(block);
        }

        [Test]
        public void BlockIfInvalidTokens_ValidatesKeywordsFile()
        {
            string tokensPath = WriteTokensFile("HH 0\nAH0 1\n");
            string kwFile = Path.Combine(_tempDir, "keywords.txt");
            File.WriteAllText(kwFile, "HH AH0 @HELLO\nBADTOKEN @BAD\n");

            bool block = KeywordTokenValidator.BlockIfInvalidTokens(tokensPath, kwFile, "", "KWS");

            Assert.IsTrue(block);
        }

        // ── DetectTokenType ──

        [Test]
        public void DetectTokenType_BpeVocabulary_ReturnsBpe()
        {
            var vocab = BuildVocabulary("<blk>", "<unk>", "▁THE", "▁A", "ED", "▁HE", "LL", "O");

            var result = KeywordTokenValidator.DetectTokenType(vocab);

            Assert.AreEqual(KeywordTokenType.Bpe, result);
        }

        [Test]
        public void DetectTokenType_PpinyinVocabulary_ReturnsPpinyin()
        {
            var vocab = BuildVocabulary("<blk>", "<unk>", "n", "ǐ", "h", "ǎo", "b", "zh");

            var result = KeywordTokenValidator.DetectTokenType(vocab);

            Assert.AreEqual(KeywordTokenType.Ppinyin, result);
        }

        [Test]
        public void DetectTokenType_PhonePpinyinVocabulary_ReturnsPhonePpinyin()
        {
            var vocab = BuildVocabulary("<blk>", "<unk>", "AA0", "AH1", "HH", "n", "ǐ", "ǎo");

            var result = KeywordTokenValidator.DetectTokenType(vocab);

            Assert.AreEqual(KeywordTokenType.PhonePpinyin, result);
        }

        [Test]
        public void DetectTokenType_EmptyVocabulary_ReturnsUnknown()
        {
            var result = KeywordTokenValidator.DetectTokenType(new HashSet<string>());

            Assert.AreEqual(KeywordTokenType.Unknown, result);
        }

        [Test]
        public void DetectTokenType_NullVocabulary_ReturnsUnknown()
        {
            var result = KeywordTokenValidator.DetectTokenType(null);

            Assert.AreEqual(KeywordTokenType.Unknown, result);
        }

        // ── Helpers ──

        private string WriteTokensFile(string content)
        {
            string path = Path.Combine(_tempDir, "tokens.txt");
            File.WriteAllText(path, content);
            return path;
        }

        private static HashSet<string> BuildVocabulary(params string[] tokens)
        {
            return new HashSet<string>(tokens);
        }
    }
}
