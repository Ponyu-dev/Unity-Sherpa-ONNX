namespace PonyuDev.SherpaOnnx.Tts
{
    /// <summary>
    /// Playback mode for <c>GenerateAndPlay</c> on a non-pooled <see cref="UnityEngine.AudioSource"/>.
    /// </summary>
    public enum TtsPlaybackMode
    {
        /// <summary>
        /// Uses <c>AudioSource.PlayOneShot</c>. Multiple TTS clips can play
        /// simultaneously on the same source. Backwards-compatible default.
        /// <para/>
        /// Caveat: clip is destroyed after a duration computed at start time
        /// (<c>clip.length / source.pitch</c>). Mid-playback pitch changes
        /// may cut off audio early or leak briefly. Acceptable for short
        /// notification-style TTS lines.
        /// </summary>
        Overlap,

        /// <summary>
        /// Sets <c>AudioSource.clip</c> and calls <c>Play()</c>. A new clip
        /// interrupts the previous one on the same source. Polls
        /// <c>source.isPlaying</c>, so mid-playback pitch changes are
        /// tracked correctly. Recommended for chat-bot / dialogue UX where
        /// overlapping speech would be unintelligible.
        /// </summary>
        Exclusive,
    }
}
