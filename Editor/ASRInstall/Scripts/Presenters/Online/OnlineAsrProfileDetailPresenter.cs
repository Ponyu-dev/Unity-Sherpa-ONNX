using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Online.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Online
{
    internal sealed class OnlineAsrProfileDetailPresenter : ProfileDetailPresenterBase<OnlineAsrProfile, AsrProjectSettings>
    {
        internal OnlineAsrProfileDetailPresenter(VisualElement detailContent, AsrProjectSettings settings)
            : base(detailContent, settings) { }

        protected override IReadOnlyList<OnlineAsrProfile> Profiles
            => _settings.onlineData.profiles;

        protected override Func<string, string> GetModelDirFunc
            => ModelPaths.GetAsrModelDir;

        protected override void AutoFill(OnlineAsrProfile profile, string modelDir)
            => OnlineAsrProfileAutoFiller.Fill(profile, modelDir);

        protected override async Task RedownloadCoreAsync(
            OnlineAsrProfile profile, ModelRedownloader redownloader)
        {
            string destDir = await redownloader.RedownloadArchiveAsync(
                profile.sourceUrl, ModelPaths.GetAsrModelDir, default);
            OnlineAsrProfileAutoFiller.Fill(profile, destDir);
        }

        protected override void SetModelType(OnlineAsrProfile profile, Enum value)
            => profile.modelType = (OnlineAsrModelType)value;

        protected override bool HasInt8Alternative(OnlineAsrProfile profile, string modelDir)
            => OnlineAsrInt8Switcher.HasInt8Alternative(profile, modelDir);

        protected override bool IsUsingInt8(OnlineAsrProfile profile)
            => OnlineAsrInt8Switcher.IsUsingInt8(profile);

        protected override void ToggleInt8(OnlineAsrProfile profile, string modelDir)
        {
            bool wasInt8 = OnlineAsrInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                OnlineAsrInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                OnlineAsrInt8Switcher.SwitchToInt8(profile, modelDir);
            profile.allowInt8 = !wasInt8;
        }

        protected override void BuildProfileSections(OnlineAsrProfile profile)
        {
            string modelDir = GetModelDirFunc(profile.ProfileName);
            var binder = new OnlineAsrProfileFieldBinder(profile, _settings, modelDir);

            BuildInt8SwitchButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(
                binder.BindText("Profile name", profile.profileName, OnlineAsrProfileField.ProfileName),
                profile.modelType,
                binder.BindText("Source URL", profile.sourceUrl, OnlineAsrProfileField.SourceUrl));
            BuildCommonSection(binder);
            BuildFeatureSection(binder);
            BuildRecognizerSection(binder);
            BuildEndpointSection(profile, binder);
            BuildCtcFstDecoderSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile,
                binder.BindText("Base URL", profile.remoteBaseUrl, OnlineAsrProfileField.RemoteBaseUrl));
            BuildLocalZipSection(profile);
        }

        // ── Online ASR-specific sections ──

        private void BuildCommonSection(OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt("Threads", b.Profile.numThreads, OnlineAsrProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider", b.Profile.provider, OnlineAsrProfileField.Provider));
            _detailContent.Add(b.BindFile("Tokens", b.Profile.tokens, OnlineAsrProfileField.Tokens, "txt", "tokens", isRequired: true));
        }

        private void BuildFeatureSection(OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Feature");
            _detailContent.Add(b.BindInt("Sample rate", b.Profile.sampleRate, OnlineAsrProfileField.SampleRate));
            _detailContent.Add(b.BindInt("Feature dim", b.Profile.featureDim, OnlineAsrProfileField.FeatureDim));
        }

        private void BuildRecognizerSection(OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Recognizer");
            _detailContent.Add(b.BindText("Decoding method", b.Profile.decodingMethod, OnlineAsrProfileField.DecodingMethod));
            _detailContent.Add(b.BindInt("Max active paths", b.Profile.maxActivePaths, OnlineAsrProfileField.MaxActivePaths));
            _detailContent.Add(b.BindFile("Hotwords file", b.Profile.hotwordsFile, OnlineAsrProfileField.HotwordsFile, "txt", "hotwords"));
            _detailContent.Add(b.BindFloat("Hotwords score", b.Profile.hotwordsScore, OnlineAsrProfileField.HotwordsScore));
            _detailContent.Add(b.BindFile("Rule FSTs", b.Profile.ruleFsts, OnlineAsrProfileField.RuleFsts, "fst"));
            _detailContent.Add(b.BindFile("Rule FARs", b.Profile.ruleFars, OnlineAsrProfileField.RuleFars, "far"));
            _detailContent.Add(b.BindFloat("Blank penalty", b.Profile.blankPenalty, OnlineAsrProfileField.BlankPenalty));
        }

        private void BuildEndpointSection(OnlineAsrProfile profile, OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("Endpoint Detection");
            var toggle = new Toggle("Enable endpoint") { value = profile.enableEndpoint };
            var handler = new EndpointToggleHandler(profile, _settings);
            toggle.RegisterValueChangedCallback(handler.Handle);
            _detailContent.Add(toggle);

            _detailContent.Add(b.BindFloat("Rule 1 min trailing silence", b.Profile.rule1MinTrailingSilence, OnlineAsrProfileField.Rule1MinTrailingSilence));
            _detailContent.Add(b.BindFloat("Rule 2 min trailing silence", b.Profile.rule2MinTrailingSilence, OnlineAsrProfileField.Rule2MinTrailingSilence));
            _detailContent.Add(b.BindFloat("Rule 3 min utterance length", b.Profile.rule3MinUtteranceLength, OnlineAsrProfileField.Rule3MinUtteranceLength));
        }

        private void BuildCtcFstDecoderSection(OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader("CtcFstDecoder");
            _detailContent.Add(b.BindFile("Graph", b.Profile.ctcFstDecoderGraph, OnlineAsrProfileField.CtcFstDecoderGraph, "fst"));
            _detailContent.Add(b.BindInt("Max active", b.Profile.ctcFstDecoderMaxActive, OnlineAsrProfileField.CtcFstDecoderMaxActive));
        }

        private void BuildModelFieldsSection(OnlineAsrProfile profile, OnlineAsrProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            switch (profile.modelType)
            {
                case OnlineAsrModelType.Transducer:   OnlineAsrProfileFieldBuilder.BuildTransducer(_detailContent, b); break;
                case OnlineAsrModelType.Paraformer:   OnlineAsrProfileFieldBuilder.BuildParaformer(_detailContent, b); break;
                case OnlineAsrModelType.Zipformer2Ctc: OnlineAsrProfileFieldBuilder.BuildZipformer2Ctc(_detailContent, b); break;
                case OnlineAsrModelType.NemoCtc:       OnlineAsrProfileFieldBuilder.BuildNemoCtc(_detailContent, b); break;
                case OnlineAsrModelType.ToneCtc:       OnlineAsrProfileFieldBuilder.BuildToneCtc(_detailContent, b); break;
            }
        }

        private sealed class EndpointToggleHandler
        {
            private readonly OnlineAsrProfile _p;
            private readonly AsrProjectSettings _s;

            internal EndpointToggleHandler(OnlineAsrProfile p, AsrProjectSettings s)
            {
                _p = p;
                _s = s;
            }

            internal void Handle(ChangeEvent<bool> evt)
            {
                _p.enableEndpoint = evt.newValue;
                _s.SaveSettings();
            }
        }
    }
}
