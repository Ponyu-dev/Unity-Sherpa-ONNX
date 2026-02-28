using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Kws.Data
{
    /// <summary>
    /// Root container for KWS settings.
    /// Serialized to/from JSON in StreamingAssets.
    /// </summary>
    [Serializable]
    public sealed class KwsSettingsData : ISettingsData<KwsProfile>
    {
        public int activeProfileIndex = -1;
        public List<KwsProfile> profiles = new();

        int ISettingsData<KwsProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<KwsProfile> ISettingsData<KwsProfile>.Profiles => profiles;
    }
}
