using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Editor.Common.Build;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Import;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Build
{
    /// <summary>
    /// Zips LocalZip VAD model directories before build
    /// and restores them after. Delegates to shared
    /// <see cref="LocalZipBuildHelper"/>.
    /// </summary>
    internal sealed class VadLocalZipBuildProcessor
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string BackupRoot = "VadBuildBackup";

        public int callbackOrder => 102;

        public void OnPreprocessBuild(BuildReport report)
        {
            var entries = new List<LocalZipBuildHelper.ProfileEntry>();

            foreach (VadProfile p in VadProjectSettings.instance.data.profiles)
                entries.Add(new LocalZipBuildHelper.ProfileEntry(p.profileName, p.modelSource));

            LocalZipBuildHelper.Preprocess(BackupRoot, entries, VadModelPaths.GetModelDir);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            LocalZipBuildHelper.Postprocess(BackupRoot, VadModelPaths.GetModelDir);
        }
    }
}
