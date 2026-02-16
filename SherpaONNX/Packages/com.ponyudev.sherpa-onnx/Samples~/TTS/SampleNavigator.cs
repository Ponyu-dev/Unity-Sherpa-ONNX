using System;
using System.Collections.Generic;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root MonoBehaviour for the samples scene.
    /// Owns one <see cref="CachedTtsService"/> (wrapping <see cref="TtsService"/>),
    /// one <see cref="AudioSource"/>, and switches between sample panels
    /// via <see cref="UIDocument"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SampleNavigator : MonoBehaviour
    {
        [Header("Panel UXML Assets")]
        [SerializeField] private VisualTreeAsset _menuAsset;
        [SerializeField] private VisualTreeAsset _simpleAsset;
        [SerializeField] private VisualTreeAsset _progressAsset;
        [SerializeField] private VisualTreeAsset _configAsset;
        [SerializeField] private VisualTreeAsset _cacheAsset;

        private UIDocument _document;
        private AudioSource _audioSource;

        private TtsService _innerService;
        private CachedTtsService _cachedService;

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
            _panels[SampleMenu.IdCache] = new TtsCachePanel();

            InitializeService();
        }

        private void OnEnable()
        {
            ShowMenu();
        }

        private void OnDestroy()
        {
            UnbindActive();

            if (_cachedService != null)
            {
                _cachedService.Dispose();
                _cachedService = null;
                _innerService = null;
            }
            else
            {
                _innerService?.Dispose();
                _innerService = null;
            }
        }

        // ── Init ──

        private void InitializeService()
        {
            _innerService = new TtsService();

            try
            {
                _innerService.Initialize();

                // Wrap in CachedTtsService for cache support.
                var cache = _innerService.Settings?.cache;
                if (cache != null)
                {
                    _cachedService = new CachedTtsService(
                        _innerService, cache, transform);
                }

                if (Service.IsReady)
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

        /// <summary>Active service (cached decorator or raw).</summary>
        private ITtsService Service =>
            (ITtsService)_cachedService ?? _innerService;

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

            _document.rootVisualElement.schedule.Execute(() =>
            {
                _menu.Bind(
                    _document.rootVisualElement,
                    Service,
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
                    Service,
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
                SampleMenu.IdCache => _cacheAsset,
                _ => null,
            };
        }
    }
}
