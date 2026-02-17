using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Audio
{
    /// <summary>
    /// POCO microphone capture with circular buffer polling via
    /// <see cref="PlayerLoopTiming.Update"/>. Push (<see cref="SamplesAvailable"/>)
    /// and pull (<see cref="ReadNewSamples"/>) models. <see cref="IDisposable"/>.
    /// </summary>
    public sealed class MicrophoneSource : IDisposable
    {
        private readonly string _deviceName;
        private readonly int _sampleRate;
        private readonly int _clipLengthSec;
        private readonly bool _requestPermission;

        private AudioClip _clip;
        private CancellationTokenSource _pollCts;
        private int _pushLastPos;
        private int _pullLastPos;
        private bool _disposed;

        public bool IsRecording { get; private set; }
        public string DeviceName => _deviceName;
        public int SampleRate => _sampleRate;

        /// <summary>Fires every frame with new PCM samples.</summary>
        public event Action<float[]> SamplesAvailable;

        /// <summary>Fires when recording stops.</summary>
        public event Action RecordingStopped;
        public MicrophoneSource(
            string deviceName = null,
            int sampleRate = 16000,
            int clipLengthSec = 10,
            bool requestPermission = true)
        {
            _deviceName = deviceName;
            _sampleRate = sampleRate;
            _clipLengthSec = clipLengthSec;
            _requestPermission = requestPermission;
        }

        /// <summary>Requests permission and starts capture. Returns true on success.</summary>
        public async UniTask<bool> StartRecordingAsync(
            CancellationToken ct = default)
        {
            if (_disposed)
                return false;

            if (IsRecording)
                return true;

            if (_requestPermission && !await MicrophonePermission.RequestAsync())
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] MicrophoneSource: permission denied.");
                return false;
            }

            if (Microphone.devices.Length == 0)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: no devices found.");
                return false;
            }

            _clip = Microphone.Start(_deviceName, true, _clipLengthSec, _sampleRate);
            if (_clip == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] MicrophoneSource: Microphone.Start returned null.");
                return false;
            }

            IsRecording = true;
            _pushLastPos = 0;
            _pullLastPos = 0;

            _pollCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            PollLoopAsync(_pollCts.Token).Forget();

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] MicrophoneSource: started (device='{_deviceName ?? "default"}', rate={_sampleRate}).");
            return true;
        }

        /// <summary>Stops recording.</summary>
        public void StopRecording()
        {
            if (!IsRecording)
                return;

            CancelPollLoop();
            Microphone.End(_deviceName);
            IsRecording = false;

            SherpaOnnxLog.RuntimeLog("[SherpaOnnx] MicrophoneSource: stopped.");
            RecordingStopped?.Invoke();
        }

        /// <summary>Pull model: new samples since last call, or null.</summary>
        public float[] ReadNewSamples()
        {
            if (!IsRecording || _clip == null)
                return null;

            int currentPos = Microphone.GetPosition(_deviceName);
            float[] samples = ExtractSamples(ref _pullLastPos, currentPos);
            return samples;
        }

        /// <summary>Returns entire circular buffer, or null.</summary>
        public float[] ReadAllSamples()
        {
            if (!IsRecording || _clip == null)
                return null;

            var buffer = new float[_clip.samples * _clip.channels];
            _clip.GetData(buffer, 0);
            return buffer;
        }

        /// <summary>Stops recording and releases all resources.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopRecording();
            _clip = null;
        }

        private async UniTaskVoid PollLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsRecording)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (!IsRecording || _clip == null)
                    break;

                int currentPos = Microphone.GetPosition(_deviceName);
                float[] samples = ExtractSamples(ref _pushLastPos, currentPos);

                if (samples != null && samples.Length > 0)
                    SamplesAvailable?.Invoke(samples);
            }
        }

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
                // Wrap-around: read tail, then head
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
