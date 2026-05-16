using System.IO;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Build
{
    /// <summary>
    /// Validates microphone permissions before build when ASR is enabled.
    /// Android: checks AndroidManifest.xml for RECORD_AUDIO.
    /// iOS: checks PlayerSettings.iOS.microphoneUsageDescription.
    /// macOS: checks PlayerSettings.macOS camera/microphone usage description.
    /// Never modifies user settings — only validates.
    /// </summary>
    internal sealed class MicrophonePermissionValidator : IPreprocessBuildWithReport
    {
        private const string AndroidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        private const string RecordAudioPermission = "android.permission.RECORD_AUDIO";

        public int callbackOrder => 30;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AsrProjectSettings.instance.asrEnabled)
                return;

            BuildTarget target = report.summary.platform;

            if (target == BuildTarget.Android)
                ValidateAndroid();
            else if (target == BuildTarget.iOS)
                ValidateIos();
            else if (target == BuildTarget.StandaloneOSX)
                ValidateMacOs();
        }

        private static void ValidateAndroid()
        {
            if (!File.Exists(AndroidManifestPath))
            {
                throw new BuildFailedException(
                    "[SherpaOnnx] ASR module is enabled but no custom " +
                    "AndroidManifest.xml found at " +
                    AndroidManifestPath + ".\n" +
                    "Unity does not auto-add RECORD_AUDIO permission.\n" +
                    "Create a manifest and add:\n" +
                    "<uses-permission android:name=\"" +
                    RecordAudioPermission + "\" />");
            }

            string content = File.ReadAllText(AndroidManifestPath);

            if (!content.Contains(RecordAudioPermission))
            {
                throw new BuildFailedException(
                    "[SherpaOnnx] ASR module is enabled but " +
                    AndroidManifestPath +
                    " is missing RECORD_AUDIO permission.\n" +
                    "Add the following line inside <manifest>:\n" +
                    "<uses-permission android:name=\"" +
                    RecordAudioPermission + "\" />");
            }
        }

        private static void ValidateIos()
        {
            string desc =
                PlayerSettings.iOS.microphoneUsageDescription;

            if (string.IsNullOrEmpty(desc))
            {
                throw new BuildFailedException(
                    "[SherpaOnnx] ASR module is enabled but " +
                    "Microphone Usage Description is empty.\n" +
                    "Set it in Player Settings > iOS > " +
                    "Other Settings > Configuration,\n" +
                    "or via script: " +
                    "PlayerSettings.iOS.microphoneUsageDescription " +
                    "= \"Speech recognition\";");
            }
        }

        private static void ValidateMacOs()
        {
#if UNITY_2022_1_OR_NEWER
            string desc =
                PlayerSettings.macOS.microphoneUsageDescription;

            if (string.IsNullOrEmpty(desc))
            {
                throw new BuildFailedException(
                    "[SherpaOnnx] ASR module is enabled but " +
                    "macOS Microphone Usage Description is empty.\n" +
                    "Set it in Player Settings > macOS > " +
                    "Other Settings > Configuration,\n" +
                    "or via script: " +
                    "PlayerSettings.macOS" +
                    ".microphoneUsageDescription " +
                    "= \"Speech recognition\";");
            }
#else
            UnityEngine.Debug.LogWarning(
                "[SherpaOnnx] ASR module is enabled. " +
                "Ensure NSMicrophoneUsageDescription is set " +
                "in your macOS Info.plist for microphone access.");
#endif
        }
    }
}
