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

        internal void SaveSettings()
        {
            Save(true);
        }
    }
}