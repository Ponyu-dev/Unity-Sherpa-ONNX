using System;
using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Tts.Data
{
    /// <summary>
    /// Root container for TTS settings.
    /// Serialized to JSON for runtime use.
    /// </summary>
    [Serializable]
    public sealed class TtsSettingsData
    {
        public int activeProfileIndex = -1;
        public TtsCacheSettings cache = new();
        public List<TtsProfile> profiles = new();
    }
}