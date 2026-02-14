namespace PonyuDev.SherpaOnnx.Common.InstallPipeline
{
    public enum PipelineStage
    {
        None = 0,
        Preparing = 1,
        Downloading = 2,
        Extracting = 3,
        HandlingContent = 4,
        CleaningUp = 5,
        Completed = 6,
        Failed = 7
    }
}