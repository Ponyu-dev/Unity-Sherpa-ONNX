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
        public List<VadProfile> profiles = new();

        int ISettingsData<VadProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<VadProfile> ISettingsData<VadProfile>.Profiles => profiles;
    }
}
