using System.IO;

namespace PonyuDev.SherpaOnnx.Common.IO
{
    /// <summary>
    /// Shared file-system utilities used across install pipeline, caches,
    /// and temp directory management.
    /// </summary>
    public static class FileSystemHelper
    {
        /// <summary>
        /// Deletes a directory recursively, silently ignoring any errors.
        /// Safe to call with a non-existent path.
        /// </summary>
        public static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Ensures the directory at <paramref name="path"/> exists and is empty.
        /// Deletes it first if present, then recreates.
        /// </summary>
        public static void EnsureCreatedEmpty(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);

            Directory.CreateDirectory(path);
        }
    }
}
