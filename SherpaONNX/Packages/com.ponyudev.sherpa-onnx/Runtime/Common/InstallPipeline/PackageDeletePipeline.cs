using System;
using System.IO;

namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    /// <summary>
    /// Deletes a directory or file at the given path, including its .meta file.
    /// </summary>
    public sealed class PackageDeletePipeline : IPackageDeletePipeline
    {
        public event Action<string> OnStatus;
        public event Action<string> OnError;
        public event Action OnCompleted;

        public void Run(string targetPath)
        {
            try
            {
                OnStatus?.Invoke($"Deleting: {targetPath}");

                DeleteDirectoryWithMeta(targetPath);
                DeleteFileWithMeta(targetPath);

                OnStatus?.Invoke("Deleted.");
                OnCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        private static void DeleteDirectoryWithMeta(string path)
        {
            if (!Directory.Exists(path))
                return;

            Directory.Delete(path, recursive: true);

            string metaPath = path.TrimEnd('/', '\\') + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }

        private static void DeleteFileWithMeta(string path)
        {
            if (!File.Exists(path))
                return;

            File.Delete(path);

            string metaPath = path + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }
    }
}
