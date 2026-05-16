using System;
using System.Threading;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel for the ASR sample. Holds the latest
    /// <see cref="ProfileReadyEvent"/> for both the offline and online
    /// services side by side so the sample menu and any panel can
    /// render a status line for each engine without reaching into the
    /// services directly. Publish methods are safe to call from any
    /// thread — the <see cref="Changed"/> invocation is marshaled to
    /// Unity's main thread via <see cref="MainThreadDispatcher"/> so
    /// subscribers can touch UI Toolkit elements without manual
    /// thread checks.
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
            if (MainThreadDispatcher.IsCurrent)
            {
                ApplyOffline(e);
                return;
            }
            MainThreadDispatcher.Post(PostedApplyOffline, e);
        }

        /// <summary>
        /// Method-group target passed into
        /// <c>IOnlineAsrService.InitializeAsync(PublishOnlineEvent, ct)</c>.
        /// </summary>
        public static void PublishOnlineEvent(ProfileReadyEvent e)
        {
            if (MainThreadDispatcher.IsCurrent)
            {
                ApplyOnline(e);
                return;
            }
            MainThreadDispatcher.Post(PostedApplyOnline, e);
        }

        private static readonly SendOrPostCallback PostedApplyOffline = PostedApplyOfflineImpl;
        private static readonly SendOrPostCallback PostedApplyOnline = PostedApplyOnlineImpl;

        private static void PostedApplyOfflineImpl(object state)
        {
            ApplyOffline((ProfileReadyEvent)state);
        }

        private static void PostedApplyOnlineImpl(object state)
        {
            ApplyOnline((ProfileReadyEvent)state);
        }

        // Track CURRENT phase, not sticky highest-water-mark — see the
        // note in TtsInitProgressBus.
        private static void ApplyOffline(ProfileReadyEvent e)
        {
            LastOfflineEvent = e;
            OfflineHasEvent = true;
            OfflineReady = e.Phase == ProfileReadyPhase.Ready;
            OfflineFailed = e.Phase == ProfileReadyPhase.Failed;
            Changed?.Invoke();
        }

        private static void ApplyOnline(ProfileReadyEvent e)
        {
            LastOnlineEvent = e;
            OnlineHasEvent = true;
            OnlineReady = e.Phase == ProfileReadyPhase.Ready;
            OnlineFailed = e.Phase == ProfileReadyPhase.Failed;
            Changed?.Invoke();
        }
    }
}
