using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Tts;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Builds the bottom-status line shown by every TTS sample panel.
    /// Reads the latest <see cref="ProfileReadyEvent"/> from
    /// <see cref="TtsInitProgressBus"/> and turns it into one line
    /// per phase (Download / Extract / Init / Ready / Failed).
    /// </summary>
    internal static class TtsSampleStatusUtil
    {
        public static string BuildCurrent(ITtsService tts)
        {
            if (TtsInitProgressBus.IsReady)
                return BuildReady(tts);

            if (!TtsInitProgressBus.HasEvent)
                return "Initializing TTS…";

            return BuildPhaseText(TtsInitProgressBus.LastEvent, "TTS");
        }

        public static string BuildReady(ITtsService tts)
        {
            if (tts == null)
                return "Service not available.";
            if (!tts.IsReady)
                return "TTS engine failed to load. See console for details.";

            var profile = tts.ActiveProfile;
            string profileName = profile != null ? profile.profileName : "(no profile)";
            return $"Ready • profile '{profileName}' • {tts.SampleRate} Hz";
        }

        /// <summary>
        /// Translates one <see cref="ProfileReadyEvent"/> into a
        /// human-readable status line. <paramref name="engineName"/>
        /// is the prefix shown to the user (e.g. "TTS",
        /// "offline ASR", "VAD").
        /// </summary>
        public static string BuildPhaseText(ProfileReadyEvent e, string engineName)
        {
            switch (e.Phase)
            {
                case ProfileReadyPhase.Download:
                    return $"{engineName} • downloading {e.Percent}%";
                case ProfileReadyPhase.DownloadRetrying:
                    return $"{engineName} • network issue, retrying ({e.RetryAttempt})…";
                case ProfileReadyPhase.Extract:
                    return $"{engineName} • extracting {e.Percent}%";
                case ProfileReadyPhase.Init:
                    return $"{engineName} • initializing engine {e.Percent}%";
                case ProfileReadyPhase.Failed:
                    return $"{engineName} • failed: {e.Message ?? "unknown error"}";
                case ProfileReadyPhase.Ready:
                    return $"{engineName} • ready";
                default:
                    return $"{engineName}…";
            }
        }
    }
}
