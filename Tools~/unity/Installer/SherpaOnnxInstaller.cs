#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PonyuDev.SherpaOnnx.Installer
{
    [InitializeOnLoad]
    public static class SherpaOnnxInstaller
    {
        private const string PackageName = "com.ponyudev.sherpa-onnx";
        private const string RegistryName = "OpenUPM";
        private const string RegistryUrl = "https://package.openupm.com";

        private static readonly string[] Scopes =
        {
            "com.ponyudev.sherpa-onnx",
            "com.cysharp.unitask"
        };

        static SherpaOnnxInstaller()
        {
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[SherpaOnnxInstaller] manifest.json not found.");
                return;
            }

            var manifest = File.ReadAllText(manifestPath);

            if (manifest.Contains(PackageName))
            {
                Debug.Log($"[SherpaOnnxInstaller] {PackageName} is already installed.");
                CleanUp();
                return;
            }

            var confirmed = EditorUtility.DisplayDialog(
                "Install PonyuDev Sherpa-ONNX",
                "This will add the OpenUPM scoped registry and install com.ponyudev.sherpa-onnx.\n\nContinue?",
                "Install",
                "Cancel");

            if (!confirmed)
            {
                Debug.Log("[SherpaOnnxInstaller] Installation cancelled.");
                return;
            }

            AddScopedRegistry(manifestPath, manifest);
            CleanUp();
        }

        private static void AddScopedRegistry(string manifestPath, string manifest)
        {
            // Build scopes JSON array
            var scopesJson = "";
            for (int i = 0; i < Scopes.Length; i++)
            {
                scopesJson += $"        \"{Scopes[i]}\"";
                if (i < Scopes.Length - 1) scopesJson += ",";
                scopesJson += "\n";
            }

            var registryEntry =
                "{\n" +
                $"      \"name\": \"{RegistryName}\",\n" +
                $"      \"url\": \"{RegistryUrl}\",\n" +
                "      \"scopes\": [\n" +
                scopesJson +
                "      ]\n" +
                "    }";

            // Add scopedRegistries if not present
            if (!manifest.Contains("\"scopedRegistries\""))
            {
                var insertIndex = manifest.IndexOf('{') + 1;
                var registryBlock = $"\n  \"scopedRegistries\": [\n    {registryEntry}\n  ],";
                manifest = manifest.Insert(insertIndex, registryBlock);
            }
            else if (!manifest.Contains(RegistryUrl))
            {
                var registriesIndex = manifest.IndexOf("\"scopedRegistries\"");
                var arrayStart = manifest.IndexOf('[', registriesIndex) + 1;
                manifest = manifest.Insert(arrayStart, $"\n    {registryEntry},");
            }

            // Add package dependency
            var depsIndex = manifest.IndexOf("\"dependencies\"");
            var depsStart = manifest.IndexOf('{', depsIndex) + 1;
            manifest = manifest.Insert(depsStart, $"\n    \"{PackageName}\": \"0.1.0\",");

            File.WriteAllText(manifestPath, manifest);

            Debug.Log($"[SherpaOnnxInstaller] {PackageName} added to manifest.json. Unity will resolve the package now.");

            AssetDatabase.Refresh();
        }

        private static void CleanUp()
        {
            // Find and delete the installer script and its folder
            var guids = AssetDatabase.FindAssets("SherpaOnnxInstaller t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("SherpaOnnxInstaller"))
                {
                    var directory = Path.GetDirectoryName(path);
                    AssetDatabase.DeleteAsset(directory);
                    Debug.Log("[SherpaOnnxInstaller] Installer removed.");
                    break;
                }
            }
        }
    }
}
#endif
