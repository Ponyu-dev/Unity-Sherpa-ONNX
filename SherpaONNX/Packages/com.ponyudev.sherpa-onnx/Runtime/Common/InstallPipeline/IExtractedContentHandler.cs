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

        Task HandleAsync(string extractedDirectory, CancellationToken cancellationToken);
    }
}