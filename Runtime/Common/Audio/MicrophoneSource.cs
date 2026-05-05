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
    /// Uses Unity <see cref="Microphone"/> on every platform; the captured
    /// rate is resampled to <see cref="MicrophoneSettingsData.sampleRate"/>
    /// when the device's native rate differs from the requested one.
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

        private AudioClip _clip;
        private string _resolvedDevice;
        private int _clipFrequency;
        private int _pushLastPos;
        private int _pullLastPos;
        private GameObject _silentGo;
        private AudioSource _silentSource;

        public bool IsRecording { get; private set; }
        public string DeviceName => _deviceName;
        public int SampleRate => _sampleRate;

        /// <summary>Fires every frame with new PCM samples.</summary>
        public event Action<float[]> SamplesAvailable;

        /// <summary>Fires when recording stops.</summary>
        public event Action RecordingStopped;

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
        public async UniTask<bool> StartRecordingAsync(CancellationToken ct = default)
        {
            if (_disposed)
                return false;

            if (IsRecording)
                return true;

            if (_requestPermission && !await MicrophonePermission.RequestAsync())
            {
                SherpaOnnxLog.RuntimeWarning("[SherpaOnnx] MicrophoneSource: permission denied.");
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
            StopSilentPlayback();
            Microphone.End(_resolvedDevice);

            if (_settings.manageAudioSession)
                AudioSessionBridge.RestoreForPlayback(_settings.androidReturnToNormalOnStop);

            IsRecording = false;
            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] MicrophoneSource: stopped.");
            RecordingStopped?.Invoke();
        }

        /// <summary>
        /// Pull model: new samples since last call, or null.
        /// </summary>
        public float[] ReadNewSamples()
        {
            if (!IsRecording || _clip == null)
                return null;

            int currentPos = Microphone.GetPosition(_resolvedDevice);
            float[] samples = ExtractSamples(ref _pullLastPos, currentPos);
            return ResampleIfNeeded(samples);
        }

        /// <summary>
        /// Returns entire circular buffer, or null.
        /// </summary>
        public float[] ReadAllSamples()
        {
            if (!IsRecording || _clip == null)
                return null;

            var buffer = new float[_clip.samples * _clip.channels];
            _clip.GetData(buffer, 0);
            return ResampleIfNeeded(buffer);
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
            _clip = null;
        }

        // ── Unity Microphone path ──

        private async UniTask<bool> StartUnityAsync(CancellationToken ct)
        {
            if (Microphone.devices.Length == 0)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] MicrophoneSource: no devices found.");
                return false;
            }

            _resolvedDevice = _deviceName;
            if (string.IsNullOrEmpty(_resolvedDevice))
                _resolvedDevice = Microphone.devices[0];

            if (_settings.manageAudioSession)
            {
                bool androidModeJustApplied = AudioSessionBridge.ConfigureForRecording();
                if (androidModeJustApplied && _settings.androidAudioSessionSettleMs > 0)
                {
                    SherpaOnnxLog.RuntimeLog(
                        "[SherpaOnnx] MicrophoneSource: waiting " +
                        $"{_settings.androidAudioSessionSettleMs}ms for Android audio route to settle.");
                    await UniTask.Delay(_settings.androidAudioSessionSettleMs, cancellationToken: ct);
                }
            }

            _clip = Microphone.Start(_resolvedDevice, true, _clipLengthSec, _sampleRate);

            if (_clip == null)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] MicrophoneSource: Microphone.Start returned null.");
                if (_settings.manageAudioSession)
                    AudioSessionBridge.RestoreForPlayback(_settings.androidReturnToNormalOnStop);
                return false;
            }

            StartSilentPlayback();

            bool deviceReady = await WaitForMicrophoneReadyAsync(ct);
            if (!deviceReady)
            {
                StopSilentPlayback();
                Microphone.End(_resolvedDevice);
                _clip = null;
                if (_settings.manageAudioSession)
                    AudioSessionBridge.RestoreForPlayback(_settings.androidReturnToNormalOnStop);
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: device did not start within " +
                    $"{_settings.micStartTimeoutSec}s.");
                return false;
            }

            IsRecording = true;
            _clipFrequency = _clip.frequency;
            _pushLastPos = Microphone.GetPosition(_resolvedDevice);
            _pullLastPos = _pushLastPos;

            _pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            PollUnityLoopAsync(_pollCts.Token).Forget();

            bool resampling = _clipFrequency != _sampleRate;
            SherpaOnnxLog.RuntimeLog(
                "[SherpaOnnx] MicrophoneSource: started " +
                $"(device='{_resolvedDevice}', " +
                $"rate={_sampleRate}, " +
                $"clipFreq={_clipFrequency}, " +
                $"resampling={(resampling ? _settings.resamplingMode.ToString() : "off")}).");
            return true;
        }

        private async UniTaskVoid PollUnityLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsRecording)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (!IsRecording || _clip == null)
                    break;

                int currentPos = Microphone.GetPosition(_resolvedDevice);
                float[] samples = ExtractSamples(ref _pushLastPos, currentPos);
                samples = ResampleIfNeeded(samples);

                if (samples == null || samples.Length == 0)
                    continue;

                SamplesAvailable?.Invoke(samples);
            }
        }

        // ── Silent AudioSource workaround ──
        // Some platforms route mic audio only while an AudioSource is
        // playing (iOS, certain Android builds). A silent looped clip
        // keeps the pipeline alive without affecting playback mix.

        private void StartSilentPlayback()
        {
            if (_clip == null)
                return;

            _silentGo = new GameObject("[SherpaOnnx] MicSilentPlayback");
            _silentGo.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(_silentGo);

            _silentSource = _silentGo.AddComponent<AudioSource>();
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

        private async UniTask<bool> WaitForMicrophoneReadyAsync(CancellationToken ct)
        {
            float elapsed = 0f;
            float timeout = _settings.micStartTimeoutSec;

            while (elapsed < timeout)
            {
                if (ct.IsCancellationRequested)
                    return false;

                if (Microphone.GetPosition(_resolvedDevice) > 0)
                    return true;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }

            return false;
        }

        // ── Sample extraction ──

        private float[] ExtractSamples(ref int lastPos, int currentPos)
        {
            if (currentPos == lastPos)
                return null;

            int totalSamples = _clip.samples;
            int newSampleCount = currentPos > lastPos
                ? currentPos - lastPos
                : totalSamples - lastPos + currentPos;

            var samples = new float[newSampleCount * _clip.channels];

            if (currentPos > lastPos)
            {
                _clip.GetData(samples, lastPos);
            }
            else
            {
                int tailCount = totalSamples - lastPos;
                var tail = new float[tailCount * _clip.channels];
                _clip.GetData(tail, lastPos);

                var head = new float[currentPos * _clip.channels];
                if (currentPos > 0)
                    _clip.GetData(head, 0);

                Array.Copy(tail, 0, samples, 0, tail.Length);
                Array.Copy(head, 0, samples, tail.Length, head.Length);
            }

            lastPos = currentPos;
            return samples;
        }

        private float[] ResampleIfNeeded(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return samples;
            if (_clipFrequency <= 0 || _clipFrequency == _sampleRate)
                return samples;
            return Resampler.Resample(samples, _clipFrequency, _sampleRate, _settings.resamplingMode);
        }

        // ── Shared helpers ──

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
