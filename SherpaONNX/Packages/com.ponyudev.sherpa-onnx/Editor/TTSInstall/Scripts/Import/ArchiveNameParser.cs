using System;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Import
{
    /// <summary>
    /// Extracts file name and archive name (without extensions) from a URL.
    /// Handles .tar.bz2, .tar.gz and .zip extensions.
    /// </summary>
    internal static class ArchiveNameParser
    {
        private static readonly string[] CompoundExtensions =
        {
            ".tar.bz2",
            ".tar.gz"
        };

        private static readonly string[] SimpleExtensions =
        {
            ".zip",
            ".tgz"
        };

        /// <summary>
        /// Returns the file name from a URL, e.g. "vits-zh.tar.bz2".
        /// </summary>
        internal static string GetFileName(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL must not be empty.", nameof(url));

            string path = new Uri(url).AbsolutePath;
            int lastSlash = path.LastIndexOf('/');

            return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        }

        /// <summary>
        /// Returns the archive name without extensions, e.g. "vits-zh".
        /// </summary>
        internal static string GetArchiveName(string url)
        {
            string fileName = GetFileName(url);
            string lower = fileName.ToLowerInvariant();

            foreach (string ext in CompoundExtensions)
            {
                if (lower.EndsWith(ext))
                    return fileName.Substring(0, fileName.Length - ext.Length);
            }

            foreach (string ext in SimpleExtensions)
            {
                if (lower.EndsWith(ext))
                    return fileName.Substring(0, fileName.Length - ext.Length);
            }

            return fileName;
        }
    }
}
