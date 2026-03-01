using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Kws;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Initializes KWS + microphone, loads UXML panels and
    /// navigates between menu and demo panels.
    /// Attach to a GameObject with a <see cref="UIDocument"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class KwsSampleNavigator : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _menuAsset;
        [SerializeField] private VisualTreeAsset _demoAsset;

        private KwsService _kwsService;
        private MicrophoneSource _mic;
        private UIDocument _uiDocument;

        private KwsSampleMenu _menu;
        private KwsDemoPanel _demo;
        private IKwsSamplePanel _activePanel;

        private async void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            _kwsService = new KwsService();
            await _kwsService.InitializeAsync();

            if (!_kwsService.IsReady)
            {
                Debug.LogWarning("[SherpaOnnx] KwsSampleNavigator: KWS not ready.");
                return;
            }

            _kwsService.StartSession();

            _mic = new MicrophoneSource();
            _mic.SamplesAvailable += HandleSamples;

            _menu = new KwsSampleMenu();
            _demo = new KwsDemoPanel();

            ShowMenu();
        }

        private void OnDestroy()
        {
            _activePanel?.Unbind();
            _activePanel = null;

            if (_mic != null)
            {
                _mic.SamplesAvailable -= HandleSamples;
                _mic.Dispose();
                _mic = null;
            }

            _kwsService?.Dispose();
            _kwsService = null;
        }

        // ── Navigation ──

        private void ShowMenu()
        {
            _activePanel?.Unbind();

            var root = _menuAsset.CloneTree();
            _uiDocument.rootVisualElement.Clear();
            _uiDocument.rootVisualElement.Add(root);

            _menu.Bind(root, _kwsService, HandleNavigate);
            _activePanel = _menu;
        }

        private void ShowDemo()
        {
            _activePanel?.Unbind();

            var root = _demoAsset.CloneTree();
            _uiDocument.rootVisualElement.Clear();
            _uiDocument.rootVisualElement.Add(root);

            _demo.Bind(root, _kwsService, _mic, ShowMenu);
            _activePanel = _demo;
        }

        private void HandleNavigate(string panelId)
        {
            switch (panelId)
            {
                case KwsSampleMenu.IdDemo:
                    ShowDemo();
                    break;
            }
        }

        // ── Audio ──

        private void HandleSamples(float[] samples)
        {
            if (_kwsService == null)
                return;

            int sampleRate = _kwsService.ActiveProfile?.sampleRate ?? 16000;
            _kwsService.AcceptSamples(samples, sampleRate);
            _kwsService.ProcessAvailableFrames();
        }
    }
}
