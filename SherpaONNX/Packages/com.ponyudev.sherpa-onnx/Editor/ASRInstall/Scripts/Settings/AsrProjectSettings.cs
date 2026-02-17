using System.IO;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings
{
    /// <summary>
    /// Persists ASR settings in ProjectSettings/ and exports them
    /// as JSON to StreamingAssets for runtime use.
    /// Two separate JSON files: offline and online.
    /// </summary>
    [FilePath("ProjectSettings/AsrSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class AsrProjectSettings
        : ScriptableSingleton<AsrProjectSettings>, ISaveableSettings
    {
        private const string RuntimeJsonDir =
            "Assets/StreamingAssets/SherpaOnnx";

        private const string OfflineJsonPath =
            RuntimeJsonDir + "/asr-settings.json";

        private const string OnlineJsonPath =
            RuntimeJsonDir + "/online-asr-settings.json";

        public AsrSettingsData offlineData = new();
        public OnlineAsrSettingsData onlineData = new();

        public void SaveSettings()
        {
            Save(true);
            ExportRuntimeJson();
        }

        private void ExportRuntimeJson()
        {
            Directory.CreateDirectory(RuntimeJsonDir);

            string offlineJson = JsonUtility.ToJson(offlineData,
                prettyPrint: true);
            File.WriteAllText(OfflineJsonPath, offlineJson);

            string onlineJson = JsonUtility.ToJson(onlineData,
                prettyPrint: true);
            File.WriteAllText(OnlineJsonPath, onlineJson);

            AssetDatabase.Refresh();
        }
    }
}
