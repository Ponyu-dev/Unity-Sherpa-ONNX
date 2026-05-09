using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Asr.Offline.Data
{
    /// <summary>
    /// Root container for ASR settings.
    /// Serialized to/from JSON in StreamingAssets.
    /// </summary>
    [Serializable]
    public sealed class AsrSettingsData : ISettingsData<AsrProfile>
    {
        public int activeProfileIndex = -1;
        public int offlineRecognizerPoolSize = 1;

        /// <summary>
        /// When <c>true</c>, switching to a different profile deletes the
        /// extracted directory of the previously active profile if it was
        /// a <see cref="Common.Data.ModelSource.LocalZip"/>. Only runs
        /// after the new profile loads successfully. Default <c>false</c>.
        /// </summary>
        public bool autoDeletePreviousProfile;

        public List<AsrProfile> profiles = new();

        int ISettingsData<AsrProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<AsrProfile> ISettingsData<AsrProfile>.Profiles => profiles;
    }
}
