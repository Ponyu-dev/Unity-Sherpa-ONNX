using System;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel for the ASR sample. Holds the latest
    /// <see cref="ProfileReadyEvent"/> for both the offline and online
    /// services side by side so the sample menu and any panel can
    /// render a status line for each engine without reaching into the
    /// services directly.
    /// </summary>
    internal static class AsrInitProgressBus
    {
        public static event Action Changed;

        public static ProfileReadyEvent LastOfflineEvent { get; private set; }
        public static bool OfflineHasEvent { get; private set; }
        public static bool OfflineReady { get; private set; }
        public static bool OfflineFailed { get; private set; }

        public static ProfileReadyEvent LastOnlineEvent { get; private set; }
        public static bool OnlineHasEvent { get; private set; }
        public static bool OnlineReady { get; private set; }
        public static bool OnlineFailed { get; private set; }

        /// <summary>
        /// Method-group target passed into
        /// <c>IAsrService.InitializeAsync(PublishOfflineEvent, ct)</c>.
        /// </summary>
        public static void PublishOfflineEvent(ProfileReadyEvent e)
        {
            LastOfflineEvent = e;
            OfflineHasEvent = true;
            if (e.Phase == ProfileReadyPhase.Ready)
                OfflineReady = true;
            else if (e.Phase == ProfileReadyPhase.Failed)
                OfflineFailed = true;
            Changed?.Invoke();
        }

        /// <summary>
        /// Method-group target passed into
        /// <c>IOnlineAsrService.InitializeAsync(PublishOnlineEvent, ct)</c>.
        /// </summary>
        public static void PublishOnlineEvent(ProfileReadyEvent e)
        {
            LastOnlineEvent = e;
            OnlineHasEvent = true;
            if (e.Phase == ProfileReadyPhase.Ready)
                OnlineReady = true;
            else if (e.Phase == ProfileReadyPhase.Failed)
                OnlineFailed = true;
            Changed?.Invoke();
        }
    }
}
