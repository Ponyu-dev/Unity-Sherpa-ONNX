using System;
using System.Collections;
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

        // ── Private helpers ──

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
