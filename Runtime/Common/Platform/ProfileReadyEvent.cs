using System;

namespace PonyuDev.SherpaOnnx.Common.Platform
{
    /// <summary>
    /// Pipeline phase reported by services during
    /// <c>InitializeAsync</c>. Each phase carries its own 0..100
    /// percent in <see cref="ProfileReadyEvent.Percent"/> — Download
    /// goes from 0 to 100, then Extract goes from 0 to 100, then Init
    /// goes from 0 to 100. Subscribers can show per-phase status text
    /// directly, or weight the phases into a single overall bar (see
    /// the runtime-usage docs for the recommended pattern).
    /// </summary>
    public enum ProfileReadyPhase
    {
        /// <summary>
        /// Archive download in progress. Emitted only for
        /// <see cref="Common.Data.ModelSource.Remote"/> profiles.
        /// </summary>
        Download,

        /// <summary>
        /// Previous download attempt failed; about to retry.
        /// <see cref="ProfileReadyEvent.RetryAttempt"/> holds the
        /// 1-based retry number, <see cref="ProfileReadyEvent.Message"/>
        /// the previous error.
        /// </summary>
        DownloadRetrying,

        /// <summary>
        /// Decompression in progress. Emitted for Remote (after
        /// download), LocalZip (always), and Local on Android (when
        /// the per-profile group is being staged from the APK).
        /// Tar streams report 0% at the start and 100% at the end —
        /// per-entry percent is only meaningful for zip archives.
        /// </summary>
        Extract,

        /// <summary>
        /// Native engine construction in progress (sherpa-onnx ctor
        /// runs on the thread pool). The native call is opaque so the
        /// service emits a single 0% before and 100% after — this
        /// phase exists mainly so a status label can switch from
        /// "Extracting…" to "Initializing engine…".
        /// </summary>
        Init,

        /// <summary>
        /// Pipeline aborted: either retries were exhausted or an
        /// unrecoverable I/O error happened.
        /// <see cref="ProfileReadyEvent.Error"/> /
        /// <see cref="ProfileReadyEvent.Message"/> describe what.
        /// </summary>
        Failed,

        /// <summary>
        /// Service is fully ready to use. Always emitted last on
        /// success; <see cref="ProfileReadyEvent.Percent"/> = 100.
        /// </summary>
        Ready,
    }

    /// <summary>
    /// One readiness-pipeline event passed to the service's
    /// <c>onEvent</c> callback. Read-only struct — no per-event
    /// allocation. Each phase reports its own 0..100 percent;
    /// subscribers that want a unified progress bar weight the phases
    /// themselves (see runtime-usage docs).
    /// </summary>
    public readonly struct ProfileReadyEvent
    {
        public readonly ProfileReadyPhase Phase;

        /// <summary>0..100 progress within the current phase.</summary>
        public readonly int Percent;

        /// <summary>
        /// Archive URL for Remote profiles, otherwise <c>null</c>.
        /// </summary>
        public readonly string Url;

        /// <summary>
        /// Free-form context: previous error text on
        /// <see cref="ProfileReadyPhase.DownloadRetrying"/> /
        /// <see cref="ProfileReadyPhase.Failed"/>.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// 1-based retry number for
        /// <see cref="ProfileReadyPhase.DownloadRetrying"/>; 0 otherwise.
        /// </summary>
        public readonly int RetryAttempt;

        /// <summary>
        /// Captured exception for
        /// <see cref="ProfileReadyPhase.Failed"/>; <c>null</c> otherwise.
        /// </summary>
        public readonly Exception Error;

        public ProfileReadyEvent(
            ProfileReadyPhase phase,
            int percent = 0,
            string url = null,
            string message = null,
            int retryAttempt = 0,
            Exception error = null)
        {
            Phase = phase;
            Percent = ClampPercent(percent);
            Url = url;
            Message = message;
            RetryAttempt = retryAttempt;
            Error = error;
        }

        private static int ClampPercent(int value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return value;
        }
    }
}
