using System.IO;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Editor.Common;
using UnityEditor;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.Microphone.Settings
{
    /// <summary>
    /// Persists microphone capture settings in ProjectSettings/
    /// and exports them as JSON to StreamingAssets for runtime use.
    /// </summary>
    [FilePath("ProjectSettings/MicrophoneSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class MicrophoneProjectSettings
        : ScriptableSingleton<MicrophoneProjectSettings>, ISaveableSettings
    {
        private const string RuntimeJsonDir =
            "Assets/StreamingAssets/SherpaOnnx";
        private const string RuntimeJsonPath =
            RuntimeJsonDir + "/microphone-settings.json";

        public MicrophoneSettingsData data = new();

        public void SaveSettings()
        {
            Save(true);
            ExportRuntimeJson();
        }

        private void ExportRuntimeJson()
        {
            Directory.CreateDirectory(RuntimeJsonDir);

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(RuntimeJsonPath, json);

            AssetDatabase.Refresh();
        }
    }
}
