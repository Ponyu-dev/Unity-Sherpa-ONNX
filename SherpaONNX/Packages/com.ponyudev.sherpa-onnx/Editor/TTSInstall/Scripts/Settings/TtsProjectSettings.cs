using System.IO;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEditor;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings
{
    /// <summary>
    /// Persists TTS settings in ProjectSettings/ and exports them
    /// as JSON to StreamingAssets for runtime use.
    /// </summary>
    [FilePath("ProjectSettings/TtsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class TtsProjectSettings : ScriptableSingleton<TtsProjectSettings>
    {
        private const string RuntimeJsonDir = "Assets/StreamingAssets/SherpaOnnx";
        private const string RuntimeJsonPath = RuntimeJsonDir + "/tts-settings.json";

        public TtsSettingsData data = new();

        internal void SaveSettings()
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
