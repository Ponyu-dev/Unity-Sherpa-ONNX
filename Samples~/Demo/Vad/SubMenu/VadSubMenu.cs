using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// VAD module sub-menu — single demo card (VAD + ASR pipeline)
    /// plus a back button. Status label shows two lines: VAD engine
    /// readiness and the companion offline ASR readiness used by
    /// <see cref="VadAsrPipeline"/>.
    /// </summary>
    public sealed class VadSubMenu : IDemoView
    {
        private Button _btnDemo;
        private Button _backButton;
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
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnDemo != null)
                _btnDemo.clicked += HandleDemo;
            if (_backButton != null)
                _backButton.clicked += HandleBack;

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

            _btnDemo = null;
            _backButton = null;
            _infoLabel = null;
            _nav = null;
            _vadService = null;
            _asrService = null;
        }

        private void HandleDemo() => _nav?.NavigateTo(DemoNavigator.IdVadDemo);
        private void HandleBack() => _nav?.Back();

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;

            string vadLine = VadSampleStatusUtil.BuildVadLine(_vadService);
            string asrLine = VadSampleStatusUtil.BuildPipelineAsrLine(_asrService);
            _infoLabel.text = $"{vadLine}\n{asrLine}";
        }
    }
}
