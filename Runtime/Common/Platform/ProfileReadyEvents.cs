using System;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Shared helpers around <see cref="ProfileReadyEvent"/> so each
    /// service / resolver doesn't duplicate the same emit boilerplate
    /// or its own private <see cref="IProgress{T}"/> adapter. All
    /// helpers are <c>null</c>-safe — passing a <c>null</c> callback
    /// silently drops the event.
    /// </summary>
    public static class ProfileReadyEvents
    {
        /// <summary>
        /// Emits a single <see cref="ProfileReadyEvent"/> for the given
        /// phase / percent.
        /// </summary>
        public static void Emit(Action<ProfileReadyEvent> onEvent, ProfileReadyPhase phase, int percent = 0)
        {
            if (onEvent == null)
                return;
            onEvent(new ProfileReadyEvent(phase, percent));
        }

        /// <summary>
        /// Emits <see cref="ProfileReadyPhase.Init"/> with the given
        /// percent. Use 0 right before native engine construction starts
        /// and 100 right after it returns.
        /// </summary>
        public static void EmitInit(Action<ProfileReadyEvent> onEvent, int percent)
            => Emit(onEvent, ProfileReadyPhase.Init, percent);

        /// <summary>
        /// Emits the terminal <see cref="ProfileReadyPhase.Ready"/>
        /// event with 100%. Should be the last event a successful
        /// initialization stream produces.
        /// </summary>
        public static void EmitReady(Action<ProfileReadyEvent> onEvent)
            => Emit(onEvent, ProfileReadyPhase.Ready, 100);

        /// <summary>
        /// Emits <see cref="ProfileReadyPhase.Failed"/> with an optional
        /// human-readable <paramref name="message"/> (and optional
        /// captured <paramref name="error"/>).
        /// </summary>
        public static void EmitFailed(
            Action<ProfileReadyEvent> onEvent, string message, Exception error = null)
        {
            if (onEvent == null)
                return;
            onEvent(new ProfileReadyEvent(ProfileReadyPhase.Failed, 0, null, message, 0, error));
        }

        /// <summary>
        /// Wraps an <see cref="Action{ProfileReadyEvent}"/> so it can
        /// be passed where an <see cref="IProgress{Single}"/> is
        /// expected (e.g. <c>StreamingAssetsCopier</c> /
        /// <c>LocalZipExtractor</c>). Each <c>IProgress&lt;float&gt;.Report(0..1)</c>
        /// is converted into a <see cref="ProfileReadyPhase.Extract"/>
        /// event with 0..100 percent. Returns <c>null</c> when
        /// <paramref name="onEvent"/> is <c>null</c>.
        /// </summary>
        public static IProgress<float> AsExtractProgress(Action<ProfileReadyEvent> onEvent)
        {
            return onEvent == null ? null : new ExtractProgressAdapter(onEvent);
        }

        private sealed class ExtractProgressAdapter : IProgress<float>
        {
            private readonly Action<ProfileReadyEvent> _onEvent;
            public ExtractProgressAdapter(Action<ProfileReadyEvent> onEvent) { _onEvent = onEvent; }
            public void Report(float value)
            {
                int percent = (int)(value * 100f);
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;
                _onEvent(new ProfileReadyEvent(ProfileReadyPhase.Extract, percent));
            }
        }
    }
}
