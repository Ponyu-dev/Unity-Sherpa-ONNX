using System;

namespace PonyuDev.SherpaOnnx.Tts.Data
{
    /// <summary>
    /// Enable flags and pool sizes for TTS caching:
    /// raw audio data, AudioClips, and AudioSources.
    /// </summary>
    [Serializable]
    public sealed class TtsCacheSettings
    {
        /// <summary>Whether to cache generated float arrays.</summary>
        public bool offlineTtsEnabled = true;

        /// <summary>How many generated float arrays to keep in memory.</summary>
        public int offlineTtsPoolSize = 4;

        /// <summary>Whether to cache AudioClip objects.</summary>
        public bool audioClipEnabled = true;

        /// <summary>How many AudioClip objects to keep in the pool.</summary>
        public int audioClipPoolSize = 4;

        /// <summary>Whether to cache AudioSource objects for parallel playback.</summary>
        public bool audioSourceEnabled = true;

        /// <summary>How many AudioSource objects to keep for parallel playback.</summary>
        public int audioSourcePoolSize = 2;
    }
}
