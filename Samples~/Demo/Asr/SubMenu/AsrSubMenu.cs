using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// ASR module sub-menu — three ASR demo cards (offline file,
    /// online streaming, combined) plus a back button. Subscribes to
    /// <see cref="AsrInitProgressBus"/> so the status label keeps
    /// reflecting init progress for both offline and online streams.
    /// </summary>
    public sealed class AsrSubMenu : IDemoView
    {
        private Button _btnFile;
        private Button _btnStream;
        private Button _btnCombined;
        private Button _backButton;
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
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnFile != null)
                _btnFile.clicked += HandleFile;
            if (_btnStream != null)
                _btnStream.clicked += HandleStream;
            if (_btnCombined != null)
                _btnCombined.clicked += HandleCombined;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

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

            _btnFile = null;
            _btnStream = null;
            _btnCombined = null;
            _backButton = null;
            _infoLabel = null;
            _nav = null;
            _offlineService = null;
            _onlineService = null;
        }

        private void HandleFile() => _nav?.NavigateTo(DemoNavigator.IdAsrFile);
        private void HandleStream() => _nav?.NavigateTo(DemoNavigator.IdAsrStream);
        private void HandleCombined() => _nav?.NavigateTo(DemoNavigator.IdAsrCombined);
        private void HandleBack() => _nav?.Back();

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;

            string offlineLine = AsrSampleStatusUtil.BuildOfflineCurrent(_offlineService);
            string onlineLine = AsrSampleStatusUtil.BuildOnlineCurrent(_onlineService);
            _infoLabel.text = $"{offlineLine}\n{onlineLine}";
        }
    }
}
