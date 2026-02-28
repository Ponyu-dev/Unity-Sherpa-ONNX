using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Import;
using PonyuDev.SherpaOnnx.Editor.TtsInstall.Settings;
using PonyuDev.SherpaOnnx.Tts.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.TtsInstall.Presenters
{
    internal sealed class TtsProfileDetailPresenter
        : ProfileDetailPresenterBase<TtsProfile, TtsProjectSettings>
    {
        internal TtsProfileDetailPresenter(
            VisualElement detailContent, TtsProjectSettings settings)
            : base(detailContent, settings) { }

        protected override IReadOnlyList<TtsProfile> Profiles
            => _settings.data.profiles;

        protected override Func<string, string> GetModelDirFunc
            => ModelPaths.GetTtsModelDir;

        protected override void AutoFill(TtsProfile profile, string modelDir)
            => TtsProfileAutoFiller.Fill(profile, modelDir);

        protected override async Task RedownloadCoreAsync(
            TtsProfile profile, ModelRedownloader redownloader)
        {
            string destDir = await redownloader.RedownloadArchiveAsync(
                profile.sourceUrl, ModelPaths.GetTtsModelDir, default);
            TtsProfileAutoFiller.Fill(profile, destDir);
        }

        protected override void SetModelType(TtsProfile profile, Enum value)
            => profile.modelType = (TtsModelType)value;

        protected override bool HasInt8Alternative(TtsProfile profile, string modelDir)
            => TtsInt8Switcher.HasInt8Alternative(profile, modelDir);

        protected override bool IsUsingInt8(TtsProfile profile)
            => TtsInt8Switcher.IsUsingInt8(profile);

        protected override void ToggleInt8(TtsProfile profile, string modelDir)
        {
            bool wasInt8 = TtsInt8Switcher.IsUsingInt8(profile);
            if (wasInt8)
                TtsInt8Switcher.SwitchToNormal(profile, modelDir);
            else
                TtsInt8Switcher.SwitchToInt8(profile, modelDir);
            profile.allowInt8 = !wasInt8;
        }

        protected override void BuildProfileSections(TtsProfile profile)
        {
            var binder = new ProfileFieldBinder(profile, _settings);

            BuildInt8SwitchButton(profile);
            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(
                binder.BindText("Profile name", profile.profileName, ProfileField.ProfileName),
                profile.modelType,
                binder.BindText("Source URL", profile.sourceUrl, ProfileField.SourceUrl));
            BuildCommonSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile,
                binder.BindText("Base URL", profile.remoteBaseUrl, ProfileField.RemoteBaseUrl));
            BuildLocalZipSection(profile);
        }

        // ── TTS-specific sections ──

        private void BuildCommonSection(ProfileFieldBinder b)
        {
            AddSectionHeader("Generation");
            _detailContent.Add(b.BindInt("Speaker ID", b.Profile.speakerId, ProfileField.SpeakerId));
            _detailContent.Add(b.BindFloat("Speed", b.Profile.speed, ProfileField.Speed));

            AddSectionHeader("Text Processing");
            _detailContent.Add(b.BindText("Rule FSTs", b.Profile.ruleFsts, ProfileField.RuleFsts));
            _detailContent.Add(b.BindText("Rule FARs", b.Profile.ruleFars, ProfileField.RuleFars));
            _detailContent.Add(b.BindInt("Max sentences", b.Profile.maxNumSentences, ProfileField.MaxNumSentences));
            _detailContent.Add(b.BindFloat("Silence scale", b.Profile.silenceScale, ProfileField.SilenceScale));

            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt("Threads", b.Profile.numThreads, ProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider", b.Profile.provider, ProfileField.Provider));
        }

        private void BuildModelFieldsSection(TtsProfile profile, ProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            switch (profile.modelType)
            {
                case TtsModelType.Vits:     ProfileFieldBuilder.BuildVits(_detailContent, b); break;
                case TtsModelType.Matcha:   ProfileFieldBuilder.BuildMatcha(_detailContent, b, _settings, HandleRefreshProfile); break;
                case TtsModelType.Kokoro:   ProfileFieldBuilder.BuildKokoro(_detailContent, b); break;
                case TtsModelType.Kitten:   ProfileFieldBuilder.BuildKitten(_detailContent, b); break;
                case TtsModelType.ZipVoice: ProfileFieldBuilder.BuildZipVoice(_detailContent, b); break;
                case TtsModelType.Pocket:   ProfileFieldBuilder.BuildPocket(_detailContent, b); break;
            }
        }

        private void HandleRefreshProfile() => ShowProfile(_currentIndex);
    }
}
