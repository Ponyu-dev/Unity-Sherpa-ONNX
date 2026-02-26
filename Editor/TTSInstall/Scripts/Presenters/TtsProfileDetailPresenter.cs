using System;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    /// <summary>
    /// Renders full profile editing UI.
    /// Common fields are always shown; model-specific fields
    /// rebuild when model type changes.
    /// </summary>
    internal sealed class TtsProfileDetailPresenter : IDisposable
    {
        private readonly TtsProjectSettings _settings;
        private readonly VisualElement _detailContent;

        private ProfileListPresenter<TtsProfile> _listPresenter;
        private Button _redownloadButton;
        private Button _packZipButton;
        private Button _deleteZipButton;
        private int _currentIndex = -1;
        private bool _disposed;

        internal TtsProfileDetailPresenter(
            VisualElement detailContent,
            TtsProjectSettings settings)
        {
            _detailContent = detailContent;
            _settings = settings;
        }

        internal void SetListPresenter(
            ProfileListPresenter<TtsProfile> listPresenter)
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

            TtsProfile profile = _settings.data.profiles[index];
            var binder = new ProfileFieldBinder(profile, _settings);

            _redownloadButton = MissingFilesWarningBuilder.Build(_detailContent, profile.profileName, ModelPaths.GetTtsModelDir, !string.IsNullOrEmpty(profile.sourceUrl));
            if (_redownloadButton != null)
                _redownloadButton.clicked += HandleRedownloadClicked;
            BuildAutoConfigureButton(profile);
            BuildInt8SwitchButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(profile, binder);
            BuildCommonSection(binder);
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
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;
            if (string.IsNullOrEmpty(profile.sourceUrl)) return;

            try
            {
                using var redownloader = new ModelRedownloader();
                string destDir = await redownloader.RedownloadArchiveAsync(profile.sourceUrl, ModelPaths.GetTtsModelDir, default);
                TtsProfileAutoFiller.Fill(profile, destDir);
                _settings.SaveSettings();
                AssetDatabase.Refresh();
                _listPresenter?.RefreshList();
                ShowProfile(_currentIndex);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.EditorError($"[SherpaOnnx] TTS re-download failed: {ex}");
            }
        }

        // ── Sections ──

        private void BuildAutoConfigureButton(TtsProfile profile)
        {
            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir))
                return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void BuildInt8SwitchButton(TtsProfile profile)
        {
            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;
            if (!TtsInt8Switcher.HasInt8Alternative(profile, modelDir)) return;

            bool usingInt8 = TtsInt8Switcher.IsUsingInt8(profile);
            string label = usingInt8 ? "Use normal models" : "Use int8 models";

            var button = new Button { text = label };
            button.AddToClassList("btn");
            button.AddToClassList(usingInt8 ? "btn-secondary" : "btn-accent");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleInt8SwitchClicked;
            _detailContent.Add(button);

            if (usingInt8)
            {
                _detailContent.Add(new HelpBox(
                    "INT8 models are not supported on all " +
                    "devices. If the model fails to load, a " +
                    "warning will appear in the console before " +
                    "the crash. Switch to normal models if you " +
                    "experience issues.",
                    HelpBoxMessageType.Warning));
            }
        }

        private void BuildVersionWarning(TtsModelType modelType)
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

        private void BuildIdentitySection(TtsProfile profile, ProfileFieldBinder binder)
        {
            var nameField = binder.BindText("Profile name", profile.profileName, ProfileField.ProfileName);
            nameField.RegisterCallback<FocusOutEvent>(HandleNameFocusOut);
            _detailContent.Add(nameField);

            var typeField = new EnumField("Model type", profile.modelType);
            typeField.RegisterValueChangedCallback(HandleModelTypeChanged);
            _detailContent.Add(typeField);

            var sourceField = new EnumField("Model source", profile.modelSource);
            sourceField.RegisterValueChangedCallback(HandleModelSourceChanged);
            _detailContent.Add(sourceField);

            var urlField = binder.BindText("Source URL", profile.sourceUrl, ProfileField.SourceUrl);
            if (!string.IsNullOrEmpty(profile.sourceUrl))
                urlField.isReadOnly = true;
            _detailContent.Add(urlField);
        }

        private void BuildCommonSection(ProfileFieldBinder b)
        {
            AddSectionHeader("Generation");
            _detailContent.Add(b.BindInt(
                "Speaker ID", b.Profile.speakerId, ProfileField.SpeakerId));
            _detailContent.Add(b.BindFloat(
                "Speed", b.Profile.speed, ProfileField.Speed));

            AddSectionHeader("Text Processing");
            _detailContent.Add(b.BindText(
                "Rule FSTs", b.Profile.ruleFsts, ProfileField.RuleFsts));
            _detailContent.Add(b.BindText(
                "Rule FARs", b.Profile.ruleFars, ProfileField.RuleFars));
            _detailContent.Add(b.BindInt(
                "Max sentences", b.Profile.maxNumSentences, ProfileField.MaxNumSentences));
            _detailContent.Add(b.BindFloat(
                "Silence scale", b.Profile.silenceScale, ProfileField.SilenceScale));

            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt(
                "Threads", b.Profile.numThreads, ProfileField.NumThreads));
            _detailContent.Add(b.BindText(
                "Provider", b.Profile.provider, ProfileField.Provider));
        }

        private void AddSectionHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("model-section-header");
            _detailContent.Add(header);
        }

        private void BuildModelFieldsSection(TtsProfile profile, ProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");

            switch (profile.modelType)
            {
                case TtsModelType.Vits:
                    ProfileFieldBuilder.BuildVits(_detailContent, b);
                    break;
                case TtsModelType.Matcha:
                    ProfileFieldBuilder.BuildMatcha(
                        _detailContent, b, _settings, HandleRefreshProfile);
                    break;
                case TtsModelType.Kokoro:
                    ProfileFieldBuilder.BuildKokoro(_detailContent, b);
                    break;
                case TtsModelType.Kitten:
                    ProfileFieldBuilder.BuildKitten(_detailContent, b);
                    break;
                case TtsModelType.ZipVoice:
                    ProfileFieldBuilder.BuildZipVoice(_detailContent, b);
                    break;
                case TtsModelType.Pocket:
                    ProfileFieldBuilder.BuildPocket(_detailContent, b);
                    break;
            }
        }

        private void BuildRemoteSection(TtsProfile profile, ProfileFieldBinder b)
        {
            if (profile.modelSource != ModelSource.Remote)
                return;

            AddSectionHeader("Remote");
            _detailContent.Add(b.BindText(
                "Base URL", profile.remoteBaseUrl, ProfileField.RemoteBaseUrl));
            _detailContent.Add(ModelSourceSectionBuilder.BuildArchiveUrlPreview(
                profile.remoteBaseUrl, profile.profileName));
        }

        private void BuildLocalZipSection(TtsProfile profile)
        {
            if (profile.modelSource != ModelSource.LocalZip)
                return;

            AddSectionHeader("Local Zip");

            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
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

        // ── Handlers ──

        private void HandleAutoConfigureClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            TtsProfileAutoFiller.Fill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleInt8SwitchClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            bool wasInt8 = TtsInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                TtsInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                TtsInt8Switcher.SwitchToInt8(profile, modelDir);
            profile.allowInt8 = !wasInt8;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandlePackToZipClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
            ModelFileService.PackToZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleDeleteZipClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = ModelPaths.GetTtsModelDir(profile.profileName);
            ModelFileService.DeleteZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleRefreshProfile()
        {
            ShowProfile(_currentIndex);
        }

        private void HandleNameFocusOut(FocusOutEvent evt)
        {
            _listPresenter?.RefreshList();
        }

        // ── Helpers ──

        private bool TryGetCurrentProfile(out TtsProfile profile)
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

        private void HandleModelTypeChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            profile.modelType = (TtsModelType)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleModelSourceChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            profile.modelSource = (ModelSource)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }
    }
}