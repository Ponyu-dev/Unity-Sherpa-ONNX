using System.IO;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEditor;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings
{
    /// <summary>
    /// Persists KWS settings in ProjectSettings/ and exports them
    /// as JSON to StreamingAssets for runtime use.
    /// </summary>
    [FilePath("ProjectSettings/KwsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class KwsProjectSettings
        : ScriptableSingleton<KwsProjectSettings>, ISaveableSettings
    {
        private const string RuntimeJsonDir = "Assets/StreamingAssets/SherpaOnnx";
        private const string RuntimeJsonPath = RuntimeJsonDir + "/kws-settings.json";

        public bool kwsEnabled = true;
        public KwsSettingsData data = new();

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
