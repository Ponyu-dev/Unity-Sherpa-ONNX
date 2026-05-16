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
        /// When <c>true</c>, the runtime keeps only the active online
        /// ASR profile's extracted directory; every other extraction is
        /// removed after a successful <c>InitializeAsync</c> and after
        /// every successful <c>SwitchProfile</c>. Failed loads leave
        /// the previous on-disk state intact. Implied automatically
        /// when <see cref="buildOnlyActiveProfile"/> is on.
        /// </summary>
        public bool keepOnlyActiveProfile;

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
        /// into the build. Implies <see cref="keepOnlyActiveProfile"/>
        /// at runtime — services treat this flag as if both were
        /// checked.
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
