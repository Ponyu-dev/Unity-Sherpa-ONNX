using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Build
{
    /// <summary>
    /// Zips LocalZip ASR model directories (offline + online)
    /// before build and restores them after. Delegates to shared
    /// <see cref="LocalZipBuildHelper"/>.
    /// </summary>
    internal sealed class AsrLocalZipBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string BackupRoot = "AsrBuildBackup";

        public int callbackOrder => 101;

        public void OnPreprocessBuild(BuildReport report)
        {
            AsrProjectSettings settings = AsrProjectSettings.instance;
            var entries = new List<LocalZipBuildHelper.ProfileEntry>();

            foreach (AsrProfile p in settings.offlineData.profiles)
                entries.Add(new LocalZipBuildHelper.ProfileEntry(p.profileName, p.modelSource));

            foreach (OnlineAsrProfile p in settings.onlineData.profiles)
                entries.Add(new LocalZipBuildHelper.ProfileEntry(p.profileName, p.modelSource));

            LocalZipBuildHelper.Preprocess(BackupRoot, entries, AsrModelPaths.GetModelDir);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            LocalZipBuildHelper.Postprocess(BackupRoot, AsrModelPaths.GetModelDir);
        }
    }
}
