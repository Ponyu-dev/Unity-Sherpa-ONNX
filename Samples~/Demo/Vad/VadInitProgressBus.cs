using System;
using System.Threading;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel for the VAD sample. Holds the latest
    /// <see cref="ProfileReadyEvent"/> for both the VAD service and
    /// the pipeline-companion ASR service so the sample menu and panel
    /// can render two status lines simultaneously. Publish methods are
    /// safe to call from any thread — the <see cref="Changed"/>
    /// invocation is marshaled to Unity's main thread via
    /// <see cref="MainThreadDispatcher"/>.
    /// </summary>
    internal static class VadInitProgressBus
    {
        public static event Action Changed;

        public static ProfileReadyEvent LastVadEvent { get; private set; }
        public static bool VadHasEvent { get; private set; }
        public static bool VadReady { get; private set; }
        public static bool VadFailed { get; private set; }

        public static ProfileReadyEvent LastAsrEvent { get; private set; }
        public static bool AsrHasEvent { get; private set; }
        public static bool AsrReady { get; private set; }
        public static bool AsrFailed { get; private set; }

        /// <summary>
        /// Method-group target passed into
        /// <c>IVadService.InitializeAsync(PublishVadEvent, ct)</c>.
        /// </summary>
        public static void PublishVadEvent(ProfileReadyEvent e)
        {
            if (MainThreadDispatcher.IsCurrent)
            {
                ApplyVad(e);
                return;
            }
            MainThreadDispatcher.Post(PostedApplyVad, e);
        }

        /// <summary>
        /// Method-group target passed into
        /// <c>IAsrService.InitializeAsync(PublishAsrEvent, ct)</c> for
        /// the pipeline ASR engine.
        /// </summary>
        public static void PublishAsrEvent(ProfileReadyEvent e)
        {
            if (MainThreadDispatcher.IsCurrent)
            {
                ApplyAsr(e);
                return;
            }
            MainThreadDispatcher.Post(PostedApplyAsr, e);
        }

        private static readonly SendOrPostCallback PostedApplyVad = PostedApplyVadImpl;
        private static readonly SendOrPostCallback PostedApplyAsr = PostedApplyAsrImpl;

        private static void PostedApplyVadImpl(object state)
        {
            ApplyVad((ProfileReadyEvent)state);
        }

        private static void PostedApplyAsrImpl(object state)
        {
            ApplyAsr((ProfileReadyEvent)state);
        }

        private static void ApplyVad(ProfileReadyEvent e)
        {
            LastVadEvent = e;
            VadHasEvent = true;
            if (e.Phase == ProfileReadyPhase.Ready)
                VadReady = true;
            else if (e.Phase == ProfileReadyPhase.Failed)
                VadFailed = true;
            Changed?.Invoke();
        }

        private static void ApplyAsr(ProfileReadyEvent e)
        {
            LastAsrEvent = e;
            AsrHasEvent = true;
            if (e.Phase == ProfileReadyPhase.Ready)
                AsrReady = true;
            else if (e.Phase == ProfileReadyPhase.Failed)
                AsrFailed = true;
            Changed?.Invoke();
        }
    }
}
