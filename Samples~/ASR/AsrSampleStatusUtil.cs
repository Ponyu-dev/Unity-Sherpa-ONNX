using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Builds the bottom-status line shown by every ASR sample panel.
    /// Centralises the wording so panels stay consistent and don't
    /// duplicate a one-liner across the codebase. Combines live
    /// init-progress from <see cref="AsrInitProgressBus"/> with the
    /// service's runtime state.
    /// </summary>
    internal static class AsrSampleStatusUtil
    {
        public static string BuildOfflineCurrent(IAsrService asr)
        {
            if (!AsrInitProgressBus.OfflineFinished)
                return $"Initializing offline ASR — {AsrInitProgressBus.OfflinePercent}%";

            return BuildOfflineReady(asr);
        }

        public static string BuildOnlineCurrent(IOnlineAsrService asr)
        {
            if (!AsrInitProgressBus.OnlineFinished)
                return $"Initializing online ASR — {AsrInitProgressBus.OnlinePercent}%";

            return BuildOnlineReady(asr);
        }

        public static string BuildOfflineReady(IAsrService asr)
        {
            if (asr == null)
                return "Service not available.";
            if (!asr.IsReady)
                return "Offline ASR engine failed to load. See console for details.";

            var profile = asr.ActiveProfile;
            string profileName = profile != null
                ? profile.profileName
                : "(no profile)";
            return $"Ready • offline ASR profile '{profileName}'";
        }

        public static string BuildOnlineReady(IOnlineAsrService asr)
        {
            if (asr == null)
                return "Service not available.";
            if (!asr.IsReady)
                return "Online ASR engine failed to load. See console for details.";

            var profile = asr.ActiveProfile;
            string profileName = profile != null
                ? profile.profileName
                : "(no profile)";
            return $"Ready • online ASR profile '{profileName}'";
        }
    }
}
