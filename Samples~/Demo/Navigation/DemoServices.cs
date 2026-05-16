using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// POCO bag of every service / runtime resource the demo scene
    /// owns. Built once by <see cref="DemoNavigator"/> on Awake and
    /// passed by reference into every <see cref="IDemoView"/>'s
    /// <see cref="IDemoView.Bind"/> call. Each view picks the fields
    /// it needs and ignores the rest — no view-specific arg list
    /// needed.
    ///
    /// Fields stay nullable so the navigator can hand the same
    /// instance out before every service finishes initialization;
    /// views must guard with <c>service != null &amp;&amp; service.IsReady</c>
    /// before calling into them.
    /// </summary>
    public sealed class DemoServices
    {
        public ITtsService Tts;
        public IAsrService OfflineAsr;
        public IOnlineAsrService OnlineAsr;
        public IVadService Vad;
        public VadAsrPipeline Pipeline;
        public MicrophoneSource Microphone;
        public AudioSource AudioSource;
        public AudioClip SampleClip;
    }
}
