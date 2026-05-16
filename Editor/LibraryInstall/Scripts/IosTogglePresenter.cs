using System;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall.Helpers;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.LibraryInstall
{
    /// <summary>
    /// Manages Sim / Mac toggle buttons inside the iOS platform row.
    /// Reads and writes <see cref="SherpaOnnxProjectSettings"/> toggle state;
    /// triggers reinstall from cache when iOS is already installed.
    /// </summary>
    internal sealed class IosTogglePresenter : IDisposable
    {
        private static readonly Color ColorOff = new(0.55f, 0.47f, 0.16f, 1f);
        private static readonly Color ColorOn = new(0.18f, 0.37f, 0.25f, 1f);

        private Button _simulatorToggle;
        private Button _macToggle;

        internal void Build(VisualElement rowRoot, Label statusLabel)
        {
            var row = rowRoot.Q<VisualElement>(className: "platform-row");
            if (row == null)
                return;

            int insertIdx = statusLabel != null
                ? row.IndexOf(statusLabel)
                : row.childCount;

            _simulatorToggle = new Button { text = "Sim" };
            _simulatorToggle.AddToClassList("btn-toggle");
            _simulatorToggle.style.backgroundColor = ColorOff;
            _simulatorToggle.clicked += HandleSimulatorToggle;
            row.Insert(insertIdx, _simulatorToggle);

            _macToggle = new Button { text = "Mac" };
            _macToggle.AddToClassList("btn-toggle");
            _macToggle.style.backgroundColor = ColorOff;
            _macToggle.clicked += HandleMacToggle;
            row.Insert(insertIdx + 1, _macToggle);

            Refresh();
        }

        public void Dispose()
        {
            if (_simulatorToggle != null)
                _simulatorToggle.clicked -= HandleSimulatorToggle;
            if (_macToggle != null)
                _macToggle.clicked -= HandleMacToggle;

            _simulatorToggle = null;
            _macToggle = null;
        }

        internal void Refresh()
        {
            if (_simulatorToggle == null)
                return;

            var s = SherpaOnnxProjectSettings.instance;

            _simulatorToggle.text = s.iosIncludeSimulator ? "Sim \u2713" : "Sim";
            _simulatorToggle.style.backgroundColor = s.iosIncludeSimulator ? ColorOn : ColorOff;

            _macToggle.text = s.iosIncludeMac ? "Mac \u2713" : "Mac";
            _macToggle.style.backgroundColor = s.iosIncludeMac ? ColorOn : ColorOff;
        }

        private void HandleSimulatorToggle()
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.iosIncludeSimulator = !s.iosIncludeSimulator;
            s.SaveSettings();
            Refresh();

            if (iOSArchiveCache.IsReady && LibraryInstallStatus.IsIosInstalled())
                InstallPipelineFactory.ReinstallIosFromCache();
        }

        private void HandleMacToggle()
        {
            var s = SherpaOnnxProjectSettings.instance;
            s.iosIncludeMac = !s.iosIncludeMac;
            s.SaveSettings();
            Refresh();

            if (iOSArchiveCache.IsReady && LibraryInstallStatus.IsIosInstalled())
                InstallPipelineFactory.ReinstallIosFromCache();
        }
    }
}
