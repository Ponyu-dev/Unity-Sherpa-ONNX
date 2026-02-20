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
