using System;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    internal sealed class OnlineAsrProfileDetailPresenter : IDisposable
    {
        private readonly AsrProjectSettings _settings;
        private readonly VisualElement _detailContent;

        private ProfileListPresenter<OnlineAsrProfile> _listPresenter;
        private int _currentIndex = -1;

        internal OnlineAsrProfileDetailPresenter(
            VisualElement detailContent, AsrProjectSettings settings)
        { _detailContent = detailContent; _settings = settings; }

        internal void SetListPresenter(
            ProfileListPresenter<OnlineAsrProfile> listPresenter)
        { _listPresenter = listPresenter; }

        internal void ShowProfile(int index)
        {
            _currentIndex = index;
            _detailContent.Clear();
            if (index < 0 || index >= _settings.onlineData.profiles.Count)
                return;

            OnlineAsrProfile profile = _settings.onlineData.profiles[index];
            var binder = new OnlineAsrProfileFieldBinder(profile, _settings);

            BuildAutoConfigureButton(profile);
            BuildInt8SwitchButton(profile);
            BuildIdentitySection(profile, binder);
            BuildCommonSection(binder);
            BuildFeatureSection(binder);
            BuildRecognizerSection(binder);
            BuildEndpointSection(profile, binder);
            BuildCtcFstDecoderSection(binder);
            BuildModelFieldsSection(profile, binder);
        }

        internal void Clear()
        {
            _currentIndex = -1;
            _detailContent.Clear();
        }

        public void Dispose() => Clear();

        private void BuildAutoConfigureButton(OnlineAsrProfile profile)
        {
            string modelDir = AsrModelPaths.GetModelDir(
                profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void BuildInt8SwitchButton(
            OnlineAsrProfile profile)
        {
            string modelDir = AsrModelPaths.GetModelDir(
                profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;
            if (!OnlineAsrInt8Switcher.HasInt8Alternative(
                profile, modelDir)) return;

            bool usingInt8 =
                OnlineAsrInt8Switcher.IsUsingInt8(profile);
            string label = usingInt8
                ? "Use normal models" : "Use int8 models";

            var button = new Button { text = label };
            button.AddToClassList("btn");
            button.AddToClassList(
                usingInt8 ? "btn-secondary" : "btn-accent");
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

        private void BuildIdentitySection(
            OnlineAsrProfile profile,
            OnlineAsrProfileFieldBinder binder)
        {
            var nameField = binder.BindText("Profile name",
                profile.profileName,
                OnlineAsrProfileField.ProfileName);
            nameField.RegisterCallback<FocusOutEvent>(
                HandleNameFocusOut);
            _detailContent.Add(nameField);

            var typeField = new EnumField(
                "Model type", profile.modelType);
            typeField.RegisterValueChangedCallback(
                HandleModelTypeChanged);
            _detailContent.Add(typeField);
        }

        private void BuildCommonSection(
            OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt("Threads",
                b.Profile.numThreads,
                OnlineAsrProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider",
                b.Profile.provider,
                OnlineAsrProfileField.Provider));
            _detailContent.Add(b.BindText("Tokens",
                b.Profile.tokens,
                OnlineAsrProfileField.Tokens));
        }

        private void BuildFeatureSection(
            OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Feature");
            _detailContent.Add(b.BindInt("Sample rate",
                b.Profile.sampleRate,
                OnlineAsrProfileField.SampleRate));
            _detailContent.Add(b.BindInt("Feature dim",
                b.Profile.featureDim,
                OnlineAsrProfileField.FeatureDim));
        }

        private void BuildRecognizerSection(
            OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Recognizer");
            _detailContent.Add(b.BindText("Decoding method",
                b.Profile.decodingMethod,
                OnlineAsrProfileField.DecodingMethod));
            _detailContent.Add(b.BindInt("Max active paths",
                b.Profile.maxActivePaths,
                OnlineAsrProfileField.MaxActivePaths));
            _detailContent.Add(b.BindText("Hotwords file",
                b.Profile.hotwordsFile,
                OnlineAsrProfileField.HotwordsFile));
            _detailContent.Add(b.BindFloat("Hotwords score",
                b.Profile.hotwordsScore,
                OnlineAsrProfileField.HotwordsScore));
            _detailContent.Add(b.BindText("Rule FSTs",
                b.Profile.ruleFsts,
                OnlineAsrProfileField.RuleFsts));
            _detailContent.Add(b.BindText("Rule FARs",
                b.Profile.ruleFars,
                OnlineAsrProfileField.RuleFars));
            _detailContent.Add(b.BindFloat("Blank penalty",
                b.Profile.blankPenalty,
                OnlineAsrProfileField.BlankPenalty));
        }

        private void BuildEndpointSection(
            OnlineAsrProfile profile, OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Endpoint Detection");
            var toggle = new Toggle("Enable endpoint") { value = profile.enableEndpoint };
            var handler = new EndpointToggleHandler(profile, _settings);
            toggle.RegisterValueChangedCallback(handler.Handle);
            _detailContent.Add(toggle);

            _detailContent.Add(b.BindFloat(
                "Rule 1 min trailing silence",
                b.Profile.rule1MinTrailingSilence,
                OnlineAsrProfileField.Rule1MinTrailingSilence));
            _detailContent.Add(b.BindFloat(
                "Rule 2 min trailing silence",
                b.Profile.rule2MinTrailingSilence,
                OnlineAsrProfileField.Rule2MinTrailingSilence));
            _detailContent.Add(b.BindFloat(
                "Rule 3 min utterance length",
                b.Profile.rule3MinUtteranceLength,
                OnlineAsrProfileField.Rule3MinUtteranceLength));
        }

        private void BuildCtcFstDecoderSection(OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("CtcFstDecoder");
            _detailContent.Add(b.BindText("Graph", b.Profile.ctcFstDecoderGraph, OnlineAsrProfileField.CtcFstDecoderGraph));
            _detailContent.Add(b.BindInt("Max active", b.Profile.ctcFstDecoderMaxActive, OnlineAsrProfileField.CtcFstDecoderMaxActive));
        }

        private void BuildModelFieldsSection(
            OnlineAsrProfile profile, OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            switch (profile.modelType)
            {
                case OnlineAsrModelType.Transducer: OnlineAsrProfileFieldBuilder.BuildTransducer(_detailContent, b); break;
                case OnlineAsrModelType.Paraformer: OnlineAsrProfileFieldBuilder.BuildParaformer(_detailContent, b); break;
                case OnlineAsrModelType.Zipformer2Ctc: OnlineAsrProfileFieldBuilder.BuildZipformer2Ctc(_detailContent, b); break;
                case OnlineAsrModelType.NemoCtc: OnlineAsrProfileFieldBuilder.BuildNemoCtc(_detailContent, b); break;
                case OnlineAsrModelType.ToneCtc: OnlineAsrProfileFieldBuilder.BuildToneCtc(_detailContent, b); break;
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
            if (!TryGetCurrentProfile(out OnlineAsrProfile profile)) return;
            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;
            OnlineAsrProfileAutoFiller.Fill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleInt8SwitchClicked()
        {
            if (!TryGetCurrentProfile(out OnlineAsrProfile profile)) return;
            string modelDir = AsrModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            bool wasInt8 =
                OnlineAsrInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                OnlineAsrInt8Switcher.SwitchToNormal(
                    profile, modelDir);
            else
                OnlineAsrInt8Switcher.SwitchToInt8(
                    profile, modelDir);
            profile.allowInt8 = !wasInt8;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleNameFocusOut(FocusOutEvent evt) =>
            _listPresenter?.RefreshList();

        private void HandleModelTypeChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out OnlineAsrProfile profile))
                return;
            profile.modelType = (OnlineAsrModelType)evt.newValue;
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private bool TryGetCurrentProfile(out OnlineAsrProfile profile)
        {
            profile = null;
            if (_currentIndex < 0
                || _currentIndex >= _settings.onlineData.profiles.Count)
                return false;

            profile = _settings.onlineData.profiles[_currentIndex];
            return true;
        }

        private sealed class EndpointToggleHandler
        {
            private readonly OnlineAsrProfile _p;
            private readonly AsrProjectSettings _s;

            internal EndpointToggleHandler(
                OnlineAsrProfile p, AsrProjectSettings s)
            { _p = p; _s = s; }

            internal void Handle(ChangeEvent<bool> evt)
            {
                _p.enableEndpoint = evt.newValue;
                _s.SaveSettings();
            }
        }
    }
}
