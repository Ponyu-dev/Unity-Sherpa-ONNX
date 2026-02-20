using System;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Common.Extractors
{
    public interface IArchiveExtractor : IDisposable
    {
        event Action<string, string> OnStarted; // archivePath, extractDirectory
        event Action<string, int, int> OnProgress; // currentEntryName, extractedEntries, totalEntriesOrMinus1
        event Action<string> OnCompleted; // extractDirectory
        event Action<string> OnError; // message

        Task ExtractAsync(
            string archivePath,
            string tempDirectoryPath,
            CancellationToken cancellationToken);
    }
}