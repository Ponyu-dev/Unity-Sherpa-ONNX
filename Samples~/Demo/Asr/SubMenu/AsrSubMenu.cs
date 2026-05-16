using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// ASR module sub-menu — three ASR demo cards (offline file,
    /// online streaming, combined) plus a back button. Hosts two
    /// profile pickers (offline + online ASR) so the user can switch
    /// either model without leaving the menu. Subscribes to
    /// <see cref="AsrInitProgressBus"/> so the status label keeps
    /// reflecting init progress for both streams.
    /// </summary>
    public sealed class AsrSubMenu : IDemoView
    {
        private Button _btnFile;
        private Button _btnStream;
        private Button _btnCombined;
        private Button _backButton;
        private DropdownField _offlinePicker;
        private DropdownField _onlinePicker;
        private Label _infoLabel;

        private IDemoNavigator _nav;
        private IAsrService _offlineService;
        private IOnlineAsrService _onlineService;

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _nav = nav;
            _offlineService = services?.OfflineAsr;
            _onlineService = services?.OnlineAsr;

            _btnFile = root.Q<Button>("btnFile");
            _btnStream = root.Q<Button>("btnStream");
            _btnCombined = root.Q<Button>("btnCombined");
            _backButton = root.Q<Button>("backButton");
            _offlinePicker = root.Q<DropdownField>("offlineProfilePicker");
            _onlinePicker = root.Q<DropdownField>("onlineProfilePicker");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnFile != null)
                _btnFile.clicked += HandleFile;
            if (_btnStream != null)
                _btnStream.clicked += HandleStream;
            if (_btnCombined != null)
                _btnCombined.clicked += HandleCombined;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            PopulateOfflinePicker();
            PopulateOnlinePicker();
            if (_offlinePicker != null)
                _offlinePicker.RegisterValueChangedCallback(HandleOfflinePicked);
            if (_onlinePicker != null)
                _onlinePicker.RegisterValueChangedCallback(HandleOnlinePicked);

            AsrInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            AsrInitProgressBus.Changed -= HandleInitProgressChanged;

            if (_btnFile != null)
                _btnFile.clicked -= HandleFile;
            if (_btnStream != null)
                _btnStream.clicked -= HandleStream;
            if (_btnCombined != null)
                _btnCombined.clicked -= HandleCombined;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;
            if (_offlinePicker != null)
                _offlinePicker.UnregisterValueChangedCallback(HandleOfflinePicked);
            if (_onlinePicker != null)
                _onlinePicker.UnregisterValueChangedCallback(HandleOnlinePicked);

            _btnFile = null;
            _btnStream = null;
            _btnCombined = null;
            _backButton = null;
            _offlinePicker = null;
            _onlinePicker = null;
            _infoLabel = null;
            _nav = null;
            _offlineService = null;
            _onlineService = null;
        }

        private void HandleFile() => _nav?.NavigateTo(DemoNavigator.IdAsrFile);
        private void HandleStream() => _nav?.NavigateTo(DemoNavigator.IdAsrStream);
        private void HandleCombined() => _nav?.NavigateTo(DemoNavigator.IdAsrCombined);
        private void HandleBack() => _nav?.Back();

        // Picker population filters by IsProfileAvailable so profiles
        // with no on-disk files (build-stripped or sweep-deleted) do
        // not appear as options the user could pick and immediately
        // fail on.
        private void PopulateOfflinePicker()
        {
            if (_offlinePicker == null || _offlineService?.Settings?.profiles == null)
                return;

            var names = new List<string>(_offlineService.Settings.profiles.Count);
            for (int i = 0; i < _offlineService.Settings.profiles.Count; i++)
            {
                var p = _offlineService.Settings.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.profileName))
                    continue;
                if (!_offlineService.IsProfileAvailable(p.profileName))
                    continue;
                names.Add(p.profileName);
            }
            _offlinePicker.choices = names;

            string active = _offlineService.ActiveProfile?.profileName;
            if (!string.IsNullOrEmpty(active) && names.Contains(active))
                _offlinePicker.SetValueWithoutNotify(active);
            else if (names.Count > 0)
                _offlinePicker.SetValueWithoutNotify(names[0]);
        }

        private void PopulateOnlinePicker()
        {
            if (_onlinePicker == null || _onlineService?.Settings?.profiles == null)
                return;

            var names = new List<string>(_onlineService.Settings.profiles.Count);
            for (int i = 0; i < _onlineService.Settings.profiles.Count; i++)
            {
                var p = _onlineService.Settings.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.profileName))
                    continue;
                if (!_onlineService.IsProfileAvailable(p.profileName))
                    continue;
                names.Add(p.profileName);
            }
            _onlinePicker.choices = names;

            string active = _onlineService.ActiveProfile?.profileName;
            if (!string.IsNullOrEmpty(active) && names.Contains(active))
                _onlinePicker.SetValueWithoutNotify(active);
            else if (names.Count > 0)
                _onlinePicker.SetValueWithoutNotify(names[0]);
        }

        // async void is the standard pattern for UI Toolkit value-
        // change callbacks. The native engine ctor inside SwitchProfile
        // is multi-second on Android — running it through the async
        // overload keeps the dropdown responsive while the bus emits
        // Init 0 → 100 → Ready and the status label re-renders.
        private async void HandleOfflinePicked(ChangeEvent<string> evt)
        {
            if (_offlineService == null || string.IsNullOrEmpty(evt.newValue))
                return;
            string current = _offlineService.ActiveProfile?.profileName;
            if (string.Equals(current, evt.newValue))
                return;
            await _offlineService.SwitchProfileAsync(evt.newValue);
        }

        private async void HandleOnlinePicked(ChangeEvent<string> evt)
        {
            if (_onlineService == null || string.IsNullOrEmpty(evt.newValue))
                return;
            string current = _onlineService.ActiveProfile?.profileName;
            if (string.Equals(current, evt.newValue))
                return;
            await _onlineService.SwitchProfileAsync(evt.newValue);
        }

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;

            string offlineLine = AsrSampleStatusUtil.BuildOfflineCurrent(_offlineService);
            string onlineLine = AsrSampleStatusUtil.BuildOnlineCurrent(_onlineService);
            _infoLabel.text = $"{offlineLine}\n{onlineLine}";

            // After each terminal Ready event the keep-only-active
            // sweep may have removed entries from disk — rebuild the
            // affected picker so unreachable profiles disappear from
            // the list. Each picker is rebuilt independently because
            // offline / online ASR have separate buses.
            if (AsrInitProgressBus.OfflineReady)
                PopulateOfflinePicker();
            if (AsrInitProgressBus.OnlineReady)
                PopulateOnlinePicker();
        }
    }
}
