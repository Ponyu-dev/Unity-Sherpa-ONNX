using System;

namespace PonyuDev.SherpaOnnx.Editor.Common
{
    /// <summary>
    /// Validates model download URLs before starting the import pipeline.
    /// </summary>
    internal static class UrlValidator
    {
        /// <summary>
        /// Returns null when the URL is valid, or an error message string
        /// describing why the URL is rejected.
        /// </summary>
        internal static string Validate(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "URL must not be empty.";

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return $"Invalid URL format: '{url}'.";

            string scheme = uri.Scheme;
            if (scheme != "http" && scheme != "https")
                return $"Only http and https URLs are supported (got '{scheme}').";

            if (string.IsNullOrEmpty(uri.Host))
                return "URL has no host.";

            return null;
        }
    }
}
