using System.Runtime.InteropServices;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// Cross-platform audio session helper for simultaneous TTS playback
    /// and microphone capture.
    /// iOS: switches AVAudioSession between PlayAndRecord (recording)
    /// and Playback (idle).
    /// Android: sets AudioManager MODE_IN_COMMUNICATION + speakerphone
    /// once per process — this engages the platform AEC/AGC pipeline,
    /// without which many devices return near-silent capture while TTS
    /// is using the media stream.
    /// On non-iOS/Android platforms every call is a no-op.
    /// </summary>
    /// <remarks>
    /// <see cref="MicrophoneSource"/> calls these automatically when
    /// <see cref="Config.MicrophoneSettingsData.manageAudioSession"/>
    /// is <c>true</c>. They are public so host projects can manage the
    /// audio session themselves (e.g. coordinate with their own TTS
    /// engine or push-to-talk flow) with
    /// <see cref="Config.MicrophoneSettingsData.manageAudioSession"/>
    /// disabled.
    /// </remarks>
    public static class AudioSessionBridge
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _SherpaOnnx_ConfigureAudioSessionPlayAndRecord();

        [DllImport("__Internal")]
        private static extern void _SherpaOnnx_RestoreAudioSessionPlayback();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private const int AndroidModeNormal = 0;
        private const int AndroidModeInCommunication = 3;
        private static bool _androidConfigured;
#endif

        /// <summary>
        /// Configures the platform audio session for microphone capture
        /// alongside playback. Safe to call repeatedly.
        /// </summary>
        /// <returns>
        /// <c>true</c> when an Android audio mode change was just applied
        /// (caller should wait for the route change to settle before
        /// starting <see cref="UnityEngine.Microphone"/>; otherwise the
        /// device starts on the old route and never produces samples).
        /// <c>false</c> when nothing changed (iOS, repeat call on Android,
        /// or unsupported platform).
        /// </returns>
        public static bool ConfigureForRecording()
        {
#if UNITY_IOS && !UNITY_EDITOR
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] AudioSessionBridge: configuring PlayAndRecord");
            _SherpaOnnx_ConfigureAudioSessionPlayAndRecord();
            return false;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return ApplyAndroidCommunicationMode();
#else
            return false;
#endif
        }

        /// <summary>
        /// Restores the platform audio session for playback-only use.
        /// </summary>
        /// <param name="androidReturnToNormal">
        /// When <c>true</c>, also returns Android AudioManager to
        /// MODE_NORMAL and disables speakerphone. Default is <c>false</c>
        /// because mid-session mode switches trigger an audio route
        /// change that can break the next mic capture; leaving the
        /// communication mode in place is the recommended path.
        /// </param>
        public static void RestoreForPlayback(bool androidReturnToNormal = false)
        {
#if UNITY_IOS && !UNITY_EDITOR
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] AudioSessionBridge: restoring Playback");
            _SherpaOnnx_RestoreAudioSessionPlayback();
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (androidReturnToNormal)
                ApplyAndroidNormalMode();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool ApplyAndroidCommunicationMode()
        {
            if (_androidConfigured)
                return false;

            try
            {
                using var unityPlayer = new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity");
                using var audioManager = activity.Call<AndroidJavaObject>(
                    "getSystemService", "audio");

                audioManager.Call("setMode", AndroidModeInCommunication);
                audioManager.Call("setSpeakerphoneOn", true);

                _androidConfigured = true;
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] AudioSessionBridge: " +
                    "Android MODE_IN_COMMUNICATION + speakerphone ON");
                return true;
            }
            catch (System.Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AudioSessionBridge: " +
                    "Android configure failed: " + ex.Message);
                return false;
            }
        }

        private static void ApplyAndroidNormalMode()
        {
            if (!_androidConfigured)
                return;

            try
            {
                using var unityPlayer = new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity");
                using var audioManager = activity.Call<AndroidJavaObject>(
                    "getSystemService", "audio");

                audioManager.Call("setSpeakerphoneOn", false);
                audioManager.Call("setMode", AndroidModeNormal);

                _androidConfigured = false;
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] AudioSessionBridge: " +
                    "Android MODE_NORMAL + speakerphone OFF");
            }
            catch (System.Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AudioSessionBridge: " +
                    "Android restore failed: " + ex.Message);
            }
        }
#endif
    }
}
