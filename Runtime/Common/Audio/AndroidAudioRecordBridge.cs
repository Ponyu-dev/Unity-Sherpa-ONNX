#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using PonyuDev.SherpaOnnx.Common;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// C# bridge to <c>com.ponyudev.sherpaonnx.audio.AndroidAudioRecorder</c>.
    /// Wraps <see cref="AndroidJavaObject"/> lifecycle.
    /// Active only on Android runtime builds.
    /// </summary>
    internal sealed class AndroidAudioRecordBridge : IDisposable
    {
        private const string JavaClass =
            "com.ponyudev.sherpaonnx.audio.AndroidAudioRecorder";

        private AndroidJavaObject _recorder;
        private bool _disposed;

        public bool IsRecording { get; private set; }
        public int SampleRate => 16000;

        /// <summary>
        /// Creates the Java <c>AudioRecord</c> and starts capture
        /// on a native background thread. Returns <c>true</c> on success.
        /// </summary>
        public bool Start()
        {
            if (_disposed)
                return false;

            if (IsRecording)
                return true;

            try
            {
                _recorder = new AndroidJavaObject(JavaClass);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AndroidAudioRecordBridge: " +
                    "failed to create Java object: " + ex.Message);
                return false;
            }

            bool ok = _recorder.Call<bool>("start");

            if (!ok)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AndroidAudioRecordBridge: " +
                    "Java start() returned false.");
                _recorder.Dispose();
                _recorder = null;
                return false;
            }

            IsRecording = true;
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] AndroidAudioRecordBridge: started.");
            return true;
        }

        /// <summary>
        /// Stops the native recording thread and releases
        /// the Java <c>AudioRecord</c> object.
        /// </summary>
        public void Stop()
        {
            if (!IsRecording || _recorder == null)
                return;

            _recorder.Call("stop");
            IsRecording = false;

            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] AndroidAudioRecordBridge: stopped.");
        }

        /// <summary>
        /// Drains accumulated samples from the Java-side buffer.
        /// Returns <c>null</c> when no new data is available.
        /// Must be called from the Unity main thread.
        /// </summary>
        public float[] DrainBuffer()
        {
            if (!IsRecording || _recorder == null)
                return null;

            return _recorder.Call<float[]>("drainBuffer");
        }

        /// <summary>
        /// Stops current recording, switches to the next audio source
        /// in the cascade (VOICE_RECOGNITION → VOICE_COMMUNICATION → MIC),
        /// and restarts. Returns false if no more sources to try.
        /// </summary>
        public bool RestartWithNextSource()
        {
            if (_recorder == null)
                return false;

            bool ok = _recorder.Call<bool>("restartWithNextSource");

            if (ok)
            {
                string src = _recorder.Call<string>(
                    "getCurrentSourceName");
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] AndroidAudioRecordBridge: " +
                    "restarted with source=" + src);
            }
            else
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] AndroidAudioRecordBridge: " +
                    "all audio sources exhausted.");
                IsRecording = false;
            }

            return ok;
        }

        public bool HasNextSource =>
            _recorder?.Call<bool>("hasNextSource") ?? false;

        public string CurrentSourceName =>
            _recorder?.Call<string>("getCurrentSourceName")
            ?? "UNKNOWN";

        /// <summary>
        /// Runs silence diagnostics via Java. Checks global mic toggle,
        /// AppOps, concurrent capture, etc.
        /// Returns semicolon-separated diagnostic string.
        /// </summary>
        public string DiagnoseSilence()
        {
            if (_recorder == null)
                return "Recorder=null";

            try
            {
                using var unityPlayer = new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity");

                return _recorder.Call<string>(
                    "diagnoseSilence", activity);
            }
            catch (Exception ex)
            {
                return "DiagError=" + ex.Message;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();

            if (_recorder != null)
            {
                _recorder.Dispose();
                _recorder = null;
            }
        }
    }
}
#endif
