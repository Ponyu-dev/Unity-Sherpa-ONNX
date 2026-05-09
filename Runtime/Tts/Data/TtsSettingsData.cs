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

        /// <summary>
        /// When <c>true</c>, switching to a different profile via
        /// <see cref="ITtsService.SwitchProfile(int)"/> /
        /// <see cref="ITtsService.SwitchProfile(string)"/> deletes the
        /// extracted directory of the previously active profile if it was
        /// a <see cref="Common.Data.ModelSource.LocalZip"/> source. Only
        /// runs after the new profile is successfully loaded — a failed
        /// switch leaves the previous extraction intact. Default
        /// <c>false</c>: keep older models on disk so re-switching does
        /// not pay the re-extract cost.
        /// </summary>
        public bool autoDeletePreviousProfile;

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