using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Vad;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Builds the bottom-status line for VAD sample panels and the
    /// menu. Reads the latest <see cref="ProfileReadyEvent"/>s from
    /// <see cref="VadInitProgressBus"/> for the VAD engine and the
    /// pipeline-companion ASR engine.
    /// </summary>
    internal static class VadSampleStatusUtil
    {
        public static string BuildCurrent(IVadService vad)
        {
            if (VadInitProgressBus.VadReady && VadInitProgressBus.AsrReady)
                return BuildReady(vad);

            // Show whichever engine still has work to do.
            if (!VadInitProgressBus.VadReady && VadInitProgressBus.VadHasEvent)
                return BuildPhaseText(VadInitProgressBus.LastVadEvent, "VAD");

            if (!VadInitProgressBus.AsrReady && VadInitProgressBus.AsrHasEvent)
                return BuildPhaseText(VadInitProgressBus.LastAsrEvent, "Pipeline ASR");

            return "Initializing VAD…";
        }

        public static string BuildReady(IVadService vad)
        {
            if (vad == null)
                return "Service not available.";
            if (!vad.IsReady)
                return "VAD engine failed to load. See console for details.";

            var profile = vad.ActiveProfile;
            string profileName = profile != null ? profile.profileName : "(no profile)";
            return $"Ready • VAD profile '{profileName}' • window {vad.WindowSize}";
        }

        // VAD-menu helpers: the menu shows two engines (VAD + pipeline
        // ASR) side by side, each with its own status line.

        public static string BuildVadLine(IVadService vad)
        {
            if (VadInitProgressBus.VadReady)
            {
                if (vad == null || !vad.IsReady)
                    return "VAD: failed to load. See console for details.";
                var profile = vad.ActiveProfile;
                string profileName = profile != null ? profile.profileName : "(no profile)";
                return $"VAD: ready • '{profileName}' • window {vad.WindowSize}";
            }
            if (!VadInitProgressBus.VadHasEvent)
                return "VAD: initializing…";
            return BuildPhaseText(VadInitProgressBus.LastVadEvent, "VAD");
        }

        public static string BuildPipelineAsrLine(IAsrService asr)
        {
            if (VadInitProgressBus.AsrReady)
            {
                if (asr == null || !asr.IsReady)
                    return "ASR: failed to load. See console for details.";
                var profile = asr.ActiveProfile;
                string profileName = profile != null ? profile.profileName : "(no profile)";
                return $"ASR: ready • '{profileName}'";
            }
            if (!VadInitProgressBus.AsrHasEvent)
                return "ASR: initializing…";
            return BuildPhaseText(VadInitProgressBus.LastAsrEvent, "ASR");
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
    }
}
