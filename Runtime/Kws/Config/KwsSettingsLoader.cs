using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Kws.Config
{
    /// <summary>
    /// Loads KWS settings from the StreamingAssets JSON file.
    /// Use <see cref="Load"/> for Desktop (sync),
    /// <see cref="LoadAsync"/> for all platforms including Android.
    /// </summary>
    public static class KwsSettingsLoader
    {
        private const string SettingsRelativePath = "SherpaOnnx/kws-settings.json";

        /// <summary>
        /// Reads and deserializes kws-settings.json from StreamingAssets.
        /// </summary>
        public static KwsSettingsData Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Loading KWS settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] KWS settings not found: {path}");
                return new KwsSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<KwsSettingsData>(json);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] KWS settings loaded: {data?.profiles?.Count ?? 0} profiles");

            return data ?? new KwsSettingsData();
        }

        /// <summary>
        /// Async version: extracts files on Android first, then reads
        /// kws-settings.json. Works on all platforms.
        /// </summary>
        public static async UniTask<KwsSettingsData> LoadAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            bool ready = await StreamingAssetsCopier.EnsureExtractedAsync(progress, ct);

            if (!ready)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] StreamingAssets extraction failed.");
                return new KwsSettingsData();
            }

            string path = Path.Combine(StreamingAssetsCopier.GetResolvedStreamingAssetsPath(), SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Loading KWS settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] KWS settings not found: {path}");
                return new KwsSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<KwsSettingsData>(json);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] KWS settings loaded: {data?.profiles?.Count ?? 0} profiles");

            return data ?? new KwsSettingsData();
        }

        /// <summary>
        /// Returns the active profile from loaded settings, or null.
        /// </summary>
        public static KwsProfile GetActiveProfile(KwsSettingsData data)
        {
            if (data?.profiles == null || data.profiles.Count == 0)
                return null;

            int idx = Mathf.Clamp(data.activeProfileIndex, 0, data.profiles.Count - 1);

            return data.profiles[idx];
        }
    }
}
