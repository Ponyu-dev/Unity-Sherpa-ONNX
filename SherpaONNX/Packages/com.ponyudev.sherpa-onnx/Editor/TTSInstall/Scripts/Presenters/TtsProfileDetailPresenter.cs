using System;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
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

        private TtsProfileListPresenter _listPresenter;
        private int _currentIndex = -1;
        private bool _disposed;

        internal TtsProfileDetailPresenter(
            VisualElement detailContent,
            TtsProjectSettings settings)
        {
            _detailContent = detailContent;
            _settings = settings;
        }

        internal void SetListPresenter(TtsProfileListPresenter listPresenter)
        {
            _listPresenter = listPresenter;
        }

        internal void ShowProfile(int index)
        {
            _currentIndex = index;
            _detailContent.Clear();

            if (index < 0 || index >= _settings.data.profiles.Count)
                return;

            TtsProfile profile = _settings.data.profiles[index];
            var binder = new ProfileFieldBinder(profile, _settings);

            BuildAutoConfigureButton(profile);
            BuildInt8SwitchButton(profile);
            BuildIdentitySection(profile, binder);
            BuildCommonSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile, binder);
            BuildLocalZipSection(profile);
        }

        internal void Clear()
        {
            _currentIndex = -1;
            _detailContent.Clear();
        }

        public void Dispose()
        {
            _disposed = true;
            Clear();
        }

        // ── Sections ──

        private void BuildAutoConfigureButton(TtsProfile profile)
        {
            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir))
                return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("tts-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void BuildInt8SwitchButton(TtsProfile profile)
        {
            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;
            if (!TtsInt8Switcher.HasInt8Alternative(profile, modelDir)) return;

            bool usingInt8 = TtsInt8Switcher.IsUsingInt8(profile);
            string label = usingInt8 ? "Use normal models" : "Use int8 models";

            var button = new Button { text = label };
            button.AddToClassList("btn");
            button.AddToClassList(usingInt8 ? "btn-secondary" : "btn-accent");
            button.AddToClassList("tts-btn-spaced");
            button.clicked += HandleInt8SwitchClicked;
            _detailContent.Add(button);
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
            header.AddToClassList("tts-section-header");
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
            if (profile.modelSource != TtsModelSource.Remote)
                return;

            AddSectionHeader("Remote");
            _detailContent.Add(b.BindText(
                "Base URL", profile.remoteBaseUrl, ProfileField.RemoteBaseUrl));

            string archiveUrl = string.IsNullOrEmpty(profile.remoteBaseUrl)
                ? "(set Base URL first)"
                : profile.remoteBaseUrl.TrimEnd('/') + "/" + profile.profileName + ".zip";

            var urlPreview = new Label("Archive URL: " + archiveUrl);
            urlPreview.AddToClassList("tts-url-preview");
            _detailContent.Add(urlPreview);
        }

        private void BuildLocalZipSection(TtsProfile profile)
        {
            if (profile.modelSource != TtsModelSource.LocalZip)
                return;

            AddSectionHeader("Local Zip");

            var infoLabel = new Label(
                "Model files will be zipped at build time and extracted " +
                "from StreamingAssets to persistentDataPath on first launch.");
            infoLabel.AddToClassList("tts-info-label");
            _detailContent.Add(infoLabel);

            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir))
                return;

            if (ModelFileService.ZipExists(modelDir))
            {
                var deleteButton = new Button { text = "Delete zip" };
                deleteButton.AddToClassList("btn");
                deleteButton.AddToClassList("btn-secondary");
                deleteButton.clicked += HandleDeleteZipClicked;
                _detailContent.Add(deleteButton);
            }
            else
            {
                var packButton = new Button { text = "Pack to zip (test)" };
                packButton.AddToClassList("btn");
                packButton.AddToClassList("btn-primary");
                packButton.clicked += HandlePackToZipClicked;
                _detailContent.Add(packButton);
            }
        }

        // ── Handlers ──

        private void HandleAutoConfigureClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            TtsProfileAutoFiller.Fill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleInt8SwitchClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            if (TtsInt8Switcher.IsUsingInt8(profile))
                TtsInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                TtsInt8Switcher.SwitchToInt8(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandlePackToZipClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
            ModelFileService.PackToZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleDeleteZipClicked()
        {
            if (!TryGetCurrentProfile(out TtsProfile profile)) return;

            string modelDir = TtsModelPaths.GetModelDir(profile.profileName);
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

            profile.modelSource = (TtsModelSource)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }
    }
}