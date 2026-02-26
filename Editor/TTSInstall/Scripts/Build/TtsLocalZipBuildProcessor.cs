using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Editor.Common.Build;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Build
{
    /// <summary>
    /// Zips LocalZip TTS model directories before build
    /// and restores them after. Delegates to shared
    /// <see cref="LocalZipBuildHelper"/>.
    /// </summary>
    internal sealed class TtsLocalZipBuildProcessor
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string BackupRoot = "TtsBuildBackup";

        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var entries = new List<LocalZipBuildHelper.ProfileEntry>();

            foreach (TtsProfile p in TtsProjectSettings.instance.data.profiles)
                entries.Add(new LocalZipBuildHelper.ProfileEntry(p.profileName, p.modelSource));

            LocalZipBuildHelper.Preprocess(BackupRoot, entries, ModelPaths.GetTtsModelDir);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            LocalZipBuildHelper.Postprocess(BackupRoot, ModelPaths.GetTtsModelDir);
        }
    }
}
