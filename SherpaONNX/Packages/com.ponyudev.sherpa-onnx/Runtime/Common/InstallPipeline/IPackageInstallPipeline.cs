using System;
using System.Threading;
using System.Threading.Tasks;

namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    public interface IPackageInstallPipeline : IDisposable
    {
        event Action<PipelineStage> OnStageChanged;
        event Action<string> OnStatus;
        event Action<float> OnProgress01;
        event Action<string> OnError;
        event Action OnCompleted;

        Task RunAsync(
            string url,
            string fileName,
            CancellationToken cancellationToken);
    }
}