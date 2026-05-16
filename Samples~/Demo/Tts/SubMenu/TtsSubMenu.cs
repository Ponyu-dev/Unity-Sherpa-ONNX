using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Tts;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// TTS module sub-menu — six TTS demo cards plus a back button.
    /// Hosts a profile picker dropdown listing every TTS profile;
    /// changing the selection calls <see cref="ITtsService.SwitchProfile(string)"/>
    /// so the user can flip between models without leaving the menu.
    /// Subscribes to <see cref="TtsInitProgressBus"/> so the status
    /// label keeps reflecting init progress while the user browses.
    /// </summary>
    public sealed class TtsSubMenu : IDemoView
    {
        private Button _btnSimple;
        private Button _btnProgress;
        private Button _btnConfig;
        private Button _btnCache;
        private Button _btnControls;
        private Button _btnStreaming;
        private Button _backButton;
        private DropdownField _profilePicker;
        private Label _infoLabel;

        private IDemoNavigator _nav;
        private ITtsService _service;

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _nav = nav;
            _service = services?.Tts;

            _btnSimple = root.Q<Button>("btnSimple");
            _btnProgress = root.Q<Button>("btnProgress");
            _btnConfig = root.Q<Button>("btnConfig");
            _btnCache = root.Q<Button>("btnCache");
            _btnControls = root.Q<Button>("btnControls");
            _btnStreaming = root.Q<Button>("btnStreaming");
            _backButton = root.Q<Button>("backButton");
            _profilePicker = root.Q<DropdownField>("profilePicker");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnSimple != null)
                _btnSimple.clicked += HandleSimple;
            if (_btnProgress != null)
                _btnProgress.clicked += HandleProgress;
            if (_btnConfig != null)
                _btnConfig.clicked += HandleConfig;
            if (_btnCache != null)
                _btnCache.clicked += HandleCache;
            if (_btnControls != null)
                _btnControls.clicked += HandleControls;
            if (_btnStreaming != null)
                _btnStreaming.clicked += HandleStreaming;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

            PopulateProfilePicker();
            if (_profilePicker != null)
                _profilePicker.RegisterValueChangedCallback(HandleProfilePicked);

            TtsInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            TtsInitProgressBus.Changed -= HandleInitProgressChanged;

            if (_btnSimple != null)
                _btnSimple.clicked -= HandleSimple;
            if (_btnProgress != null)
                _btnProgress.clicked -= HandleProgress;
            if (_btnConfig != null)
                _btnConfig.clicked -= HandleConfig;
            if (_btnCache != null)
                _btnCache.clicked -= HandleCache;
            if (_btnControls != null)
                _btnControls.clicked -= HandleControls;
            if (_btnStreaming != null)
                _btnStreaming.clicked -= HandleStreaming;
            if (_backButton != null)
                _backButton.clicked -= HandleBack;
            if (_profilePicker != null)
                _profilePicker.UnregisterValueChangedCallback(HandleProfilePicked);

            _btnSimple = null;
            _btnProgress = null;
            _btnConfig = null;
            _btnCache = null;
            _btnControls = null;
            _btnStreaming = null;
            _backButton = null;
            _profilePicker = null;
            _infoLabel = null;
            _nav = null;
            _service = null;
        }

        private void HandleSimple() => _nav?.NavigateTo(DemoNavigator.IdTtsSimple);
        private void HandleProgress() => _nav?.NavigateTo(DemoNavigator.IdTtsProgress);
        private void HandleConfig() => _nav?.NavigateTo(DemoNavigator.IdTtsConfig);
        private void HandleCache() => _nav?.NavigateTo(DemoNavigator.IdTtsCache);
        private void HandleControls() => _nav?.NavigateTo(DemoNavigator.IdTtsControls);
        private void HandleStreaming() => _nav?.NavigateTo(DemoNavigator.IdTtsStreaming);
        private void HandleBack() => _nav?.Back();

        // Initial population: only profiles whose model files are
        // actually reachable (active + Remote-with-URL + Local/LocalZip
        // with disk presence). Hides profiles that were stripped at
        // build time or swept by Keep-only-active so the user never
        // sees options that would fail on switch.
        // SetValueWithoutNotify avoids re-triggering HandleProfilePicked
        // during bind.
        private void PopulateProfilePicker()
        {
            if (_profilePicker == null || _service?.Settings?.profiles == null)
                return;

            var names = new List<string>(_service.Settings.profiles.Count);
            for (int i = 0; i < _service.Settings.profiles.Count; i++)
            {
                var p = _service.Settings.profiles[i];
                if (p == null || string.IsNullOrEmpty(p.profileName))
                    continue;
                if (!_service.IsProfileAvailable(p.profileName))
                    continue;
                names.Add(p.profileName);
            }
            _profilePicker.choices = names;

            string active = _service.ActiveProfile?.profileName;
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
            if (_service == null || string.IsNullOrEmpty(evt.newValue))
                return;
            string current = _service.ActiveProfile?.profileName;
            if (string.Equals(current, evt.newValue))
                return;
            await _service.SwitchProfileAsync(evt.newValue);
        }

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;
            _infoLabel.text = TtsSampleStatusUtil.BuildCurrent(_service);

            // After every successful switch the keep-only-active sweep
            // may have deleted other profiles' on-disk files — rebuild
            // the dropdown's choices so the user can not pick an entry
            // that would now fail. PopulateProfilePicker preserves the
            // active selection internally.
            if (TtsInitProgressBus.IsReady)
                PopulateProfilePicker();
        }
    }
}
