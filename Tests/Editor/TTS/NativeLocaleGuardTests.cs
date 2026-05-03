using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Tests
{
    [TestFixture]
    public sealed class NativeLocaleGuardTests
    {
        private const int LcNumeric = 1;

        [DllImport("c", EntryPoint = "setlocale", CharSet = CharSet.Ansi)]
        private static extern IntPtr setlocale(int category, string locale);

        private static string GetNumericLocale()
        {
            IntPtr ptr = setlocale(LcNumeric, null);
            return ptr != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(ptr)
                : null;
        }

        // The three locale-verifying tests below call libc's setlocale via
        // [DllImport("c")] which only resolves on Linux / macOS / Android.
        // On Windows the C runtime is msvcrt/ucrtbase, not "c", so the import
        // throws DllNotFoundException. Skip on Windows — `NativeLocaleGuard`
        // itself swallows that exception and becomes a safe no-op there,
        // which is the correct behavior (Windows doesn't need the guard).
#if !UNITY_EDITOR_WIN
        [Test]
        public void Begin_SetsLocaleToC()
        {
            using (NativeLocaleGuard.Begin())
            {
                var locale = GetNumericLocale();
                Assert.AreEqual("C", locale);
            }
        }

        [Test]
        public void Dispose_RestoresOriginalLocale()
        {
            var before = GetNumericLocale();

            using (NativeLocaleGuard.Begin())
            {
                // Inside guard — locale is "C".
            }

            var after = GetNumericLocale();
            Assert.AreEqual(before, after);
        }

        [Test]
        public void NestedGuards_RestoreCorrectly()
        {
            var original = GetNumericLocale();

            using (NativeLocaleGuard.Begin())
            {
                Assert.AreEqual("C", GetNumericLocale());

                using (NativeLocaleGuard.Begin())
                {
                    Assert.AreEqual("C", GetNumericLocale());
                }

                // Inner dispose restores "C" (what was set before inner).
                Assert.AreEqual("C", GetNumericLocale());
            }

            Assert.AreEqual(original, GetNumericLocale());
        }
#endif

        [Test]
        public void Begin_ReturnsDisposable()
        {
            var guard = NativeLocaleGuard.Begin();
            Assert.IsNotNull(guard);
            Assert.IsInstanceOf<IDisposable>(guard);
            guard.Dispose();
        }
    }
}
