using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Import
{
    /// <summary>
    /// Copies extracted ASR model files into
    /// Assets/StreamingAssets/SherpaOnnx/asr-models/{archiveName}/.
    /// </summary>
    internal sealed class AsrModelContentHandler
        : IExtractedContentHandler
    {
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action<string> OnError;

        private readonly string _archiveName;
        internal string DestinationDirectory { get; private set; }

        internal AsrModelContentHandler(string archiveName)
        {
            if (string.IsNullOrEmpty(archiveName))
                throw new ArgumentNullException(nameof(archiveName));

            _archiveName = archiveName;
        }

        public Task HandleAsync(
            string extractedDirectory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            OnStatus?.Invoke("Preparing model files...");
            OnProgress01?.Invoke(0f);

            string sourceDir = FindModelRoot(extractedDirectory);
            string destDir = AsrModelPaths.GetModelDir(_archiveName);
            DestinationDirectory = destDir;

            Directory.CreateDirectory(destDir);

            string[] files = Directory.GetFiles(
                sourceDir, "*", SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                string msg = "No files found in extracted directory: "
                    + sourceDir;
                OnError?.Invoke(msg);
                throw new FileNotFoundException(msg);
            }

            for (int i = 0; i < files.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = files[i]
                    .Substring(sourceDir.Length)
                    .TrimStart(Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);

                string destPath = Path.Combine(destDir, relativePath);
                string destSubDir = Path.GetDirectoryName(destPath);

                if (!string.IsNullOrEmpty(destSubDir))
                    Directory.CreateDirectory(destSubDir);

                string fileName = Path.GetFileName(files[i]);
                OnStatus?.Invoke($"Copying {fileName}...");

                File.Copy(files[i], destPath, overwrite: true);
                OnProgress01?.Invoke((float)(i + 1) / files.Length);
            }

            OnStatus?.Invoke($"Model files copied to {destDir}");
            return Task.CompletedTask;
        }

        private static string FindModelRoot(string extractedDir)
        {
            string[] topFiles = Directory.GetFiles(extractedDir);
            string[] topDirs = Directory.GetDirectories(extractedDir);

            if (topFiles.Length == 0 && topDirs.Length == 1)
                return topDirs[0];

            return extractedDir;
        }
    }
}
