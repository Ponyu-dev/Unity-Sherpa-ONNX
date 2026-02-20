using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root MonoBehaviour for the ASR samples scene.
    /// Owns <see cref="AsrService"/>, <see cref="OnlineAsrService"/>,
    /// <see cref="MicrophoneSource"/>, and switches between sample panels
    /// via <see cref="UIDocument"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AsrSampleNavigator : MonoBehaviour
    {
        [Header("Panel UXML Assets")]
        [SerializeField] private VisualTreeAsset _menuAsset;
        [SerializeField] private VisualTreeAsset _fileAsset;
        [SerializeField] private VisualTreeAsset _streamAsset;

        [Header("Audio")]
        [SerializeField] private AudioClip _sampleClip;

        private UIDocument _document;

        private AsrService _offlineService;
        private OnlineAsrService _onlineService;
        private MicrophoneSource _microphone;

        private readonly AsrSampleMenu _menu = new AsrSampleMenu();
        private readonly Dictionary<string, IAsrSamplePanel> _panels =
            new Dictionary<string, IAsrSamplePanel>();
        private IAsrSamplePanel _activePanel;

        private async void Awake()
        {
            _document = GetComponent<UIDocument>();

            _panels[AsrSampleMenu.IdFile] = new AsrFilePanel();
            _panels[AsrSampleMenu.IdStream] = new AsrStreamPanel();

            await InitializeServicesAsync();
        }

        private void OnEnable()
        {
            ShowMenu();
        }

        private void OnDestroy()
        {
            UnbindActive();

            _microphone?.Dispose();
            _microphone = null;

            _offlineService?.Dispose();
            _offlineService = null;

            _onlineService?.Dispose();
            _onlineService = null;
        }

        // ── Init ──

        private async UniTask InitializeServicesAsync()
        {
            _offlineService = new AsrService();
            _onlineService = new OnlineAsrService();
            var micSettings = await MicrophoneSettingsLoader
                .LoadAsync();
            _microphone = new MicrophoneSource(micSettings);

            try
            {
                await _offlineService.InitializeAsync();
                await _onlineService.InitializeAsync();

                bool ready = _offlineService.IsReady ||
                             _onlineService.IsReady;

                if (ready)
                {
                    SherpaOnnxLog.RuntimeLog(
                        "[SherpaOnnx] AsrSampleNavigator: " +
                        "services ready.");
                }
                else
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] AsrSampleNavigator: " +
                        "services initialized but engines not loaded.");
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AsrSampleNavigator init failed: " +
                    ex.Message);
            }
        }

        // ── Navigation ──

        private void ShowMenu()
        {
            UnbindActive();

            if (_menuAsset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] AsrSampleNavigator: " +
                    "_menuAsset is null.");
                return;
            }

            _document.visualTreeAsset = _menuAsset;

            _document.rootVisualElement.schedule.Execute(
                BindMenu);
        }

        private void BindMenu()
        {
            _menu.Bind(
                _document.rootVisualElement,
                _offlineService,
                _onlineService,
                Navigate);
        }

        private void Navigate(string panelId)
        {
            IAsrSamplePanel panel;
            if (!_panels.TryGetValue(panelId, out panel))
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] AsrSampleNavigator: " +
                    $"unknown panel '{panelId}'.");
                return;
            }

            VisualTreeAsset asset = GetAsset(panelId);
            if (asset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] AsrSampleNavigator: " +
                    $"no VisualTreeAsset for '{panelId}'.");
                return;
            }

            UnbindActive();
            _document.visualTreeAsset = asset;

            _document.rootVisualElement.schedule.Execute(
                BindActivePanel);

            void BindActivePanel()
            {
                panel.Bind(
                    _document.rootVisualElement,
                    _offlineService,
                    _onlineService,
                    _microphone,
                    _sampleClip,
                    ShowMenu);
                _activePanel = panel;
            }
        }

        private void UnbindActive()
        {
            _menu.Unbind();

            if (_activePanel != null)
            {
                _activePanel.Unbind();
                _activePanel = null;
            }
        }

        private VisualTreeAsset GetAsset(string panelId)
        {
            if (panelId == AsrSampleMenu.IdFile)
                return _fileAsset;
            if (panelId == AsrSampleMenu.IdStream)
                return _streamAsset;
            return null;
        }
    }
}
