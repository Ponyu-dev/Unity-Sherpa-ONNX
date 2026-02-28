using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline.Data;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Import;
using PonyuDev.SherpaOnnx.Editor.AsrInstall.Settings;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.AsrInstall.Presenters.Offline
{
    internal sealed class AsrProfileDetailPresenter : ProfileDetailPresenterBase<AsrProfile, AsrProjectSettings>
    {
        internal AsrProfileDetailPresenter(VisualElement detailContent, AsrProjectSettings settings)
            : base(detailContent, settings) { }

        protected override IReadOnlyList<AsrProfile> Profiles
            => _settings.offlineData.profiles;

        protected override Func<string, string> GetModelDirFunc
            => ModelPaths.GetAsrModelDir;

        protected override void AutoFill(AsrProfile profile, string modelDir)
            => AsrProfileAutoFiller.Fill(profile, modelDir);

        protected override async Task RedownloadCoreAsync(
            AsrProfile profile, ModelRedownloader redownloader)
        {
            string destDir = await redownloader.RedownloadArchiveAsync(
                profile.sourceUrl, ModelPaths.GetAsrModelDir, default);
            AsrProfileAutoFiller.Fill(profile, destDir);
        }

        protected override void SetModelType(AsrProfile profile, Enum value)
            => profile.modelType = (AsrModelType)value;

        protected override bool HasInt8Alternative(AsrProfile profile, string modelDir)
            => AsrInt8Switcher.HasInt8Alternative(profile, modelDir);

        protected override bool IsUsingInt8(AsrProfile profile)
            => AsrInt8Switcher.IsUsingInt8(profile);

        protected override void ToggleInt8(AsrProfile profile, string modelDir)
        {
            bool wasInt8 = AsrInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                AsrInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                AsrInt8Switcher.SwitchToInt8(profile, modelDir);
            profile.allowInt8 = !wasInt8;
        }

        protected override void BuildProfileSections(AsrProfile profile)
        {
            var binder = new AsrProfileFieldBinder(profile, _settings);

            BuildInt8SwitchButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(
                binder.BindText("Profile name", profile.profileName, AsrProfileField.ProfileName),
                profile.modelType,
                binder.BindText("Source URL", profile.sourceUrl, AsrProfileField.SourceUrl));
            BuildCommonSection(binder);
            BuildFeatureSection(binder);
            BuildRecognizerSection(binder);
            BuildLmSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile,
                binder.BindText("Base URL", profile.remoteBaseUrl, AsrProfileField.RemoteBaseUrl));
            BuildLocalZipSection(profile);
            BuildPoolSizeSection();
        }

        // ── ASR-specific sections ──

        private void BuildCommonSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt("Threads", b.Profile.numThreads, AsrProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider", b.Profile.provider, AsrProfileField.Provider));
            _detailContent.Add(b.BindText("Tokens", b.Profile.tokens, AsrProfileField.Tokens));
        }

        private void BuildFeatureSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Feature");
            _detailContent.Add(b.BindInt("Sample rate", b.Profile.sampleRate, AsrProfileField.SampleRate));
            _detailContent.Add(b.BindInt("Feature dim", b.Profile.featureDim, AsrProfileField.FeatureDim));
        }

        private void BuildRecognizerSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Recognizer");
            _detailContent.Add(b.BindText("Decoding method", b.Profile.decodingMethod, AsrProfileField.DecodingMethod));
            _detailContent.Add(b.BindInt("Max active paths", b.Profile.maxActivePaths, AsrProfileField.MaxActivePaths));
            _detailContent.Add(b.BindText("Hotwords file", b.Profile.hotwordsFile, AsrProfileField.HotwordsFile));
            _detailContent.Add(b.BindFloat("Hotwords score", b.Profile.hotwordsScore, AsrProfileField.HotwordsScore));
            _detailContent.Add(b.BindText("Rule FSTs", b.Profile.ruleFsts, AsrProfileField.RuleFsts));
            _detailContent.Add(b.BindText("Rule FARs", b.Profile.ruleFars, AsrProfileField.RuleFars));
            _detailContent.Add(b.BindFloat("Blank penalty", b.Profile.blankPenalty, AsrProfileField.BlankPenalty));
        }

        private void BuildLmSection(AsrProfileFieldBinder b)
        {
            AddSectionHeader("Language Model");
            _detailContent.Add(b.BindText("LM model", b.Profile.lmModel, AsrProfileField.LmModel));
            _detailContent.Add(b.BindFloat("LM scale", b.Profile.lmScale, AsrProfileField.LmScale));
        }

        private void BuildModelFieldsSection(AsrProfile profile, AsrProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            switch (profile.modelType)
            {
                case AsrModelType.Transducer:   AsrProfileFieldBuilder.BuildTransducer(_detailContent, b); break;
                case AsrModelType.Paraformer:   AsrProfileFieldBuilder.BuildParaformer(_detailContent, b); break;
                case AsrModelType.Whisper:      AsrProfileFieldBuilder.BuildWhisper(_detailContent, b); break;
                case AsrModelType.SenseVoice:   AsrProfileFieldBuilder.BuildSenseVoice(_detailContent, b); break;
                case AsrModelType.Moonshine:    AsrProfileFieldBuilder.BuildMoonshine(_detailContent, b); break;
                case AsrModelType.NemoCtc:      AsrProfileFieldBuilder.BuildNemoCtc(_detailContent, b); break;
                case AsrModelType.ZipformerCtc: AsrProfileFieldBuilder.BuildZipformerCtc(_detailContent, b); break;
                case AsrModelType.Tdnn:         AsrProfileFieldBuilder.BuildTdnn(_detailContent, b); break;
                case AsrModelType.FireRedAsr:   AsrProfileFieldBuilder.BuildFireRedAsr(_detailContent, b); break;
                case AsrModelType.Dolphin:      AsrProfileFieldBuilder.BuildDolphin(_detailContent, b); break;
                case AsrModelType.Canary:       AsrProfileFieldBuilder.BuildCanary(_detailContent, b); break;
                case AsrModelType.WenetCtc:     AsrProfileFieldBuilder.BuildWenetCtc(_detailContent, b); break;
                case AsrModelType.Omnilingual:  AsrProfileFieldBuilder.BuildOmnilingual(_detailContent, b); break;
                case AsrModelType.MedAsr:       AsrProfileFieldBuilder.BuildMedAsr(_detailContent, b); break;
                case AsrModelType.FunAsrNano:   AsrProfileFieldBuilder.BuildFunAsrNano(_detailContent, b); break;
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

        private sealed class PoolSizeHandler
        {
            private readonly AsrProjectSettings _s;
            internal PoolSizeHandler(AsrProjectSettings s) => _s = s;

            internal void Handle(ChangeEvent<int> evt)
            {
                int val = Math.Max(1, evt.newValue);
                _s.offlineData.offlineRecognizerPoolSize = val;
                _s.SaveSettings();
            }
        }
    }
}
