using System;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Static facade for the Android archive cache.
    /// Delegates all logic to <see cref="ArchiveCacheBase"/>.
    /// </summary>
    internal static class AndroidArchiveCache
    {
        private static readonly AndroidCacheImpl Impl = new();

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
        /// Returns the path to jniLibs inside the cache.
        /// </summary>
        internal static string FindJniLibsPath() =>
            Impl.FindDirectoryInCache("jniLibs");

        private sealed class AndroidCacheImpl : ArchiveCacheBase
        {
            protected override string CacheFolderName => "SherpaOnnx_AndroidCache";
            protected override string DownloadFolderName => "SherpaOnnx_AndroidDownload";
            protected override string PlatformLabel => "Android";
            protected override string ReadyMarker => "jniLibs";
        }
    }
}
