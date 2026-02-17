using System;
using System.Collections.Generic;

namespace PonyuDev.SherpaOnnx.Asr.Online.Data
{
    /// <summary>
    /// Root container for online ASR settings.
    /// Serialized as <c>online-asr-settings.json</c> in StreamingAssets.
    /// </summary>
    [Serializable]
    public sealed class OnlineAsrSettingsData
    {
        public int activeProfileIndex = -1;
        public List<OnlineAsrProfile> profiles = new();
    }
}
