using System;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Import;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    /// <summary>
    /// Renders full VAD profile editing UI.
    /// Identity, thresholds, runtime, and model fields are always shown;
    /// model-specific section rebuilds when model type changes.
    /// </summary>
    internal sealed class VadProfileDetailPresenter : IDisposable
    {
        private readonly VadProjectSettings _settings;
        private readonly VisualElement _detailContent;

        private ProfileListPresenter<VadProfile> _listPresenter;
        private Button _redownloadButton;
        private Button _packZipButton;
        private Button _deleteZipButton;
        private int _currentIndex = -1;
        private bool _disposed;

        internal VadProfileDetailPresenter(
            VisualElement detailContent,
            VadProjectSettings settings)
        {
            _detailContent = detailContent;
            _settings = settings;
        }

        internal void SetListPresenter(
            ProfileListPresenter<VadProfile> listPresenter)
        {
            _listPresenter = listPresenter;
        }

        internal void ShowProfile(int index)
        {
            UnsubscribeRedownload();
            UnsubscribeZipButtons();
            _currentIndex = index;
            _detailContent.Clear();

            if (index < 0 || index >= _settings.data.profiles.Count)
                return;

            VadProfile profile = _settings.data.profiles[index];
            var binder = new VadProfileFieldBinder(profile, _settings);

            _redownloadButton = MissingFilesWarningBuilder.Build(_detailContent, profile.profileName, VadModelPaths.GetModelDir, !string.IsNullOrEmpty(profile.sourceUrl));
            if (_redownloadButton != null)
                _redownloadButton.clicked += HandleRedownloadClicked;
            BuildAutoConfigureButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(profile, binder);
            BuildThresholdsSection(binder);
            BuildRuntimeSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile, binder);
            BuildLocalZipSection(profile);
        }

        internal void Clear()
        {
            UnsubscribeRedownload();
            UnsubscribeZipButtons();
            _currentIndex = -1;
            _detailContent.Clear();
        }

        public void Dispose()
        {
            _disposed = true;
            Clear();
        }

        // ── Redownload ──

        private void UnsubscribeRedownload()
        {
            if (_redownloadButton != null)
                _redownloadButton.clicked -= HandleRedownloadClicked;
            _redownloadButton = null;
        }

        private void UnsubscribeZipButtons()
        {
            if (_packZipButton != null)
                _packZipButton.clicked -= HandlePackToZipClicked;
            _packZipButton = null;

            if (_deleteZipButton != null)
                _deleteZipButton.clicked -= HandleDeleteZipClicked;
            _deleteZipButton = null;
        }

        private async void HandleRedownloadClicked()
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;
            if (string.IsNullOrEmpty(profile.sourceUrl)) return;

            try
            {
                string modelDir = VadModelPaths.GetModelDir(profile.profileName);
                using var redownloader = new ModelRedownloader();
                await redownloader.RedownloadFileAsync(profile.sourceUrl, modelDir, default);
                VadProfileAutoFiller.Fill(profile, modelDir);
                _settings.SaveSettings();
                AssetDatabase.Refresh();
                _listPresenter?.RefreshList();
                ShowProfile(_currentIndex);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.EditorError($"[SherpaOnnx] VAD re-download failed: {ex}");
            }
        }

        // ── Sections ──

        private void BuildAutoConfigureButton(VadProfile profile)
        {
            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir))
                return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void BuildVersionWarning(VadModelType modelType)
        {
            string ver = SherpaOnnxProjectSettings.instance.installedVersion;
            if (string.IsNullOrEmpty(ver))
                return;
            if (ModelVersionRequirements.IsSupported(modelType, ver))
                return;

            string minVer = ModelVersionRequirements.GetMinVersion(modelType);
            _detailContent.Add(new HelpBox(
                $"Model type {modelType} requires sherpa-onnx >= {minVer}. " +
                $"Installed: {ver}. Update in Project Settings > Sherpa ONNX.",
                HelpBoxMessageType.Warning));
        }

        private void BuildIdentitySection(
            VadProfile profile, VadProfileFieldBinder binder)
        {
            var nameField = binder.BindText(
                "Profile name", profile.profileName,
                VadProfileField.ProfileName);
            nameField.RegisterCallback<FocusOutEvent>(HandleNameFocusOut);
            _detailContent.Add(nameField);

            var typeField = new EnumField("Model type", profile.modelType);
            typeField.RegisterValueChangedCallback(HandleModelTypeChanged);
            _detailContent.Add(typeField);

            var sourceField = new EnumField("Model source", profile.modelSource);
            sourceField.RegisterValueChangedCallback(HandleModelSourceChanged);
            _detailContent.Add(sourceField);

            var urlField = binder.BindText("Source URL", profile.sourceUrl, VadProfileField.SourceUrl);
            if (!string.IsNullOrEmpty(profile.sourceUrl))
                urlField.isReadOnly = true;
            _detailContent.Add(urlField);
        }

        private void BuildThresholdsSection(VadProfileFieldBinder b)
        {
            AddSectionHeader("Thresholds");
            _detailContent.Add(b.BindFloat(
                "Threshold", b.Profile.threshold,
                VadProfileField.Threshold));
            _detailContent.Add(b.BindFloat(
                "Min silence duration", b.Profile.minSilenceDuration,
                VadProfileField.MinSilenceDuration));
            _detailContent.Add(b.BindFloat(
                "Min speech duration", b.Profile.minSpeechDuration,
                VadProfileField.MinSpeechDuration));
            _detailContent.Add(b.BindFloat(
                "Max speech duration", b.Profile.maxSpeechDuration,
                VadProfileField.MaxSpeechDuration));
        }

        private void BuildRuntimeSection(VadProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt(
                "Sample rate", b.Profile.sampleRate,
                VadProfileField.SampleRate));
            _detailContent.Add(b.BindInt(
                "Window size", b.Profile.windowSize,
                VadProfileField.WindowSize));
            _detailContent.Add(b.BindInt(
                "Threads", b.Profile.numThreads,
                VadProfileField.NumThreads));
            _detailContent.Add(b.BindText(
                "Provider", b.Profile.provider,
                VadProfileField.Provider));
            _detailContent.Add(b.BindFloat(
                "Buffer size (seconds)", b.Profile.bufferSizeInSeconds,
                VadProfileField.BufferSizeInSeconds));
        }

        private void BuildModelFieldsSection(
            VadProfile profile, VadProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            VadProfileFieldBuilder.BuildModelFields(_detailContent, b);
        }

        private void BuildRemoteSection(VadProfile profile, VadProfileFieldBinder b)
        {
            if (profile.modelSource != ModelSource.Remote)
                return;

            AddSectionHeader("Remote");
            _detailContent.Add(b.BindText(
                "Base URL", profile.remoteBaseUrl, VadProfileField.RemoteBaseUrl));
            _detailContent.Add(ModelSourceSectionBuilder.BuildArchiveUrlPreview(
                profile.remoteBaseUrl, profile.profileName));
        }

        private void BuildLocalZipSection(VadProfile profile)
        {
            if (profile.modelSource != ModelSource.LocalZip)
                return;

            AddSectionHeader("Local Zip");

            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            var result = ModelSourceSectionBuilder.BuildLocalZip(_detailContent, modelDir);

            if (result.PackButton != null)
            {
                _packZipButton = result.PackButton;
                _packZipButton.clicked += HandlePackToZipClicked;
            }
            if (result.DeleteButton != null)
            {
                _deleteZipButton = result.DeleteButton;
                _deleteZipButton.clicked += HandleDeleteZipClicked;
            }
        }

        private void AddSectionHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("model-section-header");
            _detailContent.Add(header);
        }

        // ── Handlers ──

        private void HandleAutoConfigureClicked()
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;

            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            VadProfileAutoFiller.Fill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleNameFocusOut(FocusOutEvent evt)
        {
            _listPresenter?.RefreshList();
        }

        private void HandleModelTypeChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;

            profile.modelType = (VadModelType)evt.newValue;

            AdjustWindowSizeForModelType(profile);

            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleModelSourceChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;
            profile.modelSource = (ModelSource)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandlePackToZipClicked()
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;

            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            ModelFileService.PackToZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleDeleteZipClicked()
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;

            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            ModelFileService.DeleteZip(modelDir);
            RefreshAfterAssetChange();
        }

        // ── Helpers ──

        private static void AdjustWindowSizeForModelType(VadProfile profile)
        {
            switch (profile.modelType)
            {
                case VadModelType.SileroVad:
                    profile.windowSize = 512;
                    break;
                case VadModelType.TenVad:
                    profile.windowSize = 256;
                    break;
            }
        }

        private bool TryGetCurrentProfile(out VadProfile profile)
        {
            profile = null;
            if (_currentIndex < 0 || _currentIndex >= _settings.data.profiles.Count)
                return false;

            profile = _settings.data.profiles[_currentIndex];
            return true;
        }

        private void RefreshAfterAssetChange()
        {
            if (_disposed) return;

            int idx = _currentIndex;
            AssetDatabase.Refresh();
            EditorApplication.delayCall += HandleDelayedRefresh;

            void HandleDelayedRefresh()
            {
                if (_disposed) return;
                ShowProfile(idx);
            }
        }
    }
}
