using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Convenience extensions for <see cref="ITtsService"/>.
    /// Combines generation + playback in a single call.
    /// </summary>
    public static class TtsServiceExtensions
    {
        // ── Simple playback (no pool) ──

        /// <summary>
        /// Generates speech and plays it via <paramref name="source"/>.
        /// Creates a new AudioClip each time and disposes it after playback
        /// (no leak). See <see cref="TtsPlaybackMode"/> for behavior choice.
        /// </summary>
        public static TtsResult GenerateAndPlay(
            this ITtsService tts,
            string text,
            AudioSource source,
            TtsPlaybackMode mode = TtsPlaybackMode.Overlap)
        {
            if (!ValidateArgs(tts, text, source))
                return null;

            var result = tts.Generate(text);
            PlayResult(result, source, mode);
            return result;
        }

        /// <summary>
        /// Generates speech on a background thread and plays it.
        /// Creates a new AudioClip each time and disposes it after playback
        /// (no leak). See <see cref="TtsPlaybackMode"/> for behavior choice.
        /// </summary>
        public static async Task<TtsResult> GenerateAndPlayAsync(
            this ITtsService tts,
            string text,
            AudioSource source,
            TtsPlaybackMode mode = TtsPlaybackMode.Overlap)
        {
            if (!ValidateArgs(tts, text, source))
                return null;

            var result = await tts.GenerateAsync(text);
            PlayResult(result, source, mode);
            return result;
        }

        // ── Pooled playback (with cache) ──

        /// <summary>
        /// Generates speech and plays it using pooled AudioClip
        /// and AudioSource from <paramref name="cache"/>.
        /// Clip and source are returned to pool when playback ends.
        /// Requires a MonoBehaviour <paramref name="owner"/> for
        /// the return coroutine.
        /// </summary>
        public static TtsResult GenerateAndPlay(
            this ITtsService tts,
            string text,
            ITtsCacheControl cache,
            MonoBehaviour owner)
        {
            if (!ValidateArgs(tts, text, cache, owner))
                return null;

            var result = tts.Generate(text);
            PlayPooled(result, cache, owner);
            return result;
        }

        /// <summary>
        /// Generates speech on a background thread and plays it
        /// using pooled AudioClip and AudioSource.
        /// Clip and source are returned to pool when playback ends.
        /// </summary>
        public static async Task<TtsResult> GenerateAndPlayAsync(
            this ITtsService tts,
            string text,
            ITtsCacheControl cache,
            MonoBehaviour owner)
        {
            if (!ValidateArgs(tts, text, cache, owner))
                return null;

            var result = await tts.GenerateAsync(text);
            PlayPooled(result, cache, owner);
            return result;
        }

        // ── Handle-based playback (cancellable, stoppable, fadeable) ──

        /// <summary>
        /// Generates speech and starts playback on <paramref name="source"/>,
        /// returning a <see cref="TtsPlaybackHandle"/> that lets the caller
        /// stop, fade out, or observe completion.
        /// <para/>
        /// Always uses Exclusive playback (sets <c>source.clip</c> + Play),
        /// because handle-based control over an overlapping voice is not
        /// well-defined. Use the <c>TtsResult</c>-returning overload with
        /// <see cref="TtsPlaybackMode.Overlap"/> if you want PlayOneShot.
        /// <para/>
        /// Returns null if the service is not ready or generation fails.
        /// Throws <see cref="OperationCanceledException"/> if cancelled
        /// before playback starts.
        /// </summary>
        public static async Task<TtsPlaybackHandle> GenerateAndPlayWithHandleAsync(
            this ITtsService tts,
            string text,
            AudioSource source,
            CancellationToken ct = default)
        {
            if (!ValidateArgs(tts, text, source))
                return null;

            var result = await tts.GenerateAsync(text, ct);
            if (result == null || !result.IsValid)
                return null;

            return BuildHandle(result, source);
        }

        /// <summary>
        /// Generates speech and starts playback on a pooled AudioSource from
        /// the cache, returning a <see cref="TtsPlaybackHandle"/>. Clip and
        /// source are returned to the pool when playback ends or is stopped.
        /// </summary>
        public static async Task<TtsPlaybackHandle> GenerateAndPlayWithHandleAsync(
            this ITtsService tts,
            string text,
            ITtsCacheControl cache,
            CancellationToken ct = default)
        {
            if (!ValidateArgs(tts, text, cache))
                return null;

            var result = await tts.GenerateAsync(text, ct);
            if (result == null || !result.IsValid)
                return null;

            var clip = cache.RentClip(result);
            var source = cache.RentSource();

            // If pool is exhausted, return clip and bail; caller can retry.
            if (source == null)
            {
                if (clip != null) cache.ReturnClip(clip);
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlayWithHandleAsync: " +
                    "no AudioSource available in pool.");
                return null;
            }

            // If clip pool is disabled or full, fall back to a one-off clip
            // and destroy it on cleanup. Source still goes back to pool.
            bool clipFromPool = clip != null;
            if (!clipFromPool)
                clip = result.ToAudioClip();

            source.clip = clip;
            source.Play();

            Action cleanup = () => ReturnPooledOrDestroy(cache, source, clip, clipFromPool);

            return new TtsPlaybackHandle(result, source, clip, cleanup);
        }

        // ── Streaming playback (cancellable, low-latency) ──

        /// <summary>
        /// Streams generated audio into a stream-mode AudioClip on
        /// <paramref name="source"/>. Returns a <see cref="TtsPlaybackHandle"/>
        /// as soon as the first chunk arrives — first audio plays in
        /// roughly the latency of one sherpa-onnx callback (~few hundred ms),
        /// instead of waiting for the full generation to complete.
        /// <para/>
        /// <see cref="TtsPlaybackHandle.Stop"/> cancels the underlying
        /// generation and stops playback in one call.
        /// <para/>
        /// Returns null if the service is not ready or the engine sample
        /// rate is unknown.
        /// </summary>
        public static async UniTask<TtsPlaybackHandle> SpeakStreamingAsync(
            this ITtsService tts,
            string text,
            AudioSource source,
            CancellationToken ct = default)
        {
            if (!ValidateArgs(tts, text, source))
                return null;

            int sampleRate = tts.SampleRate;
            if (sampleRate <= 0)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] SpeakStreamingAsync: engine sample rate is 0 " +
                    "(engine not loaded). Falling back to GenerateAndPlayWithHandleAsync.");
                return await tts.GenerateAndPlayWithHandleAsync(text, source, ct);
            }

            return await BuildStreamingHandle(tts, text, source, sampleRate, ct);
        }

        /// <summary>
        /// Streaming variant of <see cref="GenerateAndPlayWithHandleAsync"/>
        /// that uses a pooled AudioSource from the cache. Source is returned
        /// to the pool when the handle stops or completes.
        /// </summary>
        public static async UniTask<TtsPlaybackHandle> SpeakStreamingAsync(
            this ITtsService tts,
            string text,
            ITtsCacheControl cache,
            CancellationToken ct = default)
        {
            if (!ValidateArgs(tts, text, cache))
                return null;

            int sampleRate = tts.SampleRate;
            if (sampleRate <= 0)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] SpeakStreamingAsync: engine sample rate is 0.");
                return null;
            }

            var source = cache.RentSource();
            if (source == null)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] SpeakStreamingAsync: no AudioSource in pool.");
                return null;
            }

            var handle = await BuildStreamingHandle(
                tts, text, source, sampleRate, ct,
                onCleanup: () => cache.ReturnSource(source));

            if (handle == null)
                cache.ReturnSource(source);

            return handle;
        }

        // ── Sentence-queue speak ──

        /// <summary>
        /// Splits <paramref name="text"/> into sentences and plays them
        /// sequentially on <paramref name="source"/>. A sliding window of
        /// <paramref name="lookAhead"/> sentence-generations runs ahead of
        /// playback, so transitions are near-seamless.
        /// <para/>
        /// First-audio latency is the time to generate just sentence 1 — much
        /// less than waiting for the whole text. Works on any model regardless
        /// of whether the model itself emits per-chunk callbacks (unlike
        /// <see cref="SpeakStreamingAsync"/>, which depends on model-level
        /// chunking).
        /// <para/>
        /// <paramref name="lookAhead"/> sets how many sentences are pre-generated
        /// in parallel with playback. Default 1 is enough when generation is
        /// noticeably faster than playback (typical on desktop). Bump to 2-4
        /// for heavier models (Matcha, Kokoro, voice-cloning) or slower
        /// hardware (Android), where gen time approaches playback time —
        /// otherwise sentence transitions get audible gaps. Real parallelism
        /// also requires <c>EnginePoolSize &gt;= lookAhead</c>; with pool
        /// size 1 the extra pre-gens just queue serially and don't help.
        /// <para/>
        /// <paramref name="onHandleStarted"/> fires once per sentence right
        /// before its handle starts polling for completion. Use it to track
        /// handles externally (e.g. <c>TtsOrchestrator.Track</c> for StopAll
        /// integration) or to measure first-audio latency.
        /// </summary>
        public static async UniTask Speak(
            this ITtsService tts,
            string text,
            AudioSource source,
            CancellationToken ct = default,
            Action<TtsPlaybackHandle> onHandleStarted = null,
            int lookAhead = 1)
        {
            if (tts == null || !tts.IsReady || source == null)
                return;
            if (string.IsNullOrWhiteSpace(text))
                return;
            if (lookAhead < 1) lookAhead = 1;

            var sentences = new List<string>();
            foreach (var s in SentenceSplitter.Split(text))
                sentences.Add(s);
            if (sentences.Count == 0)
                return;

            // Sliding window of pre-gen tasks. Refilled one-at-a-time as
            // each playback consumes a slot — keeps the window full but
            // never pre-gens past the end of the text.
            var pending = new Queue<Task<TtsResult>>();
            int nextToEnqueue = 0;
            int prefill = Math.Min(lookAhead, sentences.Count);
            while (nextToEnqueue < prefill)
            {
                pending.Enqueue(tts.GenerateAsync(sentences[nextToEnqueue], ct));
                nextToEnqueue++;
            }

            for (int i = 0; i < sentences.Count; i++)
            {
                var currentGen = pending.Dequeue();

                // Refill the window: enqueue the next pre-gen if any
                // sentences remain unqueued.
                if (nextToEnqueue < sentences.Count)
                {
                    pending.Enqueue(tts.GenerateAsync(sentences[nextToEnqueue], ct));
                    nextToEnqueue++;
                }

                TtsResult result;
                try { result = await currentGen; }
                catch (OperationCanceledException) { return; }

                if (result != null && result.IsValid)
                {
                    bool finished = await PlayResultAndWaitAsync(
                        result, source, ct, onHandleStarted);
                    if (!finished)
                        return; // cancelled or stopped externally
                }
            }
        }

        // ── Private helpers ──

        /// <summary>
        /// Wraps a generated <see cref="TtsResult"/> into a handle for
        /// non-pooled playback. Clip is destroyed by the handle's cleanup.
        /// </summary>
        private static TtsPlaybackHandle BuildHandle(TtsResult result, AudioSource source)
        {
            var clip = result.ToAudioClip();
            source.clip = clip;
            source.Play();

            Action cleanup = () => DestroyClip(clip);

            return new TtsPlaybackHandle(result, source, clip, cleanup);
        }

        /// <summary>
        /// Builds a streaming handle: creates a stream-mode AudioClip,
        /// starts generation in the background, and returns once the first
        /// chunk has been queued so playback can begin without leading
        /// silence.
        /// </summary>
        /// <param name="onCleanup">
        /// Optional extra cleanup (e.g. return pooled source). Runs after
        /// the standard stream + CTS cleanup.
        /// </param>
        private static async UniTask<TtsPlaybackHandle> BuildStreamingHandle(
            ITtsService tts,
            string text,
            AudioSource source,
            int sampleRate,
            CancellationToken ct,
            Action onCleanup = null)
        {
            // 1-second internal buffer is plenty for sherpa-onnx chunk sizes.
            var stream = new StreamingTtsClip(sampleRate, sampleRate);

            // Internal CTS so handle.Stop() can also abort the underlying
            // generation, not just stop the audio.
            var genCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var profile = tts.ActiveProfile;
            float speed = profile?.speed ?? 1f;
            int speakerId = profile?.speakerId ?? 0;

            var genTask = tts.GenerateWithCallbackAsync(
                text, speed, speakerId, MakeStreamPump(stream), genCts.Token);

            // Fire-and-forget completion observer marks the stream done
            // once gen finishes (either normally or with an error/cancel).
            ObserveGeneration(genTask, stream).Forget();

            // Wait for the first chunk before we start playing — avoids
            // a leading silent gap.
            try
            {
                await UniTask.WaitUntil(
                    () => stream.HasFirstChunk || stream.IsGenerationComplete,
                    cancellationToken: genCts.Token);
            }
            catch (OperationCanceledException)
            {
                stream.Dispose();
                genCts.Dispose();
                return null;
            }

            if (!stream.HasFirstChunk)
            {
                // Generation finished with zero output — abort.
                stream.Dispose();
                genCts.Dispose();
                return null;
            }

            // Stream-mode clips need source.loop = true: by default a
            // non-looping AudioSource STOPS itself after playing through the
            // clip's lengthSamples (our 1-second buffer), even though the
            // PCMReaderCallback would keep delivering data. With loop=true,
            // Unity keeps calling the callback until WE explicitly stop —
            // which happens via the handle's monitor when IsDrained flips.
            // Save the previous value so cleanup can restore it for pool reuse.
            bool previousLoop = source.loop;
            source.clip = stream.Clip;
            source.loop = true;
            source.Play();

            Action cleanup = () =>
            {
                try { genCts.Cancel(); } catch { /* swallow */ }
                genCts.Dispose();
                stream.Dispose();
                if (source != null)
                    source.loop = previousLoop;
                onCleanup?.Invoke();
            };

            return new TtsPlaybackHandle(
                result: null,
                source: source,
                clip: stream.Clip,
                cleanup: cleanup,
                isCompleteCheck: () => stream.IsDrained);
        }

        /// <summary>
        /// Builds the per-call chunk pump callback that pipes sherpa-onnx
        /// audio chunks into the stream's queue. Returns 1 to keep generation
        /// going (cancellation is handled at the gen-task level via CTS).
        /// </summary>
        private static TtsCallback MakeStreamPump(StreamingTtsClip stream)
        {
            return (samples, n) =>
            {
                stream.AppendChunk(samples);
                return 1;
            };
        }

        /// <summary>
        /// Awaits the generation task and marks the stream complete once
        /// it returns. Cancellation and errors both flip the flag — the
        /// monitor loop then drains the queue and stops the source.
        /// </summary>
        private static async UniTaskVoid ObserveGeneration(
            Task<TtsResult> genTask, StreamingTtsClip stream)
        {
            try { await genTask; }
            catch { /* swallow — finally still flips the completion flag */ }
            finally { stream.Complete(); }
        }

        /// <summary>
        /// Cleanup: destroys a one-off AudioClip if still alive.
        /// Used as the cleanup action for non-pooled handles.
        /// </summary>
        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null)
                UnityEngine.Object.Destroy(clip);
        }

        /// <summary>
        /// Plays a single TtsResult on the given source via a fresh
        /// TtsPlaybackHandle, waits for completion or cancellation.
        /// Returns true on natural completion, false if cancelled or stopped
        /// externally (used by <see cref="Speak"/> to abort the queue early).
        /// </summary>
        private static async UniTask<bool> PlayResultAndWaitAsync(
            TtsResult result,
            AudioSource source,
            CancellationToken ct,
            Action<TtsPlaybackHandle> onHandleStarted)
        {
            var clip = result.ToAudioClip();
            source.clip = clip;
            source.Play();

            bool externallyStopped = false;
            void OnStopped() => externallyStopped = true;

            var handle = new TtsPlaybackHandle(
                result, source, clip,
                cleanup: () => DestroyClip(clip));
            handle.Stopped += OnStopped;

            // Hand the handle to the caller (e.g. orchestrator's Track) so
            // StopAll / external observers can act on it before we await.
            onHandleStarted?.Invoke(handle);

            try
            {
                await UniTask.WaitUntil(
                    () => handle.IsStopped, cancellationToken: ct);
                return !externallyStopped;
            }
            catch (OperationCanceledException)
            {
                handle.Dispose();
                return false;
            }
            finally
            {
                handle.Stopped -= OnStopped;
            }
        }

        /// <summary>
        /// Cleanup: returns a pooled AudioSource and either returns the clip
        /// to its pool or destroys it (depending on whether the clip itself
        /// came from the pool). Used as the cleanup action for pooled handles.
        /// </summary>
        private static void ReturnPooledOrDestroy(
            ITtsCacheControl cache,
            AudioSource source,
            AudioClip clip,
            bool clipFromPool)
        {
            cache.ReturnSource(source);
            if (clipFromPool)
                cache.ReturnClip(clip);
            else if (clip != null)
                UnityEngine.Object.Destroy(clip);
        }

        private static bool ValidateArgs(
            ITtsService tts, string text, ITtsCacheControl cache)
        {
            if (tts == null || !tts.IsReady)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlayWithHandle: service not ready.");
                return false;
            }

            if (string.IsNullOrEmpty(text))
                return false;

            if (cache == null)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlayWithHandle: cache is null.");
                return false;
            }

            return true;
        }


        private static void PlayResult(
            TtsResult result, AudioSource source, TtsPlaybackMode mode)
        {
            if (result == null || !result.IsValid || source == null)
                return;

            var clip = result.ToAudioClip();

            if (mode == TtsPlaybackMode.Overlap)
            {
                source.PlayOneShot(clip);
                ScheduleDestroyAfterDuration(clip, source);
            }
            else
            {
                PlayExclusiveAndDisposeAsync(source, clip).Forget();
            }
        }

        /// <summary>
        /// Overlap-mode cleanup: fire UniTask delay sized to the captured
        /// clip duration (pitch-adjusted). Uses unscaled time so timeScale=0
        /// doesn't strand the destroy. Visible in profiler, unlike the
        /// built-in Object.Destroy(obj, t) timer.
        /// </summary>
        private static void ScheduleDestroyAfterDuration(
            AudioClip clip, AudioSource source)
        {
            float pitch = source != null ? source.pitch : 1f;
            float seconds = clip.length / Mathf.Max(0.01f, pitch) + 0.1f;
            DestroyClipAfterAsync(clip, seconds).Forget();
        }

        private static async UniTaskVoid DestroyClipAfterAsync(
            AudioClip clip, float seconds)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(seconds),
                DelayType.UnscaledDeltaTime);

            if (clip != null)
                UnityEngine.Object.Destroy(clip);
        }

        /// <summary>
        /// Exclusive-mode playback: assigns clip, plays, polls until done,
        /// then nulls the slot and destroys the clip. Reacts to early Stop,
        /// source destruction, and clip swaps without leaking.
        /// </summary>
        private static async UniTaskVoid PlayExclusiveAndDisposeAsync(
            AudioSource source, AudioClip clip)
        {
            source.clip = clip;
            source.Play();

            while (source != null
                   && source.clip == clip
                   && source.isPlaying)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (source != null && source.clip == clip)
                source.clip = null;

            if (clip != null)
                UnityEngine.Object.Destroy(clip);
        }

        private static void PlayPooled(
            TtsResult result,
            ITtsCacheControl cache,
            MonoBehaviour owner)
        {
            if (result == null || !result.IsValid)
                return;

            var clip = cache.RentClip(result);
            if (clip == null)
            {
                // Pool unavailable — fallback to non-pooled source.
                // Preserves PlayOneShot semantics; clip is destroyed via
                // UniTask delay (see ScheduleDestroyAfterDuration).
                var fallback = cache.RentSource();
                if (fallback != null)
                {
                    var fallbackClip = result.ToAudioClip();
                    fallback.PlayOneShot(fallbackClip);
                    ScheduleDestroyAfterDuration(fallbackClip, fallback);
                    owner.StartCoroutine(
                        ReturnSourceWhenDone(fallback, null, cache, owner));
                }
                return;
            }

            var source = cache.RentSource();
            if (source == null)
            {
                cache.ReturnClip(clip);
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] No AudioSource available in pool.");
                return;
            }

            source.clip = clip;
            source.Play();
            owner.StartCoroutine(
                ReturnSourceWhenDone(source, clip, cache, owner));
        }

        private static IEnumerator ReturnSourceWhenDone(
            AudioSource source,
            AudioClip clip,
            ITtsCacheControl cache,
            MonoBehaviour owner)
        {
            yield return new WaitWhile(() =>
                owner != null && source != null && source.isPlaying);

            if (source != null)
                cache.ReturnSource(source);

            if (clip != null)
                cache.ReturnClip(clip);
        }

        private static bool ValidateArgs(
            ITtsService tts, string text, AudioSource source)
        {
            if (tts == null || !tts.IsReady)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlay: service not ready.");
                return false;
            }

            if (string.IsNullOrEmpty(text))
                return false;

            if (source == null)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlay: AudioSource is null.");
                return false;
            }

            return true;
        }

        private static bool ValidateArgs(
            ITtsService tts,
            string text,
            ITtsCacheControl cache,
            MonoBehaviour owner)
        {
            if (tts == null || !tts.IsReady)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlay: service not ready.");
                return false;
            }

            if (string.IsNullOrEmpty(text))
                return false;

            if (cache == null)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlay: cache is null.");
                return false;
            }

            if (owner == null)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] GenerateAndPlay: owner is null.");
                return false;
            }

            return true;
        }
    }
}
