using PonyuDev.SherpaOnnx.Common;
using UnityEditor;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall
{
    [FilePath("ProjectSettings/SherpaOnnxSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class SherpaOnnxProjectSettings : ScriptableSingleton<SherpaOnnxProjectSettings>
    {
        public string version = "1.12.24";
        public string installedVersion = "";
        public bool strictValidation = true;
        public bool macPostprocess = true;
        public bool debugLogEditor = true;
        public bool debugLogRuntime = true;

        internal void SaveSettings()
        {
            Save(true);
        }

        [InitializeOnLoadMethod]
        private static void SyncLogSettings()
        {
            var s = instance;
            SherpaOnnxLog.EditorEnabled = s.debugLogEditor;
            SherpaOnnxLog.RuntimeEnabled = s.debugLogRuntime;
        }
    }
}