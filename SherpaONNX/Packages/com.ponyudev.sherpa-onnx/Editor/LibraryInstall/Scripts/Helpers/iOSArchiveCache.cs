using System;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Static facade for the iOS archive cache.
    /// Delegates all logic to <see cref="ArchiveCacheBase"/>.
    /// </summary>
    internal static class iOSArchiveCache
    {
        private static readonly iOSCacheImpl Impl = new();

        /// <summary>
        /// Instance reference for code that needs the interface directly.
        /// </summary>
        internal static IArchiveCache Cache => Impl;

        internal static event Action<string> OnStatus
        {
            add => Impl.OnStatus += value;
            remove => Impl.OnStatus -= value;
        }

        internal static event Action<float> OnProgress01
        {
            add => Impl.OnProgress01 += value;
            remove => Impl.OnProgress01 -= value;
        }

        internal static event Action OnCacheChanged
        {
            add => Impl.OnCacheChanged += value;
            remove => Impl.OnCacheChanged -= value;
        }

        internal static string CachePath => Impl.CachePath;
        internal static bool IsReady => Impl.IsReady;

        internal static void Clean() => Impl.Clean();

        internal static Task EnsureExtractedAsync(
            string url, string fileName, CancellationToken ct) =>
            Impl.EnsureExtractedAsync(url, fileName, ct);

        /// <summary>
        /// Returns the path to build-ios/ inside the cache.
        /// </summary>
        internal static string FindBuildIosPath() =>
            Impl.FindDirectoryInCache("build-ios");

        private sealed class iOSCacheImpl : ArchiveCacheBase
        {
            protected override string CacheFolderName => "SherpaOnnx_iOSCache";
            protected override string DownloadFolderName => "SherpaOnnx_iOSDownload";
            protected override string PlatformLabel => "iOS";
            protected override string ReadyMarker => "sherpa-onnx.xcframework";
        }
    }
}
