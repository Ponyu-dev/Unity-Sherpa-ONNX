using System;
using System.Collections;
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
        /// Cleanup: destroys a one-off AudioClip if still alive.
        /// Used as the cleanup action for non-pooled handles.
        /// </summary>
        private static void DestroyClip(AudioClip clip)
        {
            if (clip != null)
                UnityEngine.Object.Destroy(clip);
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
