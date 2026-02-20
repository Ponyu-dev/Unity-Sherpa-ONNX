using System;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers
{
    /// <summary>
    /// Public contract for platform archive caches.
    /// Allows UI and pipeline code to work without lambdas or static facades.
    /// </summary>
    internal interface IArchiveCache
    {
        event Action<string> OnStatus;
        event Action<float> OnProgress01;
        event Action OnCacheChanged;

        string CachePath { get; }
        bool IsReady { get; }

        void Clean();

        Task EnsureExtractedAsync(
            string url, string fileName, CancellationToken ct);
    }
}
