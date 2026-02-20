using System.Collections;
using System.Threading.Tasks;
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
        /// Creates a new AudioClip each time.
        /// </summary>
        public static TtsResult GenerateAndPlay(
            this ITtsService tts,
            string text,
            AudioSource source)
        {
            if (!ValidateArgs(tts, text, source))
                return null;

            var result = tts.Generate(text);
            PlayResult(result, source);
            return result;
        }

        /// <summary>
        /// Generates speech on a background thread and plays it.
        /// Creates a new AudioClip each time.
        /// </summary>
        public static async Task<TtsResult> GenerateAndPlayAsync(
            this ITtsService tts,
            string text,
            AudioSource source)
        {
            if (!ValidateArgs(tts, text, source))
                return null;

            var result = await tts.GenerateAsync(text);
            PlayResult(result, source);
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

        private static void PlayResult(TtsResult result, AudioSource source)
        {
            if (result == null || !result.IsValid)
                return;

            source.PlayOneShot(result.ToAudioClip());
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
                var fallback = cache.RentSource();
                if (fallback != null)
                {
                    fallback.PlayOneShot(result.ToAudioClip());
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
