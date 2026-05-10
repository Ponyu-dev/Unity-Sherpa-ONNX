using System;
using System.Threading;
using PonyuDev.SherpaOnnx.Common.Platform;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Static channel through which <c>DemoNavigator</c> publishes
    /// <see cref="ProfileReadyEvent"/>s emitted by
    /// <see cref="PonyuDev.SherpaOnnx.Tts.ITtsService.InitializeAsync"/>,
    /// and any open demo view subscribes to refresh its status label
    /// in real time. Static so views can reach it without changing
    /// their <c>Bind</c> signature — views are short-lived UI
    /// controllers, the bus outlives them.
    /// </summary>
    internal static class TtsInitProgressBus
    {
        /// <summary>Fires whenever <see cref="PublishEvent"/> is called.
        /// Subscribers should read <see cref="LastEvent"/> /
        /// <see cref="IsReady"/> / <see cref="IsFailed"/> from this
        /// class — the event is parameterless so the callback can be a
        /// normal named method.</summary>
        public static event Action Changed;

        public static ProfileReadyEvent LastEvent { get; private set; }
        public static bool HasEvent { get; private set; }
        public static bool IsReady { get; private set; }
        public static bool IsFailed { get; private set; }

        /// <summary>
        /// Method-group target passed straight into
        /// <c>ITtsService.InitializeAsync(PublishEvent, ct)</c> by the
        /// sample navigator — keeps the call site lambda-free. Safe to
        /// call from any thread; the <see cref="Changed"/> invocation
        /// is marshaled to Unity's main thread via
        /// <see cref="MainThreadDispatcher"/> so subscribers can touch
        /// UI Toolkit elements without checking threading themselves.
        /// </summary>
        public static void PublishEvent(ProfileReadyEvent e)
        {
            if (MainThreadDispatcher.IsCurrent)
            {
                ApplyAndRaise(e);
                return;
            }
            // Box the struct once so SendOrPostCallback can carry it.
            MainThreadDispatcher.Post(PostedApplyAndRaise, e);
        }

        private static readonly SendOrPostCallback PostedApplyAndRaise = PostedApplyAndRaiseImpl;

        private static void PostedApplyAndRaiseImpl(object state)
        {
            ApplyAndRaise((ProfileReadyEvent)state);
        }

        private static void ApplyAndRaise(ProfileReadyEvent e)
        {
            LastEvent = e;
            HasEvent = true;
            if (e.Phase == ProfileReadyPhase.Ready)
                IsReady = true;
            else if (e.Phase == ProfileReadyPhase.Failed)
                IsFailed = true;
            Changed?.Invoke();
        }
    }
}
