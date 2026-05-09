using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Vad;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Builds the bottom-status line shown by every VAD sample panel.
    /// Centralises the wording so panels stay consistent and don't
    /// duplicate a one-liner across the codebase. Combines live
    /// init-progress from <see cref="VadInitProgressBus"/> with the
    /// service's runtime state.
    /// </summary>
    internal static class VadSampleStatusUtil
    {
        public static string BuildCurrent(IVadService vad)
        {
            if (!VadInitProgressBus.VadFinished)
                return $"Initializing VAD — {VadInitProgressBus.VadPercent}%";

            if (!VadInitProgressBus.AsrFinished)
                return $"Initializing VAD-pipeline ASR — {VadInitProgressBus.AsrPercent}%";

            return BuildReady(vad);
        }

        public static string BuildReady(IVadService vad)
        {
            if (vad == null)
                return "Service not available.";
            if (!vad.IsReady)
                return "VAD engine failed to load. See console for details.";

            var profile = vad.ActiveProfile;
            string profileName = profile != null
                ? profile.profileName
                : "(no profile)";
            return $"Ready • VAD profile '{profileName}' • window {vad.WindowSize}";
        }

        // VAD-menu helpers: the menu shows two engines (VAD + pipeline ASR)
        // side by side, each with its own loading bar / ready line.

        public static string BuildVadLine(IVadService vad)
        {
            if (!VadInitProgressBus.VadFinished)
                return $"VAD: initializing — {VadInitProgressBus.VadPercent}%";

            if (vad == null || !vad.IsReady)
                return "VAD: failed to load. See console for details.";

            var profile = vad.ActiveProfile;
            string profileName = profile != null
                ? profile.profileName
                : "(no profile)";
            return $"VAD: ready • '{profileName}' • window {vad.WindowSize}";
        }

        public static string BuildPipelineAsrLine(IAsrService asr)
        {
            if (!VadInitProgressBus.AsrFinished)
                return $"ASR: initializing — {VadInitProgressBus.AsrPercent}%";

            if (asr == null || !asr.IsReady)
                return "ASR: failed to load. See console for details.";

            var profile = asr.ActiveProfile;
            string profileName = profile != null
                ? profile.profileName
                : "(no profile)";
            return $"ASR: ready • '{profileName}'";
        }
    }
}
