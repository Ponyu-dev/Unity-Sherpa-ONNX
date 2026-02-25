using System.IO;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
    /// <summary>
    /// Checks the first bytes of a file to verify it is a known archive
    /// format before attempting extraction. Catches cases where the server
    /// returned an HTML page or a non-archive file.
    /// </summary>
    public static class ArchiveMagicValidator
    {
        private const int HeaderSize = 4;

        /// <summary>
        /// Returns null when the file looks like a valid archive,
        /// or an error message describing the problem.
        /// </summary>
        public static string Validate(string filePath)
        {
            if (!File.Exists(filePath))
                return $"Downloaded file not found: '{Path.GetFileName(filePath)}'.";

            var info = new FileInfo(filePath);
            if (info.Length == 0)
                return "Downloaded file is empty (0 bytes).";

            byte[] header = ReadHeader(filePath, HeaderSize);

            if (IsGzip(header) || IsBzip2(header) || IsZip(header))
                return null;

            if (LooksLikeHtml(header))
            {
                return "Downloaded file appears to be an HTML page, " +
                       "not an archive. Check that the URL points to " +
                       "a direct download link.";
            }

            return $"Downloaded file does not look like a supported " +
                   $"archive (unexpected header bytes). " +
                   $"Supported formats: .tar.bz2, .tar.gz, .tgz, .zip.";
        }

        // gzip: 0x1F 0x8B
        internal static bool IsGzip(byte[] h)
        {
            return h.Length >= 2 && h[0] == 0x1F && h[1] == 0x8B;
        }

        // bzip2: 'B' 'Z'
        internal static bool IsBzip2(byte[] h)
        {
            return h.Length >= 2 && h[0] == 0x42 && h[1] == 0x5A;
        }

        // zip/nupkg: 'P' 'K' (0x50 0x4B)
        internal static bool IsZip(byte[] h)
        {
            return h.Length >= 2 && h[0] == 0x50 && h[1] == 0x4B;
        }

        // HTML typically starts with '<'
        internal static bool LooksLikeHtml(byte[] h)
        {
            return h.Length >= 1 && h[0] == 0x3C;
        }

        private static byte[] ReadHeader(string path, int count)
        {
            using var stream = File.OpenRead(path);
            byte[] buffer = new byte[count];
            int bytesRead = stream.Read(buffer, 0, count);

            if (bytesRead < count)
            {
                byte[] trimmed = new byte[bytesRead];
                System.Array.Copy(buffer, trimmed, bytesRead);
                return trimmed;
            }

            return buffer;
        }
    }
}
