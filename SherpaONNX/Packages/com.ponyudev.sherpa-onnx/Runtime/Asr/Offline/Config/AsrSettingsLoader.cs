using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Asr.Offline.Config
{
    /// <summary>
    /// Loads ASR settings from the StreamingAssets JSON file.
    /// Use <see cref="Load"/> for Desktop (sync),
    /// <see cref="LoadAsync"/> for all platforms including Android.
    /// </summary>
    public static class AsrSettingsLoader
    {
        private const string SettingsRelativePath = "SherpaOnnx/asr-settings.json";

        /// <summary>
        /// Reads and deserializes asr-settings.json from StreamingAssets.
        /// </summary>
        public static AsrSettingsData Load()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath, SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Loading ASR settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] ASR settings not found: {path}");
                return new AsrSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<AsrSettingsData>(json);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ASR settings loaded: " +
                $"{data.profiles?.Count ?? 0} profiles");

            return data ?? new AsrSettingsData();
        }

        /// <summary>
        /// Async version: extracts files on Android first, then reads
        /// asr-settings.json. Works on all platforms.
        /// </summary>
        public static async UniTask<AsrSettingsData> LoadAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            bool ready = await StreamingAssetsCopier
                .EnsureExtractedAsync(progress, ct);

            if (!ready)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] StreamingAssets extraction failed.");
                return new AsrSettingsData();
            }

            string path = Path.Combine(
                StreamingAssetsCopier.GetResolvedStreamingAssetsPath(),
                SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Loading ASR settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] ASR settings not found: {path}");
                return new AsrSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<AsrSettingsData>(json);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] ASR settings loaded: " +
                $"{data?.profiles?.Count ?? 0} profiles");

            return data ?? new AsrSettingsData();
        }

        /// <summary>
        /// Returns the active profile from loaded settings, or null.
        /// </summary>
        public static AsrProfile GetActiveProfile(AsrSettingsData data)
        {
            if (data?.profiles == null || data.profiles.Count == 0)
                return null;

            int idx = Mathf.Clamp(
                data.activeProfileIndex, 0, data.profiles.Count - 1);

            return data.profiles[idx];
        }
    }
}
