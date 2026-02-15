using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root MonoBehaviour for the samples scene.
    /// Owns one <see cref="TtsService"/>, one <see cref="AudioSource"/>,
    /// and switches between sample panels via <see cref="UIDocument"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SampleNavigator : MonoBehaviour
    {
        [Header("Panel UXML Assets")]
        [SerializeField] private VisualTreeAsset _menuAsset;
        [SerializeField] private VisualTreeAsset _simpleAsset;
        [SerializeField] private VisualTreeAsset _progressAsset;
        [SerializeField] private VisualTreeAsset _configAsset;

        private UIDocument _document;
        private AudioSource _audioSource;
        private TtsService _service;

        private readonly SampleMenu _menu = new();
        private readonly Dictionary<string, ISamplePanel> _panels = new();
        private ISamplePanel _activePanel;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _panels[SampleMenu.IdSimple] = new TtsSimplePanel();
            _panels[SampleMenu.IdProgress] = new TtsProgressPanel();
            _panels[SampleMenu.IdConfig] = new TtsConfigPanel();

            InitializeService();
        }

        private void OnEnable()
        {
            ShowMenu();
        }

        private void OnDestroy()
        {
            UnbindActive();
            _service?.Dispose();
            _service = null;
        }

        // ── Init ──

        private void InitializeService()
        {
            _service = new TtsService();

            try
            {
                _service.Initialize();

                if (_service.IsReady)
                {
                    SherpaOnnxLog.RuntimeLog(
                        "[SherpaOnnx] SampleNavigator: service ready.");
                }
                else
                {
                    SherpaOnnxLog.RuntimeWarning(
                        "[SherpaOnnx] SampleNavigator: service initialized " +
                        "but engine not loaded.");
                }
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] SampleNavigator init failed: {ex.Message}");
            }
        }

        // ── Navigation ──

        private void ShowMenu()
        {
            UnbindActive();

            if (_menuAsset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] SampleNavigator: _menuAsset is null.");
                return;
            }

            _document.visualTreeAsset = _menuAsset;

            // visualTreeAsset change triggers a rebuild on next frame;
            // schedule bind after layout.
            _document.rootVisualElement.schedule.Execute(() =>
            {
                _menu.Bind(
                    _document.rootVisualElement,
                    _service,
                    Navigate);
            });
        }

        private void Navigate(string panelId)
        {
            if (!_panels.TryGetValue(panelId, out var panel))
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] SampleNavigator: unknown panel '{panelId}'.");
                return;
            }

            var asset = GetAsset(panelId);
            if (asset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] SampleNavigator: " +
                    $"no VisualTreeAsset for '{panelId}'.");
                return;
            }

            UnbindActive();
            _document.visualTreeAsset = asset;

            _document.rootVisualElement.schedule.Execute(() =>
            {
                panel.Bind(
                    _document.rootVisualElement,
                    _service,
                    _audioSource,
                    ShowMenu);
                _activePanel = panel;
            });
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
            return panelId switch
            {
                SampleMenu.IdSimple => _simpleAsset,
                SampleMenu.IdProgress => _progressAsset,
                SampleMenu.IdConfig => _configAsset,
                _ => null,
            };
        }
    }
}
