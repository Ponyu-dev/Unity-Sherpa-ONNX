using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Vad.Config
{
    /// <summary>
    /// Loads VAD settings from the StreamingAssets JSON file.
    /// Use <see cref="Load"/> for Desktop (sync),
    /// <see cref="LoadAsync"/> for all platforms including Android.
    /// </summary>
    public static class VadSettingsLoader
    {
        private const string SettingsRelativePath = "SherpaOnnx/vad-settings.json";

        /// <summary>
        /// Reads and deserializes vad-settings.json from StreamingAssets.
        /// </summary>
        public static VadSettingsData Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, SettingsRelativePath);
            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Loading VAD settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] VAD settings not found: {path}");
                return new VadSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<VadSettingsData>(json);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] VAD settings loaded: {data.profiles?.Count ?? 0} profiles");

            return data ?? new VadSettingsData();
        }

        /// <summary>
        /// Async version: extracts files on Android first, then reads
        /// vad-settings.json. Works on all platforms.
        /// </summary>
        public static async UniTask<VadSettingsData> LoadAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            bool ready = await StreamingAssetsCopier
                .EnsureExtractedAsync(progress, ct);

            if (!ready)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] StreamingAssets extraction failed.");
                return new VadSettingsData();
            }

            string path = Path.Combine(
                StreamingAssetsCopier.GetResolvedStreamingAssetsPath(),
                SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Loading VAD settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] VAD settings not found: {path}");
                return new VadSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<VadSettingsData>(json);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] VAD settings loaded: {data?.profiles?.Count ?? 0} profiles");
            return data ?? new VadSettingsData();
        }

        /// <summary>
        /// Returns the active profile from loaded settings, or null.
        /// </summary>
        public static VadProfile GetActiveProfile(VadSettingsData data)
        {
            if (data?.profiles == null || data.profiles.Count == 0)
                return null;

            int idx = Mathf.Clamp(
                data.activeProfileIndex, 0, data.profiles.Count - 1);

            return data.profiles[idx];
        }
    }
}
