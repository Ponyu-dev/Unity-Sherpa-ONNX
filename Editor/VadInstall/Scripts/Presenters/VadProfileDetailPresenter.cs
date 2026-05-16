using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Editor.Common.Import;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Import;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    internal sealed class VadProfileDetailPresenter : ProfileDetailPresenterBase<VadProfile, VadProjectSettings>
    {
        internal VadProfileDetailPresenter(VisualElement detailContent, VadProjectSettings settings)
            : base(detailContent, settings) { }

        protected override IReadOnlyList<VadProfile> Profiles
            => _settings.data.profiles;

        protected override Func<string, string> GetModelDirFunc
            => ModelPaths.GetVadModelDir;

        protected override void AutoFill(VadProfile profile, string modelDir)
            => VadProfileAutoFiller.Fill(profile, modelDir);

        protected override async Task RedownloadCoreAsync(
            VadProfile profile, ModelRedownloader redownloader)
        {
            string modelDir = ModelPaths.GetVadModelDir(profile.profileName);
            await redownloader.RedownloadFileAsync(
                profile.sourceUrl, modelDir, default);
            VadProfileAutoFiller.Fill(profile, modelDir);
        }

        protected override void SetModelType(VadProfile profile, Enum value)
        {
            profile.modelType = (VadModelType)value;
            AdjustWindowSizeForModelType(profile);
        }

        protected override void BuildProfileSections(VadProfile profile)
        {
            string modelDir = GetModelDirFunc(profile.ProfileName);
            var binder = new VadProfileFieldBinder(profile, _settings, modelDir);

            BuildVersionWarning(profile.modelType);
            BuildIdentitySection(
                binder.BindText("Profile name", profile.profileName, VadProfileField.ProfileName),
                profile.modelType,
                binder.BindText("Source URL", profile.sourceUrl, VadProfileField.SourceUrl));
            BuildThresholdsSection(binder);
            BuildRuntimeSection(binder);
            BuildModelFieldsSection(profile, binder);
            BuildRemoteSection(profile,
                binder.BindText("Base URL", profile.remoteBaseUrl, VadProfileField.RemoteBaseUrl));
            BuildLocalZipSection(profile);
        }

        // ── VAD-specific sections ──

        private void BuildThresholdsSection(VadProfileFieldBinder b)
        {
            AddSectionHeader("Thresholds");
            _detailContent.Add(b.BindFloat("Threshold", b.Profile.threshold, VadProfileField.Threshold));
            _detailContent.Add(b.BindFloat("Min silence duration", b.Profile.minSilenceDuration, VadProfileField.MinSilenceDuration));
            _detailContent.Add(b.BindFloat("Min speech duration", b.Profile.minSpeechDuration, VadProfileField.MinSpeechDuration));
            _detailContent.Add(b.BindFloat("Max speech duration", b.Profile.maxSpeechDuration, VadProfileField.MaxSpeechDuration));
        }

        private void BuildRuntimeSection(VadProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt("Sample rate", b.Profile.sampleRate, VadProfileField.SampleRate));
            _detailContent.Add(b.BindInt("Window size", b.Profile.windowSize, VadProfileField.WindowSize));
            _detailContent.Add(b.BindInt("Threads", b.Profile.numThreads, VadProfileField.NumThreads));
            _detailContent.Add(b.BindText("Provider", b.Profile.provider, VadProfileField.Provider));
            _detailContent.Add(b.BindFloat("Buffer size (seconds)", b.Profile.bufferSizeInSeconds, VadProfileField.BufferSizeInSeconds));
        }

        private void BuildModelFieldsSection(VadProfile profile, VadProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            VadProfileFieldBuilder.BuildModelFields(_detailContent, b);
        }

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
    }
}
