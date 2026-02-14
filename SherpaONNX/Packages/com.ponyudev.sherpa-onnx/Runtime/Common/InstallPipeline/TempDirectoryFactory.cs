using System;
using PonyuDev.SherpaOnnx.Common.IO;

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
                FileSystemHelper.EnsureCreatedEmpty(path);
            }

            public void Clean()
            {
                FileSystemHelper.EnsureCreatedEmpty(Path);
            }

            public void Dispose()
            {
                FileSystemHelper.TryDeleteDirectory(Path);
            }
        }
    }
}
