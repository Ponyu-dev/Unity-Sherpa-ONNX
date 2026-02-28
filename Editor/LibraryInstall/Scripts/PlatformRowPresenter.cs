using System;
using System.Threading;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.InstallPipeline;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.ContentHandlers;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall
{
    /// <summary>
    /// Presenter for a single platform/architecture row in Settings UI.
    /// Dumb orchestrator: binds UI, delegates all logic to helpers.
    /// </summary>
    internal sealed class PlatformRowPresenter : IDisposable
    {
        private const string PlatformLabelName = "platformType";
        private const string StatusLabelName = "status";
        private const string ProgressName = "progress";
        private const string InstallButtonName = "btnInstall";
        private const string DeleteButtonName = "btnDelete";

        private Label _platformLabel;
        private Label _statusLabel;
        private VisualElement _progress;
        private Button _installButton;
        private Button _deleteButton;

        private readonly LibraryArch _libraryArch;
        private readonly Func<string> _getVersion;
        private readonly bool _isIos;

        private IosTogglePresenter _iosToggles;
        private CancellationTokenSource _cts;
        private bool _isBusy;

        internal PlatformRowPresenter(LibraryArch libraryArch, Func<string> getVersion)
        {
            _libraryArch = libraryArch;
            _getVersion = getVersion;
            _isIos = InstallPipelineFactory.IsIOS(libraryArch);
        }

        internal void Build(VisualElement rowRoot)
        {
            _platformLabel = rowRoot.Q<Label>(PlatformLabelName);
            _statusLabel = rowRoot.Q<Label>(StatusLabelName);
            _progress = rowRoot.Q<VisualElement>(ProgressName);
            _installButton = rowRoot.Q<Button>(InstallButtonName);
            _deleteButton = rowRoot.Q<Button>(DeleteButtonName);

            if (_platformLabel != null)
                _platformLabel.text = _libraryArch.Name;

            if (_isIos)
            {
                _iosToggles = new IosTogglePresenter();
                _iosToggles.Build(rowRoot, _statusLabel);
            }

            SetProgress01(0f);
            RefreshStatus();
            Subscribe();
        }

        public void Dispose()
        {
            Unsubscribe();
            CancelAndDisposeCts();

            _iosToggles?.Dispose();
            _iosToggles = null;

            _platformLabel = null;
            _statusLabel = null;
            _progress = null;
            _installButton = null;
            _deleteButton = null;
        }

        internal bool NeedsUpdate()
        {
            if (!LibraryInstallStatus.IsInstalled(_libraryArch))
                return false;

            string installedVer = SherpaOnnxProjectSettings.instance.installedVersion;
            string currentVer = _getVersion();
            return !string.IsNullOrEmpty(installedVer) && installedVer != currentVer;
        }

        internal void TriggerInstall() => HandleInstallClicked();

        internal void RefreshStatus()
        {
            bool installed = LibraryInstallStatus.IsInstalled(_libraryArch);
            bool canOperate = LibraryInstallStatus.CanOperate(_libraryArch);
            bool needsUpdate = NeedsUpdate();

            if (needsUpdate)
            {
                SetStatus("Update available");
                ApplyStatusStyle("update");
                _installButton?.SetEnabled(false);
            }
            else if (installed)
            {
                SetStatus("Installed");
                ApplyStatusStyle("installed");

                bool iosNeedsCache = _isIos && !iOSArchiveCache.IsReady;
                _installButton?.SetEnabled(iosNeedsCache);
            }
            else
            {
                SetStatus("Not installed");
                ApplyStatusStyle("notinstalled");
                _installButton?.SetEnabled(canOperate && !IsVersionLocked());
            }

            _deleteButton?.SetEnabled(canOperate && installed);
            _iosToggles?.Refresh();
        }

        private void Subscribe()
        {
            if (_installButton != null)
                _installButton.clicked += HandleInstallClicked;
            if (_deleteButton != null)
                _deleteButton.clicked += HandleDeleteClicked;
        }

        private void Unsubscribe()
        {
            if (_installButton != null)
                _installButton.clicked -= HandleInstallClicked;
            if (_deleteButton != null)
                _deleteButton.clicked -= HandleDeleteClicked;
        }

        private void HandleInstallClicked()
        {
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] Install clicked: {_libraryArch.Name}, isBusy={_isBusy}");

            if (_isBusy)
                return;
            _isBusy = true;

            SetButtonsEnabled(false);
            SetProgress01(0f);

            CancelAndDisposeCts();
            _cts = new CancellationTokenSource();

            _ = InstallFlow(_cts.Token);
        }

        private void HandleDeleteClicked()
        {
            if (_isBusy)
                return;
            _isBusy = true;

            SetButtonsEnabled(false);
            SetProgress01(0f);

            DeleteFlow();
        }

        private async Task InstallFlow(CancellationToken ct)
        {
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] InstallFlow started: {_libraryArch.Name}");

            try
            {
                string version = _getVersion();

                if (InstallPipelineFactory.IsAndroid(_libraryArch))
                    await InstallPipelineFactory.RunAndroidInstallAsync(
                        _libraryArch, version, ct, SetStatus, SetProgress01);
                else if (InstallPipelineFactory.IsIOS(_libraryArch))
                    await InstallPipelineFactory.RuniOSInstallAsync(
                        _libraryArch, version, ct, SetStatus, SetProgress01);
                else
                    await InstallStandardFlow(version, ct);

                AssetDatabase.Refresh();
                PluginImportConfigurator.Configure(_libraryArch);
                ScriptingDefineHelper.EnsureDefine();

                var s = SherpaOnnxProjectSettings.instance;
                s.installedVersion = version;
                s.SaveSettings();

                SherpaOnnxLog.EditorLog($"[SherpaOnnx] InstallFlow completed: {_libraryArch.Name} v{version}");
            }
            catch (Exception ex)
            {
                SetStatus("Error");
                SherpaOnnxLog.EditorError($"[SherpaOnnx] Install failed for {_libraryArch.Name}: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
                RefreshStatus();
            }
        }

        private async Task InstallStandardFlow(string version, CancellationToken ct)
        {
            string url = InstallPipelineFactory.BuildUrl(_libraryArch, version);
            string fileName = InstallPipelineFactory.BuildFileName(_libraryArch);

            using var pipeline = InstallPipelineFactory.Create(_libraryArch);

            pipeline.OnStatus += SetStatus;
            pipeline.OnProgress01 += SetProgress01;
            pipeline.OnError += HandlePipelineError;

            await pipeline.RunAsync(url, fileName, ct);
        }

        private void DeleteFlow()
        {
            SherpaOnnxLog.EditorLog($"[SherpaOnnx] DeleteFlow started: {_libraryArch.Name}");

            try
            {
                var pipeline = new PackageDeletePipeline();

                pipeline.OnStatus += SetStatus;
                pipeline.OnError += HandlePipelineError;
                pipeline.Run(LibraryInstallStatus.GetDeleteTargetPath(_libraryArch));

                // Cascade: when deleting managed DLL, also remove
                // all non-iOS native libraries that depend on it.
                if (_libraryArch == LibraryPlatforms.ManagedLibrary)
                {
                    var dependentPaths = LibraryInstallStatus.GetNonIosInstalledPaths();

                    foreach (string path in dependentPaths)
                        pipeline.Run(path);

                }

                AssetDatabase.Refresh();

                // Sync define with managed DLL presence
                ScriptingDefineHelper.SyncDefineWithInstallState();

                if (!LibraryInstallStatus.HasAnyInstalled())
                {
                    var s = SherpaOnnxProjectSettings.instance;
                    s.installedVersion = "";
                    s.SaveSettings();
                }

                SherpaOnnxLog.EditorLog($"[SherpaOnnx] DeleteFlow completed: {_libraryArch.Name}");
            }
            catch (Exception ex)
            {
                SetStatus("Error");
                SherpaOnnxLog.EditorError($"[SherpaOnnx] Delete failed for {_libraryArch.Name}: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
                RefreshStatus();
            }
        }

        private void ApplyStatusStyle(string state)
        {
            if (_statusLabel == null)
                return;

            _statusLabel.RemoveFromClassList("status-label--installed");
            _statusLabel.RemoveFromClassList("status-label--notinstalled");
            _statusLabel.RemoveFromClassList("status-label--update");
            _statusLabel.AddToClassList("status-label--" + state);
        }

        /// <summary>
        /// Returns true when a different version is already installed,
        /// meaning new installs must go through "Update All" instead.
        /// </summary>
        private bool IsVersionLocked()
        {
            string installed = SherpaOnnxProjectSettings.instance.installedVersion;
            return !string.IsNullOrEmpty(installed) && installed != _getVersion();
        }

        private void HandlePipelineError(string message)
        {
            SetStatus("Error");
            SherpaOnnxLog.EditorError($"[SherpaOnnx] {_libraryArch.Name}: {message}");
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void SetProgress01(float p01)
        {
            if (_progress == null)
                return;

            float clamped = p01 < 0f ? 0f : p01 > 1f ? 1f : p01;
            _progress.style.scale = new Scale(new Vector3(clamped, 1f, 1f));
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _installButton?.SetEnabled(enabled);
            _deleteButton?.SetEnabled(enabled);
        }

        private void CancelAndDisposeCts()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
