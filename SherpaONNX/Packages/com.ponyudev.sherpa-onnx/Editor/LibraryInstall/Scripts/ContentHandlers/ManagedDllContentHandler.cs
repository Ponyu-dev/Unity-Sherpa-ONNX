using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers
{
    /// <summary>
    /// Copies managed sherpa-onnx.dll from extracted NuGet package
    /// (lib/netstandard2.0/sherpa-onnx.dll) into Assets/Plugins/SherpaOnnx/.
    /// </summary>
    internal sealed class ManagedDllContentHandler : IExtractedContentHandler
    {
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action<string> OnError;

        public Task HandleAsync(string extractedDirectory, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OnStatus?.Invoke("Searching for managed DLL...");
            OnProgress01?.Invoke(0f);

            string sourcePath = Path.Combine(
                extractedDirectory,
                ConstantsInstallerPaths.NuGetManagedDllSubPath);

            if (!File.Exists(sourcePath))
            {
                string msg = $"Managed DLL not found at: {sourcePath}";
                OnError?.Invoke(msg);
                throw new FileNotFoundException(msg, sourcePath);
            }

            OnStatus?.Invoke("Copying managed DLL...");
            OnProgress01?.Invoke(0.3f);

            string destDir = ConstantsInstallerPaths.AssetsPluginsSherpaOnnx;
            Directory.CreateDirectory(destDir);

            string destPath = Path.Combine(destDir, ConstantsInstallerPaths.ManagedDllFileName);
            File.Copy(sourcePath, destPath, overwrite: true);

            OnProgress01?.Invoke(1f);
            OnStatus?.Invoke("Managed DLL installed.");

            return Task.CompletedTask;
        }
    }
}
