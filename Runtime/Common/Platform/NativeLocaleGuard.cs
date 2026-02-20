using System;
using System.Runtime.InteropServices;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Forces the C runtime numeric locale to "C" (dot as decimal separator)
    /// before calling into native code, then restores the previous locale.
    ///
    /// Required on Android devices with non-US locales where the system
    /// uses comma as the decimal separator. Native sherpa-onnx internally
    /// formats float values via snprintf using the current C locale,
    /// which breaks validation (e.g. 0.2 becomes "0,2" → parsed as -0.0).
    ///
    /// Usage:
    /// <code>
    /// using (NativeLocaleGuard.Begin())
    /// {
    ///     var tts = new OfflineTts(config);
    /// }
    /// </code>
    /// </summary>
    public static class NativeLocaleGuard
    {
        private const int LC_NUMERIC = 1;
        private const string CLocale = "C";

        /// <summary>
        /// Sets LC_NUMERIC to "C" and returns a disposable that
        /// restores the original locale on dispose.
        /// </summary>
        public static IDisposable Begin()
        {
            return new Guard();
        }

        private sealed class Guard : IDisposable
        {
            private readonly string _previous;
            private bool _disposed;

            public Guard()
            {
                _previous = GetCurrentLocale();
                SetLocale(LC_NUMERIC, CLocale);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (!string.IsNullOrEmpty(_previous))
                    SetLocale(LC_NUMERIC, _previous);
            }
        }

        private static void SetLocale(int category, string locale)
        {
            try
            {
                setlocale(category, locale);
            }
            catch (Exception)
            {
                // Silently ignore if setlocale is not available.
            }
        }

        private static string GetCurrentLocale()
        {
            try
            {
                IntPtr ptr = setlocale(LC_NUMERIC, null);
                return ptr != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi(ptr)
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        [DllImport("c", EntryPoint = "setlocale", CharSet = CharSet.Ansi)]
        private static extern IntPtr setlocale(int category, string locale);
    }
}
