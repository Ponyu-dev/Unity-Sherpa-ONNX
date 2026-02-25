using System;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    /// <summary>
    /// Your installation logic working with extracted directory.
    /// For example: find DLLs, copy to Plugins, parse .nuspec, etc.
    /// </summary>
    public interface IExtractedContentHandler
    {
        event Action<string> OnStatus;
        event Action<float> OnProgress01;
        event Action<string> OnError;

        /// <summary>
        /// Final destination directory where files are copied.
        /// Available after <see cref="HandleAsync"/> completes.
        /// May be null if the handler does not copy files to a final destination.
        /// </summary>
        string DestinationDirectory { get; }

        Task HandleAsync(string extractedDirectory, CancellationToken cancellationToken);
    }
}