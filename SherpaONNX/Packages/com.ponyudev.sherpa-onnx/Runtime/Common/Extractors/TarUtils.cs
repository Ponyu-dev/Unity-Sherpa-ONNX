using System;
using System.IO;
using System.Text;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
    /// <summary>
    /// Shared tar parsing utilities used by TarGz and TarBz2 extractors.
    /// </summary>
    internal static class TarUtils
    {
        internal const int BlockSize = 512;

        internal static int ReadExact(Stream s, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int r = s.Read(buffer, offset + total, count - total);
                if (r <= 0)
                    return total;
                total += r;
            }
            return total;
        }

        internal static bool IsAllZeroBlock(byte[] block)
        {
            for (int i = 0; i < block.Length; i++)
            {
                if (block[i] != 0)
                    return false;
            }
            return true;
        }

        internal static string NormalizeEntryPath(string name)
        {
            name = name.Replace('\\', '/');
            while (name.StartsWith("/", StringComparison.Ordinal))
                name = name.Substring(1);

            if (name.Contains(".."))
                throw new InvalidDataException("Tar entry contains invalid path: " + name);

            return name;
        }
    }

    /// <summary>
    /// Parsed tar header. Supports standard POSIX and ustar formats.
    /// </summary>
    internal readonly struct TarHeader
    {
        public readonly string Name;
        public readonly long Size;
        public readonly byte TypeFlag;

        public bool IsDirectory =>
            TypeFlag == (byte)'5' || Name.EndsWith("/", StringComparison.Ordinal);

        private TarHeader(string name, long size, byte typeFlag)
        {
            Name = name;
            Size = size;
            TypeFlag = typeFlag;
        }

        public static TarHeader Parse(byte[] header)
        {
            string name = ReadNullTerminatedString(header, 0, 100);
            string sizeOctal = ReadNullTerminatedString(header, 124, 12);
            byte typeFlag = header[156];

            string prefix = ReadNullTerminatedString(header, 345, 155);
            if (!string.IsNullOrEmpty(prefix))
                name = prefix + "/" + name;

            long size = ParseOctalLong(sizeOctal);

            if (typeFlag == 0)
                typeFlag = (byte)'0';

            return new TarHeader(name, size, typeFlag);
        }

        private static string ReadNullTerminatedString(byte[] bytes, int offset, int length)
        {
            int end = offset;
            int max = offset + length;

            while (end < max && bytes[end] != 0)
                end++;

            return Encoding.ASCII.GetString(bytes, offset, end - offset).Trim();
        }

        private static long ParseOctalLong(string octal)
        {
            if (string.IsNullOrEmpty(octal))
                return 0;

            octal = octal.Trim();

            long value = 0;
            for (int i = 0; i < octal.Length; i++)
            {
                char c = octal[i];
                if (c < '0' || c > '7')
                    break;

                value = (value << 3) + (c - '0');
            }
            return value;
        }
    }
}
