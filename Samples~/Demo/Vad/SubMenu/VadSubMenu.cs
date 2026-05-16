using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// VAD module sub-menu — single demo card (VAD + ASR pipeline)
    /// plus a back button. Hosts a profile picker for the VAD model
    /// (the pipeline-companion offline ASR model is configured in
    /// the ASR sub-menu). Status label shows two lines: VAD engine
    /// readiness and the companion offline ASR readiness used by
    /// <see cref="VadAsrPipeline"/>.
    /// </summary>
    public sealed class VadSubMenu : IDemoView
    {
        private Button _btnDemo;
        private Button _backButton;
        private DropdownField _profilePicker;
        private Label _infoLabel;

        private IDemoNavigator _nav;
        private IVadService _vadService;
        private IAsrService _asrService;

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _nav = nav;
            _vadService = services?.Vad;
            _asrService = services?.OfflineAsr;

            _btnDemo = root.Q<Button>("btnDemo");
            _backButton = root.Q<Button>("backButton");
            _profilePicker = root.Q<DropdownField>("profilePicker");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnDemo != null)
                _btnDemo.clicked += HandleDemo;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            PopulateProfilePicker();
            if (_profilePicker != null)
                _profilePicker.RegisterValueChangedCallback(HandleProfilePicked);

            VadInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            VadInitProgressBus.Changed -= HandleInitProgressChanged;

            if (_btnDemo != null)
                _btnDemo.clicked -= HandleDemo;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;
            if (_profilePicker != null)
                _profilePicker.UnregisterValueChangedCallback(HandleProfilePicked);

            _btnDemo = null;
            _backButton = null;
            _profilePicker = null;
            _infoLabel = null;
            _nav = null;
            _vadService = null;
            _asrService = null;
        }

        private void HandleDemo() => _nav?.NavigateTo(DemoNavigator.IdVadDemo);
        private void HandleBack() => _nav?.Back();

        private void PopulateProfilePicker()
        {
            if (_profilePicker == null || _vadService?.Settings?.profiles == null)
                return;

            var names = new List<string>(_vadService.Settings.profiles.Count);
            for (int i = 0; i < _vadService.Settings.profiles.Count; i++)
            {
                var p = _vadService.Settings.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.profileName))
                    continue;
                if (!_vadService.IsProfileAvailable(p.profileName))
                    continue;
                names.Add(p.profileName);
            }
            _profilePicker.choices = names;

            string active = _vadService.ActiveProfile?.profileName;
            if (!string.IsNullOrEmpty(active) && names.Contains(active))
                _profilePicker.SetValueWithoutNotify(active);
            else if (names.Count > 0)
                _profilePicker.SetValueWithoutNotify(names[0]);
        }

        // async void is the standard pattern for UI Toolkit value-
        // change callbacks. The native engine ctor inside SwitchProfile
        // is multi-second on Android — running it through the async
        // overload keeps the dropdown responsive while the bus emits
        // Init 0 → 100 → Ready and the status label re-renders.
        private async void HandleProfilePicked(ChangeEvent<string> evt)
        {
            if (_vadService == null || string.IsNullOrEmpty(evt.newValue))
                return;
            string current = _vadService.ActiveProfile?.profileName;
            if (string.Equals(current, evt.newValue))
                return;
            await _vadService.SwitchProfileAsync(evt.newValue);
        }

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;

            string vadLine = VadSampleStatusUtil.BuildVadLine(_vadService);
            string asrLine = VadSampleStatusUtil.BuildPipelineAsrLine(_asrService);
            _infoLabel.text = $"{vadLine}\n{asrLine}";

            // After each terminal Ready the keep-only-active sweep may
            // have removed VAD profiles' on-disk files — rebuild the
            // dropdown so unreachable profiles disappear.
            if (VadInitProgressBus.VadReady)
                PopulateProfilePicker();
        }
    }
}
