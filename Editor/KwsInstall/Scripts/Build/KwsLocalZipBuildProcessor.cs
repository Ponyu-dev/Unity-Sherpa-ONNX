using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Editor.Common.Build;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Build
{
    /// <summary>
    /// Zips LocalZip KWS model directories before build
    /// and restores them after. Delegates to shared
    /// <see cref="LocalZipBuildHelper"/>.
    /// </summary>
    internal sealed class KwsLocalZipBuildProcessor
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string BackupRoot = "KwsBuildBackup";

        public int callbackOrder => 103;

        public void OnPreprocessBuild(BuildReport report)
        {
            var entries = new List<LocalZipBuildHelper.ProfileEntry>();

            foreach (KwsProfile p in KwsProjectSettings.instance.data.profiles)
                entries.Add(new LocalZipBuildHelper.ProfileEntry(p.profileName, p.modelSource));

            LocalZipBuildHelper.Preprocess(BackupRoot, entries, ModelPaths.GetKwsModelDir);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            LocalZipBuildHelper.Postprocess(BackupRoot, ModelPaths.GetKwsModelDir);
        }
    }
}
