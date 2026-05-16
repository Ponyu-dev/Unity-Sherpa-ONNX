using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers
{
    /// <summary>
    /// Copies Android native .so files from jniLibs/{archName}/
    /// into Assets/Plugins/SherpaOnnx/Android/{archName}/.
    /// </summary>
    internal sealed class AndroidNativeContentHandler : IExtractedContentHandler
    {
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action<string> OnError;

        public string DestinationDirectory => null;

        private readonly string _archName;

        internal AndroidNativeContentHandler(string archName)
        {
            if (string.IsNullOrEmpty(archName))
                throw new ArgumentNullException(nameof(archName));

            _archName = archName;
        }

        public Task HandleAsync(string extractedDirectory, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OnStatus?.Invoke($"Searching native libs for Android {_archName}...");
            OnProgress01?.Invoke(0f);

            string archDir = Path.Combine(extractedDirectory, _archName);

            if (!Directory.Exists(archDir))
            {
                string msg = $"Android arch directory not found: {archDir}";
                OnError?.Invoke(msg);
                throw new DirectoryNotFoundException(msg);
            }

            string[] files = Directory.GetFiles(archDir);

            if (files.Length == 0)
            {
                string msg = $"No native files found in: {archDir}";
                OnError?.Invoke(msg);
                throw new FileNotFoundException(msg);
            }

            string destDir = Path.Combine(ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, "Android", _archName);
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

            OnStatus?.Invoke($"Android {_archName} libs installed.");
            return Task.CompletedTask;
        }
    }
}
