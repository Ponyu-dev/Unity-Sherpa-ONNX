using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Platform;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Config
{
    /// <summary>
    /// Loads TTS settings from the StreamingAssets JSON file.
    /// Use <see cref="Load"/> for Desktop (sync),
    /// <see cref="LoadAsync"/> for all platforms including Android.
    /// </summary>
    public static class TtsSettingsLoader
    {
        private const string SettingsRelativePath = "SherpaOnnx/tts-settings.json";

        /// <summary>
        /// Reads and deserializes tts-settings.json from StreamingAssets.
        /// </summary>
        public static TtsSettingsData Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Loading TTS settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] TTS settings not found: {path}");
                return new TtsSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<TtsSettingsData>(json);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] TTS settings loaded: {data.profiles?.Count ?? 0} profiles");

            return data ?? new TtsSettingsData();
        }

        /// <summary>
        /// Async version: extracts files on Android first, then reads
        /// tts-settings.json. Works on all platforms.
        /// </summary>
        public static async UniTask<TtsSettingsData> LoadAsync(
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            bool ready = await StreamingAssetsCopier
                .EnsureExtractedAsync(progress, ct);

            if (!ready)
            {
                SherpaOnnxLog.RuntimeError("[SherpaOnnx] StreamingAssets extraction failed.");
                return new TtsSettingsData();
            }

            string path = Path.Combine(
                StreamingAssetsCopier.GetResolvedStreamingAssetsPath(),
                SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog($"[SherpaOnnx] Loading TTS settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] TTS settings not found: {path}");
                return new TtsSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<TtsSettingsData>(json);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS settings loaded: " +
                $"{data?.profiles?.Count ?? 0} profiles");

            return data ?? new TtsSettingsData();
        }

        /// <summary>
        /// Returns the active profile from loaded settings, or null.
        /// </summary>
        public static TtsProfile GetActiveProfile(TtsSettingsData data)
        {
            if (data?.profiles == null || data.profiles.Count == 0)
                return null;

            int idx = Mathf.Clamp(
                data.activeProfileIndex, 0, data.profiles.Count - 1);

            return data.profiles[idx];
        }
    }
}
