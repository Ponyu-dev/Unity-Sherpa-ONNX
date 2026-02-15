namespace PonyuDev.SherpaOnnx.Tts.Data
{
    /// <summary>
    /// Where the TTS model files are located.
    /// </summary>
    public enum TtsModelSource
    {
        /// <summary>Model bundled in StreamingAssets for offline use.</summary>
        Local,

        /// <summary>Model downloaded from a remote server at runtime.</summary>
        Remote
    }
}
