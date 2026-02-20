using System.IO;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEditor;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Settings
{
    /// <summary>
    /// Persists VAD settings in ProjectSettings/ and exports them
    /// as JSON to StreamingAssets for runtime use.
    /// </summary>
    [FilePath("ProjectSettings/VadSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class VadProjectSettings
        : ScriptableSingleton<VadProjectSettings>, ISaveableSettings
    {
        private const string RuntimeJsonDir = "Assets/StreamingAssets/SherpaOnnx";
        private const string RuntimeJsonPath = RuntimeJsonDir + "/vad-settings.json";

        public bool vadEnabled = true;
        public VadSettingsData data = new();

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
