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
        /// When <c>true</c>, the runtime keeps only the active TTS
        /// profile's extracted directory under
        /// <see cref="UnityEngine.Application.persistentDataPath"/>.
        /// Every other extraction (left over from previous switches or
        /// abandoned imports) is removed after a successful
        /// <c>InitializeAsync</c> and after every successful
        /// <see cref="ITtsService.SwitchProfile(int)"/> /
        /// <see cref="ITtsService.SwitchProfile(string)"/>. Failed
        /// loads leave the previous on-disk state untouched so the
        /// user can recover. Implied automatically when
        /// <see cref="buildOnlyActiveProfile"/> is on — the build only
        /// ships one profile, so keeping more than one extracted at
        /// runtime makes no sense.
        /// </summary>
        public bool keepOnlyActiveProfile;

        /// <summary>
        /// When <c>true</c>, the Editor build pipeline temporarily moves
        /// every non-active TTS profile's model directory (and any
        /// <see cref="Common.Data.ModelSource.LocalZip"/> archive) out of
        /// StreamingAssets before manifest generation, so the produced
        /// build only ships the active profile's model files. The moved
        /// content is restored after the build finishes; a defensive
        /// restore on Editor reload covers crashes / cancellations.
        /// Default <c>false</c>: every profile listed in
        /// <see cref="profiles"/> ships into the build. Implies
        /// <see cref="keepOnlyActiveProfile"/> at runtime — services
        /// treat this flag as if both were checked.
        /// </summary>
        public bool buildOnlyActiveProfile;

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