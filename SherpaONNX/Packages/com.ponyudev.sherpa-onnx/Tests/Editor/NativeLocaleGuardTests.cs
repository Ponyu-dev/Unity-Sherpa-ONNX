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
