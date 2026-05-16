using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace PonyuDev.SherpaOnnx.Editor.Common.Build
{
    /// <summary>
    /// Temporarily adds <c>SHERPA_ONNX</c> define for iOS builds
    /// when the standard managed DLL is absent but the iOS-specific
    /// managed DLL is installed.
    /// <para>
    /// The iOS managed DLL uses <c>__Internal</c> P/Invoke and
    /// cannot load in the Editor, so the define is normally off.
    /// This processor enables it only for player compilation.
    /// </para>
    /// </summary>
    internal sealed class IosBuildDefineProcessor
        : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        /// <summary>
        /// Runs before all other processors to ensure the define
        /// is set before any code that depends on it.
        /// </summary>
        public int callbackOrder => 0;

        private bool _addedDefine;

        public void OnPreprocessBuild(BuildReport report)
        {
            _addedDefine = false;

            if (report.summary.platform != BuildTarget.iOS)
                return;

            if (LibraryInstallStatus.IsManagedDllPresent())
                return;

            if (!LibraryInstallStatus.IsIosManagedDllPresent())
                return;

            SherpaOnnxLog.EditorLog(
                "[SherpaOnnx] iOS build: adding SHERPA_ONNX " +
                "define (iOS managed DLL detected).");

            ScriptingDefineHelper.EnsureDefineForTarget(
                NamedBuildTarget.iOS);
            _addedDefine = true;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (!_addedDefine)
                return;

            SherpaOnnxLog.EditorLog(
                "[SherpaOnnx] iOS build: removing temporary " +
                "SHERPA_ONNX define.");

            ScriptingDefineHelper.RemoveDefineForTarget(
                NamedBuildTarget.iOS);
            _addedDefine = false;
        }
    }
}
