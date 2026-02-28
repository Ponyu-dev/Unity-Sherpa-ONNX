using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.Common.Presenters
{
    /// <summary>
    /// Shared logic for profile detail presenters (ASR, Online ASR, TTS, VAD).
    /// Handles identity section, redownload, auto-configure, local-zip,
    /// remote section, and common lifecycle (Clear / Dispose).
    /// Subclasses provide type-specific sections via <see cref="BuildProfileSections"/>.
    /// </summary>
    internal abstract class ProfileDetailPresenterBase<TProfile, TSettings> : IDisposable
        where TProfile : class, IModelProfile, new()
        where TSettings : ISaveableSettings
    {
        protected readonly VisualElement _detailContent;
        protected readonly TSettings _settings;

        private ProfileListPresenter<TProfile> _listPresenter;
        private Button _redownloadButton;
        private Button _int8Button;
        private Button _packZipButton;
        private Button _deleteZipButton;
        protected int _currentIndex = -1;
        protected bool _disposed;

        protected ProfileDetailPresenterBase(
            VisualElement detailContent, TSettings settings)
        {
            _detailContent = detailContent;
            _settings = settings;
        }

        // ── Abstract ──

        protected abstract IReadOnlyList<TProfile> Profiles { get; }
        protected abstract Func<string, string> GetModelDirFunc { get; }
        protected abstract void AutoFill(TProfile profile, string modelDir);
        protected abstract Task RedownloadCoreAsync(TProfile profile, ModelRedownloader redownloader);
        protected abstract void SetModelType(TProfile profile, Enum value);
        protected abstract void BuildProfileSections(TProfile profile);

        // ── Virtual (Int8 support — override in subclasses that need it) ──

        protected virtual bool HasInt8Alternative(TProfile profile, string modelDir) => false;
        protected virtual bool IsUsingInt8(TProfile profile) => false;
        protected virtual void ToggleInt8(TProfile profile, string modelDir) { }

        // ── Public API ──

        internal void SetListPresenter(ProfileListPresenter<TProfile> lp)
            => _listPresenter = lp;

        internal void ShowProfile(int index)
        {
            UnsubscribeRedownload();
            UnsubscribeInt8Button();
            UnsubscribeZipButtons();
            _currentIndex = index;
            _detailContent.Clear();

            if (!TryGetCurrentProfile(out TProfile profile))
                return;

            var mp = (IModelProfile)profile;
            _redownloadButton = MissingFilesWarningBuilder.Build(
                _detailContent, profile.ProfileName, GetModelDirFunc,
                !string.IsNullOrEmpty(mp.SourceUrl));
            if (_redownloadButton != null)
                _redownloadButton.clicked += HandleRedownloadClicked;

            BuildAutoConfigureButton(profile);
            BuildProfileSections(profile);
        }

        internal void Clear()
        {
            UnsubscribeRedownload();
            UnsubscribeInt8Button();
            UnsubscribeZipButtons();
            _currentIndex = -1;
            _detailContent.Clear();
        }

        public void Dispose()
        {
            _disposed = true;
            Clear();
        }

        // ── Protected helpers (called from subclass BuildProfileSections) ──

        protected bool TryGetCurrentProfile(out TProfile profile)
        {
            profile = null;
            var profiles = Profiles;
            if (_currentIndex < 0 || _currentIndex >= profiles.Count)
                return false;

            profile = profiles[_currentIndex];
            return true;
        }

        protected void AddSectionHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("model-section-header");
            _detailContent.Add(header);
        }

        protected void BuildInt8SwitchButton(TProfile profile)
        {
            string modelDir = GetModelDirFunc(profile.ProfileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;
            if (!HasInt8Alternative(profile, modelDir)) return;

            bool usingInt8 = IsUsingInt8(profile);
            string label = usingInt8 ? "Use normal models" : "Use int8 models";
            var button = new Button { text = label };
            button.AddToClassList("btn");
            button.AddToClassList(usingInt8 ? "btn-secondary" : "btn-accent");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleInt8SwitchClicked;
            _int8Button = button;
            _detailContent.Add(button);

            if (usingInt8)
            {
                _detailContent.Add(new HelpBox(
                    "INT8 models are not supported on all devices. " +
                    "If the model fails to load, a warning will " +
                    "appear in the console before the crash. " +
                    "Switch to normal models if you experience issues.",
                    HelpBoxMessageType.Warning));
            }
        }

        protected void BuildVersionWarning(Enum modelType)
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

        protected void BuildIdentitySection(
            VisualElement boundNameField,
            Enum modelType,
            VisualElement boundSourceUrlField)
        {
            boundNameField.RegisterCallback<FocusOutEvent>(HandleNameFocusOut);
            _detailContent.Add(boundNameField);

            var typeField = new EnumField("Model type", modelType);
            typeField.RegisterValueChangedCallback(HandleModelTypeChanged);
            _detailContent.Add(typeField);

            if (!TryGetCurrentProfile(out TProfile profile))
                return;

            var mp = (IModelProfile)profile;

            var sourceField = new EnumField("Model source", mp.ModelSource);
            sourceField.RegisterValueChangedCallback(HandleModelSourceChanged);
            _detailContent.Add(sourceField);

            if (boundSourceUrlField is TextField tf && !string.IsNullOrEmpty(mp.SourceUrl))
                tf.isReadOnly = true;
            _detailContent.Add(boundSourceUrlField);
        }

        protected void BuildRemoteSection(TProfile profile, VisualElement boundUrlField)
        {
            var mp = (IModelProfile)profile;
            if (mp.ModelSource != ModelSource.Remote)
                return;

            AddSectionHeader("Remote");
            _detailContent.Add(boundUrlField);
            _detailContent.Add(ModelSourceSectionBuilder.BuildArchiveUrlPreview(
                mp.RemoteBaseUrl, profile.ProfileName));
        }

        protected void BuildLocalZipSection(TProfile profile)
        {
            var mp = (IModelProfile)profile;
            if (mp.ModelSource != ModelSource.LocalZip)
                return;

            AddSectionHeader("Local Zip");

            string modelDir = GetModelDirFunc(profile.ProfileName);
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

        protected void RefreshAfterAssetChange()
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

        // ── Event handlers (register from subclass BuildIdentitySection calls) ──

        protected void HandleNameFocusOut(FocusOutEvent evt)
            => _listPresenter?.RefreshList();

        protected void HandleModelTypeChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            SetModelType(profile, evt.newValue);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleInt8SwitchClicked()
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            string modelDir = GetModelDirFunc(profile.ProfileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            ToggleInt8(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        protected void HandleModelSourceChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            ((IModelProfile)profile).ModelSource = (ModelSource)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        // ── Private ──

        private void BuildAutoConfigureButton(TProfile profile)
        {
            string modelDir = GetModelDirFunc(profile.ProfileName);
            if (!ModelFileService.ModelDirExists(modelDir))
                return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void HandleAutoConfigureClicked()
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            string modelDir = GetModelDirFunc(profile.ProfileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            AutoFill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private async void HandleRedownloadClicked()
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            var mp = (IModelProfile)profile;
            if (string.IsNullOrEmpty(mp.SourceUrl)) return;

            try
            {
                using var redownloader = new ModelRedownloader();
                await RedownloadCoreAsync(profile, redownloader);
                _settings.SaveSettings();
                AssetDatabase.Refresh();
                _listPresenter?.RefreshList();
                ShowProfile(_currentIndex);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.EditorError($"[SherpaOnnx] Re-download failed: {ex}");
            }
        }

        private void HandlePackToZipClicked()
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            string modelDir = GetModelDirFunc(profile.ProfileName);
            ModelFileService.PackToZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleDeleteZipClicked()
        {
            if (!TryGetCurrentProfile(out TProfile profile)) return;
            string modelDir = GetModelDirFunc(profile.ProfileName);
            ModelFileService.DeleteZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void UnsubscribeInt8Button()
        {
            if (_int8Button != null)
                _int8Button.clicked -= HandleInt8SwitchClicked;
            _int8Button = null;
        }

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
    }
}
