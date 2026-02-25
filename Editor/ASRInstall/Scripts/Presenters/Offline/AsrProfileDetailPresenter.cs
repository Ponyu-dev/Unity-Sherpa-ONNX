using System;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Common.Data;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.LibraryInstall;
using UnityEditor;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    internal sealed class AsrProfileDetailPresenter : IDisposable
    {
        private readonly AsrProjectSettings _settings;
        private readonly VisualElement _detailContent;

        private ProfileListPresenter<AsrProfile> _listPresenter;
        private Button _redownloadButton;
        private Button _packZipButton;
        private Button _deleteZipButton;
        private int _currentIndex = -1;
        private bool _disposed;

        internal AsrProfileDetailPresenter(VisualElement detailContent, AsrProjectSettings settings)
        {
            _detailContent = detailContent;
            _settings = settings;
        }

        internal void SetListPresenter(ProfileListPresenter<AsrProfile> lp)
        {
            _listPresenter = lp;
        }

        internal void ShowProfile(int index)
        {
            UnsubscribeRedownload();
            UnsubscribeZipButtons();
            _currentIndex = index;
            _detailContent.Clear();

            if (index < 0 || index >= _settings.offlineData.profiles.Count)
                return;

            AsrProfile profile = _settings.offlineData.profiles[index];
            var binder = new AsrProfileFieldBinder(profile, _settings);

            _redownloadButton = MissingFilesWarningBuilder.Build(_detailContent, profile.profileName, AsrModelPaths.GetModelDir, !string.IsNullOrEmpty(profile.sourceUrl));
            if (_redownloadButton != null)
                _redownloadButton.clicked += HandleRedownloadClicked;
            BuildAutoConfigureButton(profile);
            BuildInt8SwitchButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(profile, binder);
            BuildCommonSection(binder);
            BuildFeatureSection(binder);
            BuildRecognizerSection(binder);
            BuildLmSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile, binder);
            BuildLocalZipSection(profile);
            BuildPoolSizeSection();
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
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;
            if (string.IsNullOrEmpty(profile.sourceUrl)) return;

            try
            {
                using var redownloader = new ModelRedownloader();
                string destDir = await redownloader.RedownloadArchiveAsync(profile.sourceUrl, AsrModelPaths.GetModelDir, default);
                AsrProfileAutoFiller.Fill(profile, destDir);
                _settings.SaveSettings();
                AssetDatabase.Refresh();
                _listPresenter?.RefreshList();
                ShowProfile(_currentIndex);
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.EditorError($"[SherpaOnnx] ASR re-download failed: {ex}");
            }
        }

        private void BuildAutoConfigureButton(AsrProfile profile)
        {
            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void BuildInt8SwitchButton(AsrProfile profile)
        {
            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;
            if (!AsrInt8Switcher.HasInt8Alternative(profile, modelDir))
                return;

            bool usingInt8 = AsrInt8Switcher.IsUsingInt8(profile);
            string label = usingInt8
                ? "Use normal models"
                : "Use int8 models";

            var button = new Button { text = label };
            button.AddToClassList("btn");
            button.AddToClassList(usingInt8 ? "btn-secondary" : "btn-accent");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleInt8SwitchClicked;
            _detailContent.Add(button);

            if (usingInt8)
            {
                _detailContent.Add(new HelpBox(
                    "INT8 models are not supported on all devices. " +
                    "If the model fails to load, a warning will " +
                    "appear in the console before the crash. " +
                    "Switch to normal models if you experience " +
                    "issues.",
                    HelpBoxMessageType.Warning));
            }
        }

        private void BuildVersionWarning(AsrModelType modelType)
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
            AsrProfile profile, AsrProfileFieldBinder binder)
        {
            var nameField = binder.BindText("Profile name",
                profile.profileName, AsrProfileField.ProfileName);
            nameField.RegisterCallback<FocusOutEvent>(HandleNameFocusOut);
            _detailContent.Add(nameField);

            var typeField = new EnumField("Model type", profile.modelType);
            typeField.RegisterValueChangedCallback(HandleModelTypeChanged);
            _detailContent.Add(typeField);

            var sourceField = new EnumField("Model source", profile.modelSource);
            sourceField.RegisterValueChangedCallback(HandleModelSourceChanged);
            _detailContent.Add(sourceField);

            var urlField = binder.BindText("Source URL", profile.sourceUrl, AsrProfileField.SourceUrl);
            if (!string.IsNullOrEmpty(profile.sourceUrl))
                urlField.isReadOnly = true;
            _detailContent.Add(urlField);
        }

        private void BuildCommonSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt("Threads",
                b.Profile.numThreads, AsrProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider",
                b.Profile.provider, AsrProfileField.Provider));
            _detailContent.Add(b.BindText("Tokens",
                b.Profile.tokens, AsrProfileField.Tokens));
        }

        private void BuildFeatureSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Feature");
            _detailContent.Add(b.BindInt("Sample rate",
                b.Profile.sampleRate, AsrProfileField.SampleRate));
            _detailContent.Add(b.BindInt("Feature dim",
                b.Profile.featureDim, AsrProfileField.FeatureDim));
        }

        private void BuildRecognizerSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Recognizer");
            _detailContent.Add(b.BindText("Decoding method",
                b.Profile.decodingMethod,
                AsrProfileField.DecodingMethod));
            _detailContent.Add(b.BindInt("Max active paths",
                b.Profile.maxActivePaths,
                AsrProfileField.MaxActivePaths));
            _detailContent.Add(b.BindText("Hotwords file",
                b.Profile.hotwordsFile,
                AsrProfileField.HotwordsFile));
            _detailContent.Add(b.BindFloat("Hotwords score",
                b.Profile.hotwordsScore,
                AsrProfileField.HotwordsScore));
            _detailContent.Add(b.BindText("Rule FSTs",
                b.Profile.ruleFsts, AsrProfileField.RuleFsts));
            _detailContent.Add(b.BindText("Rule FARs",
                b.Profile.ruleFars, AsrProfileField.RuleFars));
            _detailContent.Add(b.BindFloat("Blank penalty",
                b.Profile.blankPenalty,
                AsrProfileField.BlankPenalty));
        }

        private void BuildLmSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Language Model");
            _detailContent.Add(b.BindText("LM model", b.Profile.lmModel, AsrProfileField.LmModel));
            _detailContent.Add(b.BindFloat("LM scale", b.Profile.lmScale, AsrProfileField.LmScale));
        }

        private void BuildModelFieldsSection(
            AsrProfile profile, AsrProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            switch (profile.modelType)
            {
                case AsrModelType.Transducer:
                    AsrProfileFieldBuilder.BuildTransducer(_detailContent, b);
                    break;
                case AsrModelType.Paraformer:
                    AsrProfileFieldBuilder.BuildParaformer(_detailContent, b);
                    break;
                case AsrModelType.Whisper:
                    AsrProfileFieldBuilder.BuildWhisper(_detailContent, b);
                    break;
                case AsrModelType.SenseVoice:
                    AsrProfileFieldBuilder.BuildSenseVoice(_detailContent, b);
                    break;
                case AsrModelType.Moonshine:
                    AsrProfileFieldBuilder.BuildMoonshine(_detailContent, b);
                    break;
                case AsrModelType.NemoCtc:
                    AsrProfileFieldBuilder.BuildNemoCtc(_detailContent, b);
                    break;
                case AsrModelType.ZipformerCtc:
                    AsrProfileFieldBuilder.BuildZipformerCtc(_detailContent, b);
                    break;
                case AsrModelType.Tdnn:
                    AsrProfileFieldBuilder.BuildTdnn(_detailContent, b);
                    break;
                case AsrModelType.FireRedAsr:
                    AsrProfileFieldBuilder.BuildFireRedAsr(_detailContent, b);
                    break;
                case AsrModelType.Dolphin:
                    AsrProfileFieldBuilder.BuildDolphin(_detailContent, b);
                    break;
                case AsrModelType.Canary:
                    AsrProfileFieldBuilder.BuildCanary(_detailContent, b);
                    break;
                case AsrModelType.WenetCtc:
                    AsrProfileFieldBuilder.BuildWenetCtc(_detailContent, b);
                    break;
                case AsrModelType.Omnilingual:
                    AsrProfileFieldBuilder.BuildOmnilingual(_detailContent, b);
                    break;
                case AsrModelType.MedAsr:
                    AsrProfileFieldBuilder.BuildMedAsr(_detailContent, b);
                    break;
                case AsrModelType.FunAsrNano:
                    AsrProfileFieldBuilder.BuildFunAsrNano(_detailContent, b);
                    break;
            }
        }

        private void BuildPoolSizeSection()
        {
            AddSectionHeader("Engine Pool");
            var poolField = new IntegerField("Pool size")
            {
                value = _settings.offlineData.offlineRecognizerPoolSize
            };
            var handler = new PoolSizeHandler(_settings);
            poolField.RegisterValueChangedCallback(handler.Handle);
            _detailContent.Add(poolField);
        }

        private void BuildRemoteSection(AsrProfile profile, AsrProfileFieldBinder b)
        {
            if (profile.modelSource != ModelSource.Remote)
                return;

            AddSectionHeader("Remote");
            _detailContent.Add(b.BindText(
                "Base URL", profile.remoteBaseUrl, AsrProfileField.RemoteBaseUrl));
            _detailContent.Add(ModelSourceSectionBuilder.BuildArchiveUrlPreview(
                profile.remoteBaseUrl, profile.profileName));
        }

        private void BuildLocalZipSection(AsrProfile profile)
        {
            if (profile.modelSource != ModelSource.LocalZip)
                return;

            AddSectionHeader("Local Zip");

            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
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

        private void HandleAutoConfigureClicked()
        {
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;

            string modelDir = AsrModelPaths.GetModelDir(
                profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            AsrProfileAutoFiller.Fill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleInt8SwitchClicked()
        {
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;

            string modelDir = AsrModelPaths.GetModelDir(
                profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            bool wasInt8 = AsrInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                AsrInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                AsrInt8Switcher.SwitchToInt8(profile, modelDir);
            profile.allowInt8 = !wasInt8;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandlePackToZipClicked()
        {
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;

            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
            ModelFileService.PackToZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleDeleteZipClicked()
        {
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;

            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
            ModelFileService.DeleteZip(modelDir);
            RefreshAfterAssetChange();
        }

        private void HandleNameFocusOut(FocusOutEvent evt) =>
            _listPresenter?.RefreshList();

        private void HandleModelTypeChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;
            profile.modelType = (AsrModelType)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleModelSourceChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out AsrProfile profile)) return;
            profile.modelSource = (ModelSource)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        // ── Helpers ──

        private bool TryGetCurrentProfile(out AsrProfile profile)
        {
            profile = null;
            if (_currentIndex < 0 || _currentIndex >= _settings.offlineData.profiles.Count)
                return false;

            profile = _settings.offlineData.profiles[_currentIndex];
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

        private sealed class PoolSizeHandler
        {
            private readonly AsrProjectSettings _s;

            internal PoolSizeHandler(AsrProjectSettings s)
            {
                _s = s;
            }

            internal void Handle(ChangeEvent<int> evt)
            {
                int val = Math.Max(1, evt.newValue);
                _s.offlineData.offlineRecognizerPoolSize = val;
                _s.SaveSettings();
            }
        }
    }
}