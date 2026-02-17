using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common.Data;

namespace PonyuDev.SherpaOnnx.Tts.Data
{
    /// <summary>
    /// Root container for TTS settings.
    /// Serialized to JSON for runtime use.
    /// </summary>
    [Serializable]
    public sealed class TtsSettingsData : ISettingsData<TtsProfile>
    {
        public int activeProfileIndex = -1;
        public TtsCacheSettings cache = new();
        public List<TtsProfile> profiles = new();

        int ISettingsData<TtsProfile>.ActiveProfileIndex
        {
            get => activeProfileIndex;
            set => activeProfileIndex = value;
        }

        List<TtsProfile> ISettingsData<TtsProfile>.Profiles => profiles;
    }
}