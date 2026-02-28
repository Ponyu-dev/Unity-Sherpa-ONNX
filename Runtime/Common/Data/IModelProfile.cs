namespace PonyuDev.SherpaOnnx.Common.Data
{
    /// <summary>
    /// Common identity fields shared by all model profiles (ASR, TTS, VAD).
    /// Enables generic presenter code to read/write model source, URL, etc.
    /// </summary>
    public interface IModelProfile : IProfileData
    {
        ModelSource ModelSource { get; set; }
        string SourceUrl { get; }
        string RemoteBaseUrl { get; }
    }
}
