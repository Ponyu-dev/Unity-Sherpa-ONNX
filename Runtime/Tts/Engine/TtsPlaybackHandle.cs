using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Engine
{
    /// <summary>
    /// Handle to an in-flight TTS playback. Returned by
    /// <c>GenerateAndPlayWithHandle</c> overloads; lets callers stop or
    /// fade out playback, observe completion, and clean up resources.
    /// <para/>
    /// Thread-safety: instance methods must be called from the Unity main
    /// thread. Internal monitor loop runs on <c>PlayerLoopTiming.Update</c>.
    /// </summary>
    public sealed class TtsPlaybackHandle : IDisposable
    {
        /// <summary>The generated audio result. Remains valid after stop.</summary>
        public TtsResult Result { get; }

        /// <summary>The AudioSource playing the clip (may be pooled or user-supplied).</summary>
        public AudioSource Source { get; }

        /// <summary>The AudioClip created for this playback.</summary>
        public AudioClip Clip { get; }

        /// <summary>True once playback has been stopped (naturally or explicitly).</summary>
        public bool IsStopped { get; private set; }

        /// <summary>True while audio is actively playing on the source.</summary>
        public bool IsPlaying =>
            !IsStopped
            && Source != null
            && Source.clip == Clip
            && Source.isPlaying;

        /// <summary>Fires once when playback ends naturally (not via Stop/StopAsync).</summary>
        public event Action Completed;

        /// <summary>Fires once when playback is explicitly stopped via Stop or StopAsync.</summary>
        public event Action Stopped;

        private readonly Action _cleanup;
        private readonly float _originalVolume;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        /// <summary>
        /// Created internally by extension methods; consumers receive
        /// pre-built handles from <c>GenerateAndPlayWithHandle</c>.
        /// </summary>
        /// <param name="result">Generated audio.</param>
        /// <param name="source">AudioSource to monitor.</param>
        /// <param name="clip">Clip set on the source (assumed already playing).</param>
        /// <param name="cleanup">
        /// Invoked exactly once when playback ends or is stopped. Caller
        /// decides what to do (destroy clip, return to pool, etc.).
        /// </param>
        internal TtsPlaybackHandle(
            TtsResult result,
            AudioSource source,
            AudioClip clip,
            Action cleanup)
        {
            Result = result;
            Source = source;
            Clip = clip;
            _cleanup = cleanup;
            _originalVolume = source != null ? source.volume : 1f;

            MonitorAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// Stops playback immediately and runs cleanup. Idempotent.
        /// </summary>
        public void Stop()
        {
            if (IsStopped)
                return;

            IsStopped = true;
            _cts.Cancel();

            if (Source != null)
            {
                Source.Stop();
                if (Source.clip == Clip)
                    Source.clip = null;
                Source.volume = _originalVolume;
            }

            try { Stopped?.Invoke(); } catch { /* user code, swallow */ }

            _cleanup?.Invoke();
        }

        /// <summary>
        /// Fades the source volume out over <paramref name="fadeSeconds"/>
        /// (unscaled time), then stops. If <paramref name="fadeSeconds"/>
        /// is &lt;= 0, behaves like <see cref="Stop"/>.
        /// </summary>
        public async UniTask StopAsync(float fadeSeconds)
        {
            if (IsStopped)
                return;

            if (fadeSeconds <= 0f)
            {
                Stop();
                return;
            }

            float startVolume = Source != null ? Source.volume : 0f;
            float t = 0f;
            var monitorCt = _cts.Token;

            while (t < fadeSeconds
                   && !monitorCt.IsCancellationRequested
                   && Source != null
                   && Source.clip == Clip
                   && Source.isPlaying)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeSeconds);

                // Exponential out — sounds natural for voice; linear is too abrupt.
                if (Source != null)
                    Source.volume = startVolume * Mathf.Pow(1f - k, 2f);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Stop();
        }

        /// <summary>
        /// Equivalent to <see cref="Stop"/>. Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Stop();
            _cts.Dispose();
        }

        private async UniTaskVoid MonitorAsync(CancellationToken ct)
        {
            try
            {
                // Yield once so the audio engine has a frame to register isPlaying.
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                while (!ct.IsCancellationRequested
                       && Source != null
                       && Source.clip == Clip
                       && Source.isPlaying)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                // Loop exited without cancellation → playback finished naturally
                // (or source destroyed externally / clip swapped).
                if (!ct.IsCancellationRequested && !IsStopped)
                {
                    IsStopped = true;

                    if (Source != null && Source.clip == Clip)
                        Source.clip = null;
                    if (Source != null)
                        Source.volume = _originalVolume;

                    try { Completed?.Invoke(); } catch { /* swallow */ }

                    _cleanup?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // Stop() was called; that path already runs cleanup.
            }
        }
    }
}
