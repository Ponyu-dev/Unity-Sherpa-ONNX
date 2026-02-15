using System.IO;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Tts.Config
{
    /// <summary>
    /// Loads TTS settings from the StreamingAssets JSON file.
    /// Phase 1: File.ReadAllText — Desktop only.
    /// Android/WebGL support will be added in later phases.
    /// </summary>
    public static class TtsSettingsLoader
    {
        private const string SettingsRelativePath = "SherpaOnnx/tts-settings.json";

        /// <summary>
        /// Reads and deserializes tts-settings.json from StreamingAssets.
        /// </summary>
        public static TtsSettingsData Load()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath, SettingsRelativePath);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] Loading TTS settings: {path}");

            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TTS settings not found: {path}");
                return new TtsSettingsData();
            }

            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<TtsSettingsData>(json);

            SherpaOnnxLog.RuntimeLog(
                $"[SherpaOnnx] TTS settings loaded: {data.profiles?.Count ?? 0} profiles");

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
