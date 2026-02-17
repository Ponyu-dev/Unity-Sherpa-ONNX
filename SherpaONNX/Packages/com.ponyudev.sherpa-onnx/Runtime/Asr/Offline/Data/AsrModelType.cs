namespace PonyuDev.SherpaOnnx.Asr.Offline.Data
{
    /// <summary>
    /// Supported offline ASR model architectures in sherpa-onnx.
    /// Matches sub-configs in <see cref="SherpaOnnx.OfflineModelConfig"/>.
    /// </summary>
    public enum AsrModelType
    {
        Transducer,
        Paraformer,
        Whisper,
        SenseVoice,
        Moonshine,
        NemoCtc,
        ZipformerCtc,
        Tdnn,
        FireRedAsr,
        Dolphin,
        Canary,
        WenetCtc,
        Omnilingual,
        MedAsr,
        FunAsrNano
    }
}
