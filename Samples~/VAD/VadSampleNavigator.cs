using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root MonoBehaviour for the VAD samples scene.
    /// Owns <see cref="VadService"/>, <see cref="AsrService"/>,
    /// <see cref="VadAsrPipeline"/>, <see cref="MicrophoneSource"/>,
    /// and switches between sample panels via <see cref="UIDocument"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class VadSampleNavigator : MonoBehaviour
    {
        [Header("Panel UXML Assets")]
        [SerializeField] private VisualTreeAsset _menuAsset;
        [SerializeField] private VisualTreeAsset _demoAsset;

        private UIDocument _document;

        private VadService _vadService;
        private AsrService _asrService;
        private VadAsrPipeline _pipeline;
        private MicrophoneSource _microphone;

        private readonly VadSampleMenu _menu = new VadSampleMenu();
        private readonly Dictionary<string, IVadSamplePanel> _panels =
            new Dictionary<string, IVadSamplePanel>();
        private IVadSamplePanel _activePanel;

        private async void Awake()
        {
            _document = GetComponent<UIDocument>();

            _panels[VadSampleMenu.IdDemo] = new VadDemoPanel();

            await InitializeServicesAsync();
        }

        private void OnEnable()
        {
            ShowMenu();
        }

        private void OnDestroy()
        {
            UnbindActive();

            _pipeline?.Dispose();
            _pipeline = null;

            _microphone?.Dispose();
            _microphone = null;

            _asrService?.Dispose();
            _asrService = null;

            _vadService?.Dispose();
            _vadService = null;
        }

        // ── Init ──

        private async UniTask InitializeServicesAsync()
        {
            _vadService = new VadService();
            _asrService = new AsrService();
            var micSettings = await MicrophoneSettingsLoader
                .LoadAsync();
            _microphone = new MicrophoneSource(micSettings);

            try
            {
                await _vadService.InitializeAsync();
                await _asrService.InitializeAsync();

                if (_vadService.IsReady)
                {
                    _pipeline = new VadAsrPipeline(
                        _vadService, _asrService);
                }

                bool ready = _vadService.IsReady &&
                             _asrService.IsReady;

                if (ready)
                {
                    SherpaOnnxLog.RuntimeLog(
                        "[SherpaOnnx] VadSampleNavigator: " +
                        "services ready.");
                }
                else
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] VadSampleNavigator: " +
                        "services initialized but engines " +
                        "not loaded.");
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] VadSampleNavigator init " +
                    "failed: " + ex.Message);
            }
        }

        // ── Navigation ──

        private void ShowMenu()
        {
            UnbindActive();

            if (_menuAsset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] VadSampleNavigator: " +
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
                _vadService,
                _asrService,
                Navigate);
        }

        private void Navigate(string panelId)
        {
            IVadSamplePanel panel;
            if (!_panels.TryGetValue(panelId, out panel))
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] VadSampleNavigator: " +
                    $"unknown panel '{panelId}'.");
                return;
            }

            VisualTreeAsset asset = GetAsset(panelId);
            if (asset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] VadSampleNavigator: " +
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
                    _vadService,
                    _asrService,
                    _pipeline,
                    _microphone,
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
            if (panelId == VadSampleMenu.IdDemo)
                return _demoAsset;
            return null;
        }
    }
}
