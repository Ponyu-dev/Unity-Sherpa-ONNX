using System;
using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Asr.Data
{
    /// <summary>
    /// Root container for ASR settings.
    /// Serialized to/from JSON in StreamingAssets.
    /// </summary>
    [Serializable]
    public sealed class AsrSettingsData
    {
        public int activeProfileIndex = -1;
        public int offlineRecognizerPoolSize = 1;
        public List<AsrProfile> profiles = new();
    }
}
