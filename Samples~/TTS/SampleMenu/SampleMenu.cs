using System;
using PonyuDev.SherpaOnnx.Tts;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root menu panel — lists available TTS samples.
    /// Calls <c>onNavigate</c> with a panel ID when the user taps a card.
    /// </summary>
    public sealed class SampleMenu : ISamplePanel
    {
        public const string IdSimple = "simple";
        public const string IdProgress = "progress";
        public const string IdConfig = "config";
        public const string IdCache = "cache";
        public const string IdControls = "controls";
        public const string IdStreaming = "streaming";

        private Button _btnSimple;
        private Button _btnProgress;
        private Button _btnConfig;
        private Button _btnCache;
        private Button _btnControls;
        private Button _btnStreaming;
        private Label _infoLabel;
        private Action<string> _onNavigate;
        private ITtsService _service;

        public void Bind(
            VisualElement root,
            ITtsService service,
            AudioSource audio,
            Action onBack)
        {
            // onBack is unused for the root menu.
        }

        /// <summary>
        /// Extended bind that receives a navigation callback instead of onBack.
        /// </summary>
        public void Bind(
            VisualElement root,
            ITtsService service,
            Action<string> onNavigate)
        {
            _onNavigate = onNavigate;
            _service = service;

            _btnSimple = root.Q<Button>("btnSimple");
            _btnProgress = root.Q<Button>("btnProgress");
            _btnConfig = root.Q<Button>("btnConfig");
            _btnCache = root.Q<Button>("btnCache");
            _btnControls = root.Q<Button>("btnControls");
            _btnStreaming = root.Q<Button>("btnStreaming");
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

            _btnSimple = null;
            _btnProgress = null;
            _btnConfig = null;
            _btnCache = null;
            _btnControls = null;
            _btnStreaming = null;
            _infoLabel = null;
            _onNavigate = null;
            _service = null;
        }

        private void HandleSimple() => _onNavigate?.Invoke(IdSimple);
        private void HandleProgress() => _onNavigate?.Invoke(IdProgress);
        private void HandleConfig() => _onNavigate?.Invoke(IdConfig);
        private void HandleCache() => _onNavigate?.Invoke(IdCache);
        private void HandleControls() => _onNavigate?.Invoke(IdControls);
        private void HandleStreaming() => _onNavigate?.Invoke(IdStreaming);

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;
            _infoLabel.text = TtsSampleStatusUtil.BuildCurrent(_service);
        }
    }
}
