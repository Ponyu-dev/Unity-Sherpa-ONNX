using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Asr.Online.Data
{
    /// <summary>
    /// Root container for online ASR settings.
    /// Serialized as <c>online-asr-settings.json</c> in StreamingAssets.
    /// </summary>
    [Serializable]
    public sealed class OnlineAsrSettingsData
        : ISettingsData<OnlineAsrProfile>
    {
        public int activeProfileIndex = -1;

        /// <summary>
        /// When <c>true</c>, switching to a different profile deletes the
        /// extracted directory of the previously active profile if it was
        /// a <see cref="Common.Data.ModelSource.LocalZip"/>. Only runs
        /// after the new profile loads successfully. Default <c>false</c>.
        /// </summary>
        public bool autoDeletePreviousProfile;

        /// <summary>
        /// When <c>true</c>, the Editor build pipeline temporarily moves
        /// every non-active online ASR profile's model directory (and
        /// any <see cref="Common.Data.ModelSource.LocalZip"/> archive)
        /// out of StreamingAssets before manifest generation, so the
        /// produced build only ships the active profile's model files.
        /// Offline ASR has its own independent flag on
        /// <see cref="Offline.Data.AsrSettingsData"/>. Moved content is
        /// restored after the build finishes; a defensive restore on
        /// Editor reload covers crashes / cancellations. Default
        /// <c>false</c>: every profile in <see cref="profiles"/> ships
        /// into the build.
        /// </summary>
        public bool buildOnlyActiveProfile;

        public List<OnlineAsrProfile> profiles = new();

        int ISettingsData<OnlineAsrProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<OnlineAsrProfile> ISettingsData<OnlineAsrProfile>.Profiles
            => profiles;
    }
}
