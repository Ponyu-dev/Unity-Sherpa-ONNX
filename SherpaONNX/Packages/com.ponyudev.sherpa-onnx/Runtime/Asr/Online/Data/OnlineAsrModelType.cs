namespace PonyuDev.SherpaOnnx.Asr.Online.Data
{
    /// <summary>
    /// Supported online (streaming) ASR model architectures.
    /// Matches sub-configs in <see cref="SherpaOnnx.OnlineModelConfig"/>.
    /// </summary>
    public enum OnlineAsrModelType
    {
        Transducer,
        Paraformer,
        Zipformer2Ctc,
        NemoCtc,
        ToneCtc
    }
}
