using System;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Vad.Engine;

namespace PonyuDev.SherpaOnnx.Vad
{
    /// <summary>
    /// Combines <see cref="IVadService"/> and <see cref="IAsrService"/>:
    /// VAD filters silence, speech segments are fed to ASR.
    /// Saves resources — ASR runs only when speech is detected.
    /// Pure POCO, suitable for VContainer DI or manual instantiation.
    /// </summary>
    public sealed class VadAsrPipeline : IDisposable
    {
        private readonly IVadService _vad;
        private readonly IAsrService _asr;
        private readonly int _sampleRate;

        private float[] _ringBuffer;
        private int _ringPos;

        /// <summary>Fires when ASR produces a result from a speech segment.</summary>
        public event Action<AsrResult> OnResult;

        /// <summary>Fires when speech starts (passthrough from VAD).</summary>
        public event Action OnSpeechStart;

        /// <summary>Fires when speech ends (passthrough from VAD).</summary>
        public event Action OnSpeechEnd;

        public bool IsReady => _vad.IsReady && _asr.IsReady;

        /// <summary>
        /// Window size in samples. Feed audio in chunks of this size
        /// or use <see cref="AcceptSamples"/> which handles buffering.
        /// </summary>
        public int WindowSize => _vad.WindowSize;

        public VadAsrPipeline(IVadService vad, IAsrService asr)
        {
            _vad = vad ?? throw new ArgumentNullException(nameof(vad));
            _asr = asr ?? throw new ArgumentNullException(nameof(asr));
            _sampleRate = vad.ActiveProfile?.sampleRate ?? 16000;

            _vad.OnSpeechStart += HandleSpeechStart;
            _vad.OnSpeechEnd += HandleSpeechEnd;
            _vad.OnSegment += HandleSegment;
        }

        /// <summary>
        /// Feed arbitrary-length PCM samples. Internally buffers
        /// and sends window-sized chunks to VAD.
        /// </summary>
        public void AcceptSamples(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return;

            if (!IsReady)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] VadAsrPipeline: not ready. Initialize both VAD and ASR first.");
                return;
            }

            int windowSize = _vad.WindowSize;

            if (windowSize <= 0)
                return;

            EnsureRingBuffer(windowSize);

            for (int i = 0; i < samples.Length; i++)
            {
                _ringBuffer[_ringPos++] = samples[i];

                if (_ringPos < windowSize)
                    continue;
                
                _vad.AcceptWaveform(_ringBuffer);
                _ringPos = 0;
            }
        }

        /// <summary>
        /// Flushes VAD buffer and recognizes any remaining speech.
        /// Call when recording stops.
        /// </summary>
        public void Flush()
        {
            _vad.Flush();
        }

        /// <summary>Resets both VAD state and the internal ring buffer.</summary>
        public void Reset()
        {
            _vad.Reset();
            _ringPos = 0;
        }

        public void Dispose()
        {
            _vad.OnSpeechStart -= HandleSpeechStart;
            _vad.OnSpeechEnd -= HandleSpeechEnd;
            _vad.OnSegment -= HandleSegment;

            OnResult = null;
            OnSpeechStart = null;
            OnSpeechEnd = null;
        }

        // ── Event handlers ──

        private void HandleSpeechStart()
        {
            OnSpeechStart?.Invoke();
        }

        private void HandleSpeechEnd()
        {
            OnSpeechEnd?.Invoke();
        }

        private void HandleSegment(VadSegment segment)
        {
            if (segment?.Samples == null || segment.Samples.Length == 0)
                return;

            var result = _asr.Recognize(segment.Samples, _sampleRate);

            if (result != null && result.IsValid)
                OnResult?.Invoke(result);
        }

        /// <summary>
        /// Async variant: recognizes the segment on a background thread.
        /// </summary>
        public async Task HandleSegmentAsync(VadSegment segment)
        {
            if (segment?.Samples == null || segment.Samples.Length == 0)
                return;

            var result = await _asr.RecognizeAsync(
                segment.Samples, _sampleRate);

            if (result != null && result.IsValid)
                OnResult?.Invoke(result);
        }

        // ── Private ──

        private void EnsureRingBuffer(int windowSize)
        {
            if (_ringBuffer != null && _ringBuffer.Length == windowSize)
                return;

            _ringBuffer = new float[windowSize];
            _ringPos = 0;
        }
    }
}
