using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers
{
    /// <summary>
    /// Copies iOS xcframeworks (sherpa-onnx + onnxruntime) into
    /// Assets/Plugins/SherpaOnnx/iOS/.
    /// Device (ios-arm64) is always included.
    /// Simulator and macOS slices are optional via constructor flags.
    /// </summary>
    internal sealed class iOSNativeContentHandler : IExtractedContentHandler
    {
        public event Action<string> OnStatus;
        public event Action<float> OnProgress01;
        public event Action<string> OnError;

        public string DestinationDirectory => null;

        private const string SherpaXcframework = "sherpa-onnx.xcframework";
        private const string OnnxruntimeXcframework = "onnxruntime.xcframework";

        private readonly bool _includeSimulator;
        private readonly bool _includeMac;

        internal iOSNativeContentHandler(bool includeSimulator, bool includeMac)
        {
            _includeSimulator = includeSimulator;
            _includeMac = includeMac;
        }

        public Task HandleAsync(string extractedRoot, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var excludes = BuildExcludes();

            OnStatus?.Invoke("Searching xcframeworks for iOS...");
            OnProgress01?.Invoke(0f);

            string destDir = Path.Combine(ConstantsInstallerPaths.AssetsPluginsSherpaOnnx, "iOS");

            if (Directory.Exists(destDir))
                Directory.Delete(destDir, recursive: true);

            var xcframeworks = FindXcframeworks(extractedRoot);

            if (xcframeworks.Count == 0)
            {
                string msg = $"No xcframeworks found in {extractedRoot}";
                OnError?.Invoke(msg);
                throw new DirectoryNotFoundException(msg);
            }

            int totalFiles = 0;
            foreach (string xcDir in xcframeworks)
                totalFiles += CountFilteredFiles(xcDir, excludes);

            int copied = 0;

            foreach (string xcDir in xcframeworks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string xcName = Path.GetFileName(xcDir);
                string destXcDir = Path.Combine(destDir, xcName);

                OnStatus?.Invoke($"Copying {xcName}...");
                CopyXcframeworkFiltered(xcDir, destXcDir, excludes, cancellationToken, ref copied, totalFiles);
            }

            OnProgress01?.Invoke(1f);
            OnStatus?.Invoke("iOS frameworks installed.");
            return Task.CompletedTask;
        }

        private HashSet<string> BuildExcludes()
        {
            var excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!_includeMac)
                excludes.Add("macos-arm64_x86_64");

            if (!_includeSimulator)
                excludes.Add("ios-arm64_x86_64-simulator");

            return excludes;
        }

        private static List<string> FindXcframeworks(string extractedRoot)
        {
            var result = new List<string>(2);

            string[] sherpa = Directory.GetDirectories(extractedRoot, SherpaXcframework, SearchOption.AllDirectories);
            if (sherpa.Length > 0)
                result.Add(sherpa[0]);

            string[] onnx = Directory.GetDirectories(extractedRoot, OnnxruntimeXcframework, SearchOption.AllDirectories);
            if (onnx.Length > 0)
                result.Add(onnx[0]);

            return result;
        }

        private void CopyXcframeworkFiltered(
            string srcXcDir,
            string destXcDir,
            HashSet<string> excludes,
            CancellationToken ct,
            ref int copied,
            int totalFiles)
        {
            Directory.CreateDirectory(destXcDir);

            foreach (string file in Directory.GetFiles(srcXcDir))
            {
                ct.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(destXcDir, fileName), overwrite: true);

                copied++;
                ReportProgress(copied, totalFiles);
            }

            foreach (string subDir in Directory.GetDirectories(srcXcDir))
            {
                string dirName = Path.GetFileName(subDir);

                if (excludes.Contains(dirName))
                    continue;

                string destSub = Path.Combine(destXcDir, dirName);
                CopyDirectoryRecursive(subDir, destSub, ct, ref copied, totalFiles);
            }
        }

        private void CopyDirectoryRecursive(
            string sourceDir,
            string destDir,
            CancellationToken ct,
            ref int copied,
            int totalFiles)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                ct.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(file);
                OnStatus?.Invoke($"Copying {fileName}...");
                File.Copy(file, Path.Combine(destDir, fileName), overwrite: true);

                copied++;
                ReportProgress(copied, totalFiles);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                CopyDirectoryRecursive(subDir, Path.Combine(destDir, dirName), ct, ref copied, totalFiles);
            }
        }

        private void ReportProgress(int copied, int total)
        {
            if (total > 0)
                OnProgress01?.Invoke((float)copied / total);
        }

        private static int CountFilteredFiles(string xcDir, HashSet<string> excludes)
        {
            int count = Directory.GetFiles(xcDir).Length;

            foreach (string subDir in Directory.GetDirectories(xcDir))
            {
                string dirName = Path.GetFileName(subDir);

                if (excludes.Contains(dirName))
                    continue;

                count += CountFilesRecursive(subDir);
            }

            return count;
        }

        private static int CountFilesRecursive(string dir)
        {
            int count = Directory.GetFiles(dir).Length;
            foreach (string sub in Directory.GetDirectories(dir))
                count += CountFilesRecursive(sub);
            return count;
        }
    }
}
