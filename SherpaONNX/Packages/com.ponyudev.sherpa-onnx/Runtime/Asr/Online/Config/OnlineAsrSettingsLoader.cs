using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Asr.Online.Config
{
    /// <summary>
    /// Loads online ASR settings from StreamingAssets JSON.
    /// </summary>
    public static class OnlineAsrSettingsLoader
    {
        private const string SettingsRelativePath =
            "SherpaOnnx/online-asr-settings.json";

        /// <summary>
        /// Sync load. Works on Desktop; not on Android (use LoadAsync).
        /// </summary>
        public static OnlineAsrSettingsData Load()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath, SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Loading online ASR settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Online ASR settings not found: {path}");
                return new OnlineAsrSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<OnlineAsrSettingsData>(json);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online ASR settings loaded: " +
                $"{data?.profiles?.Count ?? 0} profiles");

            return data ?? new OnlineAsrSettingsData();
        }

        /// <summary>
        /// Async load with Android extraction support.
        /// </summary>
        public static async UniTask<OnlineAsrSettingsData> LoadAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            bool ready = await StreamingAssetsCopier
                .EnsureExtractedAsync(progress, ct);

            if (!ready)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] StreamingAssets extraction failed.");
                return new OnlineAsrSettingsData();
            }

            string path = Path.Combine(
                StreamingAssetsCopier.GetResolvedStreamingAssetsPath(),
                SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Loading online ASR settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] Online ASR settings not found: {path}");
                return new OnlineAsrSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<OnlineAsrSettingsData>(json);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Online ASR settings loaded: " +
                $"{data?.profiles?.Count ?? 0} profiles");

            return data ?? new OnlineAsrSettingsData();
        }

        /// <summary>
        /// Returns the active profile from loaded settings, or null.
        /// </summary>
        public static OnlineAsrProfile GetActiveProfile(
            OnlineAsrSettingsData data)
        {
            if (data?.profiles == null || data.profiles.Count == 0)
                return null;

            int idx = Mathf.Clamp(
                data.activeProfileIndex, 0, data.profiles.Count - 1);

            return data.profiles[idx];
        }
    }
}
