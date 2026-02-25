namespace PonyuDev.SherpaOnnx.Common.Data
{
    /// <summary>
    /// Where model files are located at runtime.
    /// Shared by all model types (TTS, ASR, VAD).
    /// </summary>
    public enum ModelSource
    {
        /// <summary>Model folder bundled in StreamingAssets for offline use.</summary>
        Local = 0,

        /// <summary>Model downloaded from a remote server at runtime.</summary>
        Remote = 1,

        /// <summary>
        /// Model folder zipped at build time.
        /// Extracted from StreamingAssets to persistentDataPath on first launch.
        /// </summary>
        LocalZip = 2
    }
}
