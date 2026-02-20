using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common.Platform;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Common.Audio.Config
{
    /// <summary>
    /// Loads microphone settings from
    /// <c>SherpaOnnx/microphone-settings.json</c>
    /// in StreamingAssets. Returns defaults when the file
    /// is missing or invalid.
    /// </summary>
    public static class MicrophoneSettingsLoader
    {
        private const string SettingsRelativePath =
            "SherpaOnnx/microphone-settings.json";

        /// <summary>Desktop-only sync load.</summary>
        public static MicrophoneSettingsData Load()
        {
            string path = Path.Combine(
                Application.streamingAssetsPath,
                SettingsRelativePath);

            return LoadFromPath(path);
        }

        /// <summary>
        /// All-platform async load. Extracts StreamingAssets
        /// on Android first.
        /// </summary>
        public static async UniTask<MicrophoneSettingsData>
            LoadAsync(CancellationToken ct = default)
        {
            bool ready = await StreamingAssetsCopier
                .EnsureExtractedAsync(null, ct);

            if (!ready)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] MicrophoneSettingsLoader: " +
                    "extraction failed, using defaults.");
                return new MicrophoneSettingsData();
            }

            string path = Path.Combine(
                StreamingAssetsCopier
                    .GetResolvedStreamingAssetsPath(),
                SettingsRelativePath);

            return LoadFromPath(path);
        }

        private static MicrophoneSettingsData LoadFromPath(
            string path)
        {
            if (!File.Exists(path))
            {
                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] MicrophoneSettingsLoader: " +
                    "file not found, using defaults.");
                return new MicrophoneSettingsData();
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility
                    .FromJson<MicrophoneSettingsData>(json);

                SherpaOnnxLog.RuntimeLog(
                    "[SherpaOnnx] Microphone settings loaded " +
                    $"(threshold={data.silenceThreshold}, " +
                    $"silenceLimit={data.silenceFrameLimit}).");
                return data ?? new MicrophoneSettingsData();
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeWarning(
                    "[SherpaOnnx] MicrophoneSettingsLoader: " +
                    "parse error, using defaults. " +
                    ex.Message);
                return new MicrophoneSettingsData();
            }
        }
    }
}
