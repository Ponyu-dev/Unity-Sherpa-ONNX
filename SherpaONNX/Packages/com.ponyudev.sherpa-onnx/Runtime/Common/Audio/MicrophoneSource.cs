using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// POCO microphone capture with polling via
    /// <see cref="PlayerLoopTiming.Update"/>. Push (<see cref="SamplesAvailable"/>)
    /// and pull (<see cref="ReadNewSamples"/>) models. <see cref="IDisposable"/>.
    /// Uses Unity <see cref="Microphone"/> API by default.
    /// On Android, automatically falls back to native <c>AudioRecord</c>
    /// if Unity API returns silence.
    /// </summary>
    public sealed class MicrophoneSource : IDisposable
    {
        private readonly string _deviceName;
        private readonly int _sampleRate;
        private readonly int _clipLengthSec;
        private readonly bool _requestPermission;
        private readonly MicrophoneSettingsData _settings;

        private CancellationTokenSource _pollCts;
        private bool _disposed;
        private bool _useNativeFallback;

        private AudioClip _clip;
        private string _resolvedDevice;
        private int _pushLastPos;
        private int _pullLastPos;
        private GameObject _silentGo;
        private AudioSource _silentSource;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidAudioRecordBridge _androidBridge;
#endif

        public bool IsRecording { get; private set; }
        public string DeviceName => _deviceName;
        public int SampleRate => _sampleRate;

        /// <summary>Fires every frame with new PCM samples.</summary>
        public event Action<float[]> SamplesAvailable;

        /// <summary>Fires when recording stops.</summary>
        public event Action RecordingStopped;

        /// <summary>
        /// Fires when silence is detected and diagnostics are available.
        /// The string contains semicolon-separated diagnostic info.
        /// On Android includes native checks (global mic toggle, etc.).
        /// </summary>
        public event Action<string> SilenceDetected;

        public MicrophoneSource(
            MicrophoneSettingsData settings = null,
            string deviceName = null,
            bool requestPermission = true)
        {
            _settings = settings ?? new MicrophoneSettingsData();
            _deviceName = deviceName;
            _sampleRate = _settings.sampleRate;
            _clipLengthSec = _settings.clipLengthSec;
            _requestPermission = requestPermission;
        }

        /// <summary>Requests permission and starts capture.</summary>
        public async UniTask<bool> StartRecordingAsync(
            CancellationToken ct = default)
        {
            if (_disposed)
                return false;

            if (IsRecording)
                return true;

            if (_requestPermission
                && !await MicrophonePermission.RequestAsync())
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] MicrophoneSource: " +
                    "permission denied.");
                return false;
            }

            return await StartUnityAsync(ct);
        }

        /// <summary>Stops recording.</summary>
        public void StopRecording()
        {
            if (!IsRecording)
                return;

            CancelPollLoop();

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_useNativeFallback)
                StopAndroid();
            else
                StopUnity();
#else
            StopUnity();
#endif

            _useNativeFallback = false;
            IsRecording = false;
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] MicrophoneSource: stopped.");
            RecordingStopped?.Invoke();
        }

        /// <summary>
        /// Pull model: new samples since last call, or null.
        /// </summary>
        public float[] ReadNewSamples()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_useNativeFallback)
            {
                if (!IsRecording || _androidBridge == null)
                    return null;
                return _androidBridge.DrainBuffer();
            }
#endif
            if (!IsRecording || _clip == null)
                return null;

            int currentPos =
                Microphone.GetPosition(_resolvedDevice);
            return ExtractSamples(
                ref _pullLastPos, currentPos);
        }

        /// <summary>
        /// Returns entire circular buffer, or null.
        /// </summary>
        public float[] ReadAllSamples()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_useNativeFallback)
                return null;
#endif
            if (!IsRecording || _clip == null)
                return null;

            var buffer =
                new float[_clip.samples * _clip.channels];
            _clip.GetData(buffer, 0);
            return buffer;
        }

        /// <summary>
        /// Stops recording and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopRecording();

#if UNITY_ANDROID && !UNITY_EDITOR
            _androidBridge?.Dispose();
            _androidBridge = null;
#endif
            _clip = null;
        }

        // ── Unity Microphone path ──

        private async UniTask<bool> StartUnityAsync(
            CancellationToken ct)
        {
            if (Microphone.devices.Length == 0)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: " +
                    "no devices found.");
                return false;
            }

            _resolvedDevice = _deviceName;
            if (string.IsNullOrEmpty(_resolvedDevice))
                _resolvedDevice = Microphone.devices[0];

            _clip = Microphone.Start(
                _resolvedDevice, true,
                _clipLengthSec, _sampleRate);

            if (_clip == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: " +
                    "Microphone.Start returned null.");
                return false;
            }

            StartSilentPlayback();

            bool deviceReady =
                await WaitForMicrophoneReadyAsync(ct);
            if (!deviceReady)
            {
                StopSilentPlayback();
                Microphone.End(_resolvedDevice);
                _clip = null;
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: " +
                    "device did not start within " +
                    $"{_settings.micStartTimeoutSec}s.");
                return false;
            }

            IsRecording = true;
            _pushLastPos =
                Microphone.GetPosition(_resolvedDevice);
            _pullLastPos = _pushLastPos;

            _pollCts = CancellationTokenSource
                .CreateLinkedTokenSource(ct);
            PollUnityLoopAsync(_pollCts.Token).Forget();

            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] MicrophoneSource: started " +
                $"(device='{_resolvedDevice}', " +
                $"rate={_sampleRate}, " +
                $"clipFreq={_clip.frequency}).");
            return true;
        }

        private void StopUnity()
        {
            StopSilentPlayback();
            Microphone.End(_resolvedDevice);
        }

        private async UniTaskVoid PollUnityLoopAsync(
            CancellationToken ct)
        {
            int diagFrames = 0;
            int silentFrames = 0;
            bool speechDetected = false;

            while (!ct.IsCancellationRequested && IsRecording)
            {
                await UniTask.Yield(
                    PlayerLoopTiming.Update, ct);

                if (!IsRecording || _clip == null)
                    break;

                int currentPos = Microphone.GetPosition(
                    _resolvedDevice);
                float[] samples = ExtractSamples(
                    ref _pushLastPos, currentPos);

                if (samples == null || samples.Length == 0)
                    continue;

                float maxAbs = ComputeMaxAbs(samples);
                LogDiagnostics(
                    samples.Length, maxAbs, ref diagFrames);

                if (maxAbs >= _settings.silenceThreshold)
                {
                    speechDetected = true;
                    silentFrames = 0;
                }
                else if (!speechDetected)
                {
                    silentFrames++;
                    if (silentFrames
                        == _settings.silenceFrameLimit)
                    {
                        HandleSilenceDetected();
                        break;
                    }
                }

                SamplesAvailable?.Invoke(samples);
            }
        }

        // ── Silence detection ──

        private void HandleSilenceDetected()
        {
            string diagnosis = "UnityMic=silent";

#if UNITY_ANDROID && !UNITY_EDITOR
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] MicrophoneSource: Unity " +
                "Microphone returned silence. Switching " +
                "to native AudioRecord.");

            StopUnity();

            bool nativeOk = StartAndroidFallback();
            if (nativeOk)
            {
                diagnosis += "; Fallback=NativeAudioRecord";
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] MicrophoneSource: " +
                    "native AudioRecord fallback active.");
            }
            else
            {
                diagnosis += "; Fallback=FAILED";
                string nativeDiag =
                    _androidBridge?.DiagnoseSilence()
                    ?? "bridge=null";
                diagnosis += "; " + nativeDiag;
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: " +
                    "native fallback failed. Diag: "
                    + nativeDiag);
            }
#else
            SherpaOnnxLog.RuntimeWarning(
                "[SherpaOnnx] MicrophoneSource: " +
                "sustained silence detected (maxAbs < " +
                $"{_settings.silenceThreshold} for " +
                $"{_settings.silenceFrameLimit} frames).");
#endif

            SilenceDetected?.Invoke(diagnosis);
        }

        // ── Android native fallback ──

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool StartAndroidFallback()
        {
            _androidBridge = new AndroidAudioRecordBridge();

            if (!_androidBridge.Start())
            {
                _androidBridge.Dispose();
                _androidBridge = null;
                return false;
            }

            _useNativeFallback = true;
            _clip = null;

            CancelPollLoop();
            _pollCts = new CancellationTokenSource();
            PollAndroidLoopAsync(_pollCts.Token).Forget();
            return true;
        }

        private void StopAndroid()
        {
            _androidBridge?.Stop();
        }

        private async UniTaskVoid PollAndroidLoopAsync(
            CancellationToken ct)
        {
            int diagFrames = 0;
            int silentFrames = 0;
            bool speechDetected = false;

            while (!ct.IsCancellationRequested && IsRecording)
            {
                await UniTask.Yield(
                    PlayerLoopTiming.Update, ct);

                if (!IsRecording || _androidBridge == null)
                    break;

                float[] samples =
                    _androidBridge.DrainBuffer();

                if (samples == null || samples.Length == 0)
                    continue;

                float maxAbs = ComputeMaxAbs(samples);
                LogDiagnostics(
                    samples.Length, maxAbs, ref diagFrames);

                if (maxAbs >= _settings.silenceThreshold)
                {
                    speechDetected = true;
                    silentFrames = 0;
                }
                else if (!speechDetected)
                {
                    silentFrames++;
                    if (silentFrames
                        == _settings.silenceFrameLimit)
                    {
                        if (_androidBridge.HasNextSource)
                        {
                            string src = _androidBridge
                                .CurrentSourceName;
                            SherpaOnnxLog.RuntimeWarning(
                                "[SherpaOnnx] " +
                                "MicrophoneSource: silence " +
                                "on source=" + src +
                                ". Trying next.");

                            _androidBridge
                                .RestartWithNextSource();
                            silentFrames = 0;
                            diagFrames = 0;
                            continue;
                        }

                        string diag = _androidBridge
                            .DiagnoseSilence();
                        SherpaOnnxLog.RuntimeError(
                            "[SherpaOnnx] " +
                            "MicrophoneSource: all native " +
                            "sources silent. Diag: " + diag);
                        SilenceDetected?.Invoke(
                            "AllNativeSources=silent; "
                            + diag);
                        break;
                    }
                }

                SamplesAvailable?.Invoke(samples);
            }
        }
#endif

        // ── Silent AudioSource workaround ──

        private void StartSilentPlayback()
        {
            if (_clip == null)
                return;

            _silentGo = new GameObject(
                "[SherpaOnnx] MicSilentPlayback");
            _silentGo.hideFlags =
                HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(_silentGo);

            _silentSource =
                _silentGo.AddComponent<AudioSource>();
            _silentSource.clip = _clip;
            _silentSource.volume = 0f;
            _silentSource.loop = true;
            _silentSource.Play();
        }

        private void StopSilentPlayback()
        {
            if (_silentSource != null)
            {
                _silentSource.Stop();
                _silentSource = null;
            }

            if (_silentGo != null)
            {
                UnityEngine.Object.Destroy(_silentGo);
                _silentGo = null;
            }
        }

        // ── Microphone readiness ──

        private async UniTask<bool>
            WaitForMicrophoneReadyAsync(
                CancellationToken ct)
        {
            float elapsed = 0f;
            float timeout = _settings.micStartTimeoutSec;

            while (elapsed < timeout)
            {
                if (ct.IsCancellationRequested)
                    return false;

                if (Microphone.GetPosition(
                    _resolvedDevice) > 0)
                    return true;

                await UniTask.Yield(
                    PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }

            return false;
        }

        // ── Sample extraction ──

        private float[] ExtractSamples(
            ref int lastPos, int currentPos)
        {
            if (currentPos == lastPos)
                return null;

            int totalSamples = _clip.samples;
            int newSampleCount = currentPos > lastPos
                ? currentPos - lastPos
                : totalSamples - lastPos + currentPos;

            var samples =
                new float[newSampleCount * _clip.channels];

            if (currentPos > lastPos)
            {
                _clip.GetData(samples, lastPos);
            }
            else
            {
                int tailCount = totalSamples - lastPos;
                var tail =
                    new float[tailCount * _clip.channels];
                _clip.GetData(tail, lastPos);

                var head =
                    new float[currentPos * _clip.channels];
                if (currentPos > 0)
                    _clip.GetData(head, 0);

                Array.Copy(
                    tail, 0, samples, 0, tail.Length);
                Array.Copy(
                    head, 0, samples,
                    tail.Length, head.Length);
            }

            lastPos = currentPos;
            return samples;
        }

        // ── Shared helpers ──

        private static float ComputeMaxAbs(float[] samples)
        {
            float maxAbs = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = samples[i] < 0
                    ? -samples[i]
                    : samples[i];
                if (abs > maxAbs)
                    maxAbs = abs;
            }
            return maxAbs;
        }

        private void LogDiagnostics(
            int sampleCount, float maxAbs,
            ref int diagFrames)
        {
            if (diagFrames >= _settings.diagFrameCount)
                return;

            string source = _useNativeFallback
                ? "native" : "unity";

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] MicPoll#{diagFrames}" +
                $"({source}): len={sampleCount}, " +
                $"maxAbs={maxAbs:F6}");
            diagFrames++;
        }

        private void CancelPollLoop()
        {
            if (_pollCts == null)
                return;
            _pollCts.Cancel();
            _pollCts.Dispose();
            _pollCts = null;
        }
    }
}
