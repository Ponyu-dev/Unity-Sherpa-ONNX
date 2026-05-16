using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Builds the bottom-status line for ASR sample panels and the
    /// menu. Reads the latest <see cref="ProfileReadyEvent"/> from
    /// <see cref="AsrInitProgressBus"/> for offline / online streams
    /// and turns each into one line per phase.
    /// </summary>
    internal static class AsrSampleStatusUtil
    {
        public static string BuildOfflineCurrent(IAsrService asr)
        {
            if (AsrInitProgressBus.OfflineReady)
                return BuildOfflineReady(asr);

            if (!AsrInitProgressBus.OfflineHasEvent)
                return "Initializing offline ASR…";

            return BuildPhaseText(AsrInitProgressBus.LastOfflineEvent, "Offline ASR");
        }

        public static string BuildOnlineCurrent(IOnlineAsrService asr)
        {
            if (AsrInitProgressBus.OnlineReady)
                return BuildOnlineReady(asr);

            if (!AsrInitProgressBus.OnlineHasEvent)
                return "Initializing online ASR…";

            return BuildPhaseText(AsrInitProgressBus.LastOnlineEvent, "Online ASR");
        }

        private static string BuildPhaseText(ProfileReadyEvent e, string engineName)
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

        public static string BuildOfflineReady(IAsrService asr)
        {
            if (asr == null)
                return "Service not available.";
            if (!asr.IsReady)
                return "Offline ASR engine failed to load. See console for details.";

            var profile = asr.ActiveProfile;
            string profileName = profile != null ? profile.profileName : "(no profile)";
            return $"Ready • offline ASR profile '{profileName}'";
        }

        public static string BuildOnlineReady(IOnlineAsrService asr)
        {
            if (asr == null)
                return "Service not available.";
            if (!asr.IsReady)
                return "Online ASR engine failed to load. See console for details.";

            var profile = asr.ActiveProfile;
            string profileName = profile != null ? profile.profileName : "(no profile)";
            return $"Ready • online ASR profile '{profileName}'";
        }
    }
}
