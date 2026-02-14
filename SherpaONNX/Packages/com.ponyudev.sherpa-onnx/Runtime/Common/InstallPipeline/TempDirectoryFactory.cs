using System;
using System.IO;

namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    public interface ITempDirectory : IDisposable
    {
        string Path { get; }
        void Clean();
    }

    public interface ITempDirectoryFactory
    {
        ITempDirectory Create(string baseTempRoot, string folderName);
    }

    public sealed class TempDirectoryFactory : ITempDirectoryFactory
    {
        public ITempDirectory Create(string baseTempRoot, string folderName)
        {
            string dir = System.IO.Path.Combine(baseTempRoot, folderName);
            return new TempDirectory(dir);
        }

        private sealed class TempDirectory : ITempDirectory
        {
            public string Path { get; }

            public TempDirectory(string path)
            {
                Path = path;
                EnsureCreatedEmpty(path);
            }

            public void Clean()
            {
                EnsureCreatedEmpty(Path);
            }

            public void Dispose()
            {
                TryDeleteDirectory(Path);
            }

            private static void EnsureCreatedEmpty(string path)
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);

                Directory.CreateDirectory(path);
            }

            private static void TryDeleteDirectory(string path)
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
        }
    }
}