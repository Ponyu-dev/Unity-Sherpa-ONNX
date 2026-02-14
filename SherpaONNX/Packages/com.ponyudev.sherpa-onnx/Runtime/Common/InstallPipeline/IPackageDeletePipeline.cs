using System;

namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    /// <summary>
    /// Deletes installed package files from the target directory.
    /// </summary>
    public interface IPackageDeletePipeline
    {
        event Action<string> OnStatus;
        event Action<string> OnError;
        event Action OnCompleted;

        void Run(string targetPath);
    }
}
