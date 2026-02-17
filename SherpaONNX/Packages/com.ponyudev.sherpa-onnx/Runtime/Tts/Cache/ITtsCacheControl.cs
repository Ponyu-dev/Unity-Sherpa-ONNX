using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Cache
{
    /// <summary>
    /// Explicit cache management: enable/disable at runtime,
    /// rent/return pooled objects, inspect counts, clear caches.
    /// </summary>
    public interface ITtsCacheControl
    {
        // ── Enable / disable at runtime ──

        /// <summary>Enable or disable TtsResult memoization. Clears on disable.</summary>
        bool ResultCacheEnabled { get; set; }

        /// <summary>Enable or disable AudioClip pooling. Clears on disable.</summary>
        bool AudioClipPoolEnabled { get; set; }

        /// <summary>Enable or disable AudioSource pooling. Clears on disable.</summary>
        bool AudioSourcePoolEnabled { get; set; }

        // ── Sizes ──

        /// <summary>Maximum number of memoized results. Evicts LRU on shrink.</summary>
        int ResultCacheMaxSize { get; set; }

        /// <summary>Maximum number of pooled AudioClips.</summary>
        int AudioClipPoolMaxSize { get; set; }

        /// <summary>Maximum number of pooled AudioSources. Creates/destroys GameObjects.</summary>
        int AudioSourcePoolMaxSize { get; set; }

        // ── Counts ──

        /// <summary>Number of memoized TtsResult entries.</summary>
        int ResultCacheCount { get; }

        /// <summary>Number of available AudioClips in the pool.</summary>
        int AudioClipAvailableCount { get; }

        /// <summary>Number of idle AudioSources in the pool.</summary>
        int AudioSourceAvailableCount { get; }

        // ── Clear ──

        /// <summary>Clear all caches (results, clips, sources).</summary>
        void ClearAll();

        /// <summary>Clear only the TtsResult cache.</summary>
        void ClearResultCache();

        /// <summary>Clear only the AudioClip pool.</summary>
        void ClearClipPool();

        /// <summary>Clear only the AudioSource pool.</summary>
        void ClearSourcePool();

        /// <summary>
        /// Rent an AudioClip from the pool, sized for the given result.
        /// Data is written via SetData.
        /// </summary>
        AudioClip RentClip(TtsResult result);

        /// <summary>Return an AudioClip to the pool.</summary>
        void ReturnClip(AudioClip clip);

        /// <summary>Rent an idle AudioSource for playback.</summary>
        AudioSource RentSource();

        /// <summary>Return an AudioSource (stops playback, clears clip).</summary>
        void ReturnSource(AudioSource source);
    }
}
