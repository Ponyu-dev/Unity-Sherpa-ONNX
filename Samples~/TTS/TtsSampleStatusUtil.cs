using PonyuDev.SherpaOnnx.Tts;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Builds the bottom-status line shown by every TTS sample panel.
    /// Centralises the wording so panels stay consistent and don't
    /// duplicate a one-liner across the codebase. Combines live
    /// init-progress from <see cref="TtsInitProgressBus"/> with the
    /// service's runtime state.
    /// </summary>
    internal static class TtsSampleStatusUtil
    {
        public static string BuildCurrent(ITtsService tts)
        {
            if (!TtsInitProgressBus.HasFinished)
                return $"Initializing TTS — {TtsInitProgressBus.CurrentPercent}%";

            return BuildReady(tts);
        }

        public static string BuildReady(ITtsService tts)
        {
            if (tts == null)
                return "Service not available.";
            if (!tts.IsReady)
                return "TTS engine failed to load. See console for details.";

            var profile = tts.ActiveProfile;
            string profileName = profile != null
                ? profile.profileName
                : "(no profile)";
            return $"Ready • profile '{profileName}' • {tts.SampleRate} Hz";
        }
    }
}
