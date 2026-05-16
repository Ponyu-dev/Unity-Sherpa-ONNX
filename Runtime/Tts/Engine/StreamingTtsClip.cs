using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Wraps a <see cref="UnityEngine.AudioClip"/> created in stream mode
    /// (<c>AudioClip.Create(..., stream: true, pcmReaderCallback)</c>) plus
    /// a thread-safe chunk queue. Producer threads call <see cref="AppendChunk"/>
    /// (sherpa-onnx fires its callback on a worker thread); Unity's audio
    /// thread drains the queue via the PCM reader callback.
    /// <para/>
    /// Lifecycle:
    /// 1. Construct with sample rate + initial buffer length.
    /// 2. Producer enqueues chunks until generation is done, then calls <see cref="Complete"/>.
    /// 3. <see cref="IsDrained"/> goes true once both <c>generationComplete</c> AND
    ///    the queue + current-chunk are empty — caller should then stop the source.
    /// 4. Caller invokes <see cref="Dispose"/> to destroy the AudioClip.
    /// </summary>
    public sealed class StreamingTtsClip : IDisposable
    {
        private readonly ConcurrentQueue<float[]> _chunks = new();
        private readonly object _readerLock = new();

        private float[] _currentChunk;
        private int _currentChunkOffset;
        private long _samplesProduced;
        private long _samplesConsumed;

        private volatile bool _generationComplete;
        private volatile bool _disposed;

        // Tail latency: Unity buffers ~50-100ms of audio ahead of what's
        // actually heard. If we report "drained" the instant the queue is
        // empty, Source.Stop() fires before the last chunk is mixed to the
        // output, cutting playback off mid-word. Hold drained status for
        // this long after first observation so trailing audio finishes.
        private const float TailLatencySeconds = 0.25f;
        private float? _emptyObservedAtUnscaledTime;

        /// <summary>Underlying Unity AudioClip in stream mode.</summary>
        public AudioClip Clip { get; }

        /// <summary>Sample rate in Hz the producer commits to feed.</summary>
        public int SampleRate { get; }

        /// <summary>Number of mono samples appended so far (cumulative).</summary>
        public long SamplesProduced => System.Threading.Interlocked.Read(ref _samplesProduced);

        /// <summary>Number of mono samples consumed by the audio thread.</summary>
        public long SamplesConsumed => System.Threading.Interlocked.Read(ref _samplesConsumed);

        /// <summary>True after producer signals end-of-stream via <see cref="Complete"/>.</summary>
        public bool IsGenerationComplete => _generationComplete;

        /// <summary>True when there is at least one chunk available to play.</summary>
        public bool HasFirstChunk => _chunks.Count > 0 || _currentChunk != null;

        /// <summary>
        /// True once generation finished AND every queued sample has been read
        /// AND the tail-latency window (250 ms) has elapsed so Unity finishes
        /// flushing its internal audio buffer. Call from main thread only —
        /// uses <c>Time.unscaledTime</c>.
        /// <para/>
        /// Use this to know when to call <c>source.Stop()</c> on a streaming clip
        /// (Unity never reports <c>isPlaying = false</c> for stream clips).
        /// </summary>
        public bool IsDrained
        {
            get
            {
                bool queueEmpty =
                    _generationComplete
                    && _chunks.IsEmpty
                    && (_currentChunk == null
                        || _currentChunkOffset >= _currentChunk.Length);

                if (!queueEmpty)
                {
                    // Reset the tail timer in case more chunks arrive late.
                    _emptyObservedAtUnscaledTime = null;
                    return false;
                }

                if (!_emptyObservedAtUnscaledTime.HasValue)
                    _emptyObservedAtUnscaledTime = Time.unscaledTime;

                return Time.unscaledTime - _emptyObservedAtUnscaledTime.Value
                    >= TailLatencySeconds;
            }
        }

        /// <param name="sampleRate">Mono sample rate (e.g. 22050).</param>
        /// <param name="bufferLengthSamples">
        /// Internal Unity loop length. Should be >= one PCM read window
        /// (typically 1024-8192 samples). 1 second of audio is a safe default.
        /// </param>
        /// <param name="clipName">Optional debug name for the AudioClip.</param>
        public StreamingTtsClip(
            int sampleRate,
            int bufferLengthSamples,
            string clipName = "tts-streaming")
        {
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (bufferLengthSamples <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferLengthSamples));

            SampleRate = sampleRate;
            Clip = AudioClip.Create(
                clipName,
                bufferLengthSamples,
                channels: 1,
                frequency: sampleRate,
                stream: true,
                OnAudioRead);
        }

        /// <summary>
        /// Append a chunk of PCM mono samples. Thread-safe.
        /// Called from sherpa-onnx's worker thread inside its callback.
        /// Empty / null chunks are ignored.
        /// </summary>
        public void AppendChunk(float[] samples)
        {
            if (_disposed || samples == null || samples.Length == 0)
                return;

            // Defensive copy: caller may reuse the buffer after the call.
            var copy = new float[samples.Length];
            Buffer.BlockCopy(samples, 0, copy, 0, samples.Length * sizeof(float));

            _chunks.Enqueue(copy);
            System.Threading.Interlocked.Add(ref _samplesProduced, samples.Length);
        }

        /// <summary>
        /// Marks generation as complete. After this, once the queue drains,
        /// <see cref="IsDrained"/> becomes true.
        /// </summary>
        public void Complete()
        {
            _generationComplete = true;
        }

        /// <summary>
        /// PCMReader callback — called on Unity's audio thread asking for
        /// <c>data.Length</c> samples. Drains the queue; on under-run writes
        /// silence so playback continues without crashing.
        /// </summary>
        private void OnAudioRead(float[] data)
        {
            lock (_readerLock)
            {
                int written = 0;

                while (written < data.Length)
                {
                    if (_currentChunk == null
                        || _currentChunkOffset >= _currentChunk.Length)
                    {
                        if (!_chunks.TryDequeue(out _currentChunk))
                        {
                            // Under-run: fill remaining buffer with silence.
                            // Producer is either generating still or generation
                            // is done and we'll be drained on the next call.
                            for (int i = written; i < data.Length; i++)
                                data[i] = 0f;
                            return;
                        }
                        _currentChunkOffset = 0;
                    }

                    int available = _currentChunk.Length - _currentChunkOffset;
                    int needed = data.Length - written;
                    int copy = available < needed ? available : needed;

                    Array.Copy(
                        _currentChunk, _currentChunkOffset,
                        data, written,
                        copy);

                    _currentChunkOffset += copy;
                    written += copy;
                }

                System.Threading.Interlocked.Add(ref _samplesConsumed, data.Length);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // Drop pending chunks so the GC can reclaim them.
            while (_chunks.TryDequeue(out _)) { }
            _currentChunk = null;

            if (Clip != null)
                UnityEngine.Object.Destroy(Clip);
        }
    }
}
