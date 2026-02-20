using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers
{
    /// <summary>
    /// Copies native libraries from extracted NuGet package
    /// (runtimes/{rid}/native/*) into Assets/Plugins/SherpaOnnx/{rid}/.
    /// </summary>
    internal sealed class NativeLibraryContentHandler : IExtractedContentHandler
    {
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action<string> OnError;

        private readonly string _rid;

        internal NativeLibraryContentHandler(string rid)
        {
            if (string.IsNullOrEmpty(rid))
                throw new ArgumentNullException(nameof(rid));

            _rid = rid;
        }

        public Task HandleAsync(string extractedDirectory, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OnStatus?.Invoke($"Searching native libs for {_rid}...");
            OnProgress01?.Invoke(0f);

            string nativeDir = Path.Combine(extractedDirectory, "runtimes", _rid, "native");

            if (!Directory.Exists(nativeDir))
            {
                string msg = $"Native directory not found: {nativeDir}";
                OnError?.Invoke(msg);
                throw new DirectoryNotFoundException(msg);
            }

            string[] files = Directory.GetFiles(nativeDir);

            if (files.Length == 0)
            {
                string msg = $"No native files found in: {nativeDir}";
                OnError?.Invoke(msg);
                throw new FileNotFoundException(msg);
            }

            string destDir = Path.Combine(
                ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, _rid);
            Directory.CreateDirectory(destDir);

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(files[i]);
                string destPath = Path.Combine(destDir, fileName);

                OnStatus?.Invoke($"Copying {fileName}...");
                File.Copy(files[i], destPath, overwrite: true);

                OnProgress01?.Invoke((float)(i + 1) / files.Length);
            }

            OnStatus?.Invoke($"Native libs for {_rid} installed.");
            return Task.CompletedTask;
        }
    }
}
