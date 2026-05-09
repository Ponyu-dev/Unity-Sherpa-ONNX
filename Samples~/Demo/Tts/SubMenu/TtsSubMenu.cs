using PonyuDev.SherpaOnnx.Tts;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// TTS module sub-menu — six TTS demo cards plus a back button.
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

            _btnSimple = null;
            _btnProgress = null;
            _btnConfig = null;
            _btnCache = null;
            _btnControls = null;
            _btnStreaming = null;
            _backButton = null;
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

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;
            _infoLabel.text = TtsSampleStatusUtil.BuildCurrent(_service);
        }
    }
}
