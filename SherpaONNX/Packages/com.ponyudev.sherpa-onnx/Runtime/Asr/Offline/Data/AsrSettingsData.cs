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
        public List<AsrProfile> profiles = new();

        int ISettingsData<AsrProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<AsrProfile> ISettingsData<AsrProfile>.Profiles => profiles;
    }
}
