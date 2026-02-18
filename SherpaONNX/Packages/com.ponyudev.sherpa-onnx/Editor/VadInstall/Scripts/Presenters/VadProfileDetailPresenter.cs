using System;
using PonyuDev.SherpaOnnx.Editor.Common;
using PonyuDev.SherpaOnnx.Editor.Common.Presenters;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Import;
using PonyuDev.SherpaOnnx.Editor.VadInstall.Settings;
using PonyuDev.SherpaOnnx.Vad.Data;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Editor.VadInstall.Presenters
{
    /// <summary>
    /// Renders full VAD profile editing UI.
    /// Identity, thresholds, runtime, and model fields are always shown;
    /// model-specific section rebuilds when model type changes.
    /// </summary>
    internal sealed class VadProfileDetailPresenter : IDisposable
    {
        private readonly VadProjectSettings _settings;
        private readonly VisualElement _detailContent;

        private ProfileListPresenter<VadProfile> _listPresenter;
        private int _currentIndex = -1;

        internal VadProfileDetailPresenter(
            VisualElement detailContent,
            VadProjectSettings settings)
        {
            _detailContent = detailContent;
            _settings = settings;
        }

        internal void SetListPresenter(
            ProfileListPresenter<VadProfile> listPresenter)
        {
            _listPresenter = listPresenter;
        }

        internal void ShowProfile(int index)
        {
            _currentIndex = index;
            _detailContent.Clear();

            if (index < 0 || index >= _settings.data.profiles.Count)
                return;

            VadProfile profile = _settings.data.profiles[index];
            var binder = new VadProfileFieldBinder(profile, _settings);

            BuildAutoConfigureButton(profile);
            BuildIdentitySection(profile, binder);
            BuildThresholdsSection(binder);
            BuildRuntimeSection(binder);
            BuildModelFieldsSection(profile, binder);
        }

        internal void Clear()
        {
            _currentIndex = -1;
            _detailContent.Clear();
        }

        public void Dispose()
        {
            Clear();
        }

        // ── Sections ──

        private void BuildAutoConfigureButton(VadProfile profile)
        {
            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir))
                return;

            var button = new Button { text = "Auto-configure paths" };
            button.AddToClassList("btn");
            button.AddToClassList("btn-primary");
            button.AddToClassList("model-btn-spaced");
            button.clicked += HandleAutoConfigureClicked;
            _detailContent.Add(button);
        }

        private void BuildIdentitySection(
            VadProfile profile, VadProfileFieldBinder binder)
        {
            var nameField = binder.BindText(
                "Profile name", profile.profileName,
                VadProfileField.ProfileName);
            nameField.RegisterCallback<FocusOutEvent>(HandleNameFocusOut);
            _detailContent.Add(nameField);

            var typeField = new EnumField("Model type", profile.modelType);
            typeField.RegisterValueChangedCallback(HandleModelTypeChanged);
            _detailContent.Add(typeField);
        }

        private void BuildThresholdsSection(VadProfileFieldBinder b)
        {
            AddSectionHeader("Thresholds");
            _detailContent.Add(b.BindFloat(
                "Threshold", b.Profile.threshold,
                VadProfileField.Threshold));
            _detailContent.Add(b.BindFloat(
                "Min silence duration", b.Profile.minSilenceDuration,
                VadProfileField.MinSilenceDuration));
            _detailContent.Add(b.BindFloat(
                "Min speech duration", b.Profile.minSpeechDuration,
                VadProfileField.MinSpeechDuration));
            _detailContent.Add(b.BindFloat(
                "Max speech duration", b.Profile.maxSpeechDuration,
                VadProfileField.MaxSpeechDuration));
        }

        private void BuildRuntimeSection(VadProfileFieldBinder b)
        {
            AddSectionHeader("Runtime");
            _detailContent.Add(b.BindInt(
                "Sample rate", b.Profile.sampleRate,
                VadProfileField.SampleRate));
            _detailContent.Add(b.BindInt(
                "Window size", b.Profile.windowSize,
                VadProfileField.WindowSize));
            _detailContent.Add(b.BindInt(
                "Threads", b.Profile.numThreads,
                VadProfileField.NumThreads));
            _detailContent.Add(b.BindText(
                "Provider", b.Profile.provider,
                VadProfileField.Provider));
            _detailContent.Add(b.BindFloat(
                "Buffer size (seconds)", b.Profile.bufferSizeInSeconds,
                VadProfileField.BufferSizeInSeconds));
        }

        private void BuildModelFieldsSection(
            VadProfile profile, VadProfileFieldBinder b)
        {
            AddSectionHeader(profile.modelType + " Settings");
            VadProfileFieldBuilder.BuildModelFields(_detailContent, b);
        }

        private void AddSectionHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("model-section-header");
            _detailContent.Add(header);
        }

        // ── Handlers ──

        private void HandleAutoConfigureClicked()
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;

            string modelDir = VadModelPaths.GetModelDir(profile.profileName);
            if (!ModelFileService.ModelDirExists(modelDir)) return;

            VadProfileAutoFiller.Fill(profile, modelDir);
            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        private void HandleNameFocusOut(FocusOutEvent evt)
        {
            _listPresenter?.RefreshList();
        }

        private void HandleModelTypeChanged(ChangeEvent<Enum> evt)
        {
            if (!TryGetCurrentProfile(out VadProfile profile)) return;

            profile.modelType = (VadModelType)evt.newValue;

            AdjustWindowSizeForModelType(profile);

            _settings.SaveSettings();
            ShowProfile(_currentIndex);
        }

        // ── Helpers ──

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

        private bool TryGetCurrentProfile(out VadProfile profile)
        {
            profile = null;
            if (_currentIndex < 0 || _currentIndex >= _settings.data.profiles.Count)
                return false;

            profile = _settings.data.profiles[_currentIndex];
            return true;
        }
    }
}
