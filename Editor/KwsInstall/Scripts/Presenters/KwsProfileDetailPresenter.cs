using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.KwsInstall.Settings;
using PonyuDev.SherpaOnnx.Kws.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.KwsInstall.Presenters
{
    internal sealed class KwsProfileDetailPresenter : ProfileDetailPresenterBase<KwsProfile, KwsProjectSettings>
    {
        internal KwsProfileDetailPresenter(VisualElement detailContent, KwsProjectSettings settings)
            : base(detailContent, settings) { }

        protected override IReadOnlyList<KwsProfile> Profiles
            => _settings.data.profiles;

        protected override Func<string, string> GetModelDirFunc
            => ModelPaths.GetKwsModelDir;

        protected override void AutoFill(KwsProfile profile, string modelDir)
            => KwsProfileAutoFiller.Fill(profile, modelDir);

        protected override async Task RedownloadCoreAsync(
            KwsProfile profile, ModelRedownloader redownloader)
        {
            string destDir = await redownloader.RedownloadArchiveAsync(
                profile.sourceUrl, ModelPaths.GetKwsModelDir, default);
            KwsProfileAutoFiller.Fill(profile, destDir);
        }

        protected override void SetModelType(KwsProfile profile, Enum value)
        {
            profile.modelType = (KwsModelType)value;
        }

        protected override bool HasInt8Alternative(KwsProfile profile, string modelDir)
            => KwsInt8Switcher.HasInt8Alternative(profile, modelDir);

        protected override bool IsUsingInt8(KwsProfile profile)
            => KwsInt8Switcher.IsUsingInt8(profile);

        protected override void ToggleInt8(KwsProfile profile, string modelDir)
        {
            bool wasInt8 = KwsInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                KwsInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                KwsInt8Switcher.SwitchToInt8(profile, modelDir);
        }

        protected override void BuildProfileSections(KwsProfile profile)
        {
            string modelDir = GetModelDirFunc(profile.ProfileName);
            var binder = new KwsProfileFieldBinder(profile, _settings, modelDir);

            BuildInt8SwitchButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(
                binder.BindText("Profile name", profile.profileName, KwsProfileField.ProfileName),
                profile.modelType,
                binder.BindText("Source URL", profile.sourceUrl, KwsProfileField.SourceUrl));
            BuildRuntimeSection(binder);
            BuildKeywordsSection(binder);
            BuildTransducerSection(profile, binder);
            BuildRemoteSection(profile,
                binder.BindText("Base URL", profile.remoteBaseUrl, KwsProfileField.RemoteBaseUrl));
            BuildLocalZipSection(profile);
        }

        // ── KWS-specific sections ──

        private void BuildRuntimeSection(KwsProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindFile("Tokens", b.Profile.tokens, KwsProfileField.Tokens,
                extension: "txt", isRequired: true));
            _detailContent.Add(b.BindInt("Sample rate", b.Profile.sampleRate, KwsProfileField.SampleRate));
            _detailContent.Add(b.BindInt("Feature dim", b.Profile.featureDim, KwsProfileField.FeatureDim));
            _detailContent.Add(b.BindInt("Threads", b.Profile.numThreads, KwsProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider", b.Profile.provider, KwsProfileField.Provider));
        }

        private void BuildKeywordsSection(KwsProfileFieldBinder b)
        {
            bool hasCustom = !string.IsNullOrEmpty(b.Profile.customKeywords);
            bool hasFile = !string.IsNullOrEmpty(b.Profile.keywordsFile);

            AddSectionHeader("Keywords");
            _detailContent.Add(b.BindFile("Keywords file", b.Profile.keywordsFile, KwsProfileField.KeywordsFile,
                extension: "txt", isRequired: !hasCustom));

            if (!hasFile && !hasCustom)
            {
                _detailContent.Add(new HelpBox(
                    "Provide either a keywords file or custom keywords below.",
                    HelpBoxMessageType.Warning));
            }

            _detailContent.Add(new HelpBox(
                "Custom keywords are merged with the keywords file at runtime via keywords_buf.\n"
                + "Each line: space-separated tokens + optional @TAG :boost #threshold.\n"
                + "Example: ▁HE LL O ▁WORLD @HELLO_WORLD :1.5 #0.3\n"
                + "Use sherpa-onnx-cli text2token to convert raw keywords to token format.",
                HelpBoxMessageType.Info));
            _detailContent.Add(b.BindMultilineText("Custom keywords", b.Profile.customKeywords, KwsProfileField.CustomKeywords));

            if (hasCustom)
            {
                List<string> warnings = CustomKeywordsValidator.Validate(b.Profile.customKeywords);
                foreach (string w in warnings)
                    _detailContent.Add(new HelpBox(w, HelpBoxMessageType.Warning));
            }

            _detailContent.Add(b.BindFloat("Keywords score", b.Profile.keywordsScore, KwsProfileField.KeywordsScore));
            _detailContent.Add(b.BindFloat("Keywords threshold", b.Profile.keywordsThreshold, KwsProfileField.KeywordsThreshold));
            _detailContent.Add(b.BindInt("Max active paths", b.Profile.maxActivePaths, KwsProfileField.MaxActivePaths));
            _detailContent.Add(b.BindInt("Num trailing blanks", b.Profile.numTrailingBlanks, KwsProfileField.NumTrailingBlanks));
        }

        private void BuildTransducerSection(KwsProfile profile, KwsProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            KwsProfileFieldBuilder.BuildTransducerFields(_detailContent, b);
        }
    }
}
