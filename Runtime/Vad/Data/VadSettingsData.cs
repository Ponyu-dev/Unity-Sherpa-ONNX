using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Vad.Data
{
    /// <summary>
    /// Root container for VAD settings.
    /// Serialized to/from JSON in StreamingAssets.
    /// </summary>
    [Serializable]
    public sealed class VadSettingsData : ISettingsData<VadProfile>
    {
        public int activeProfileIndex = -1;

        /// <summary>
        /// When <c>true</c>, the runtime keeps only the active VAD
        /// profile's extracted directory; every other extraction is
        /// removed after a successful <c>InitializeAsync</c> and after
        /// every successful <c>SwitchProfile</c>. Failed loads leave
        /// the previous on-disk state intact. Implied automatically
        /// when <see cref="buildOnlyActiveProfile"/> is on.
        /// </summary>
        public bool keepOnlyActiveProfile;

        /// <summary>
        /// When <c>true</c>, the Editor build pipeline temporarily moves
        /// every non-active VAD profile's model directory (and any
        /// <see cref="Common.Data.ModelSource.LocalZip"/> archive) out
        /// of StreamingAssets before manifest generation, so the
        /// produced build only ships the active profile's model files.
        /// Moved content is restored after the build finishes; a
        /// defensive restore on Editor reload covers crashes /
        /// cancellations. Default <c>false</c>: every profile in
        /// <see cref="profiles"/> ships into the build. Implies
        /// <see cref="keepOnlyActiveProfile"/> at runtime — services
        /// treat this flag as if both were checked.
        /// </summary>
        public bool buildOnlyActiveProfile;

        public List<VadProfile> profiles = new();

        int ISettingsData<VadProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<VadProfile> ISettingsData<VadProfile>.Profiles => profiles;
    }
}
