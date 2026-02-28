using System.Runtime.InteropServices;

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// Configures iOS AVAudioSession to PlayAndRecord category.
    /// Required for simultaneous TTS playback and microphone capture.
    /// On non-iOS platforms the call is a no-op.
    /// </summary>
    internal static class iOSAudioSessionBridge
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _SherpaOnnx_ConfigureAudioSessionPlayAndRecord();
#endif

        internal static void ConfigureForRecording()
        {
#if UNITY_IOS && !UNITY_EDITOR
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] iOSAudioSessionBridge: configuring PlayAndRecord");
            _SherpaOnnx_ConfigureAudioSessionPlayAndRecord();
#endif
        }
    }
}
