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

        private Button _btnSimple;
        private Button _btnProgress;
        private Button _btnConfig;
        private Label _infoLabel;
        private Action<string> _onNavigate;

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

            _btnSimple = root.Q<Button>("btnSimple");
            _btnProgress = root.Q<Button>("btnProgress");
            _btnConfig = root.Q<Button>("btnConfig");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnSimple != null)
                _btnSimple.clicked += HandleSimple;
            if (_btnProgress != null)
                _btnProgress.clicked += HandleProgress;
            if (_btnConfig != null)
                _btnConfig.clicked += HandleConfig;

            UpdateInfo(service);
        }

        public void Unbind()
        {
            if (_btnSimple != null)
                _btnSimple.clicked -= HandleSimple;
            if (_btnProgress != null)
                _btnProgress.clicked -= HandleProgress;
            if (_btnConfig != null)
                _btnConfig.clicked -= HandleConfig;

            _btnSimple = null;
            _btnProgress = null;
            _btnConfig = null;
            _infoLabel = null;
            _onNavigate = null;
        }

        private void HandleSimple() => _onNavigate?.Invoke(IdSimple);
        private void HandleProgress() => _onNavigate?.Invoke(IdProgress);
        private void HandleConfig() => _onNavigate?.Invoke(IdConfig);

        private void UpdateInfo(ITtsService service)
        {
            if (_infoLabel == null)
                return;

            if (service == null || !service.IsReady)
            {
                _infoLabel.text = "Engine not loaded.";
                return;
            }

            var profile = service.ActiveProfile;
            _infoLabel.text =
                $"Profile: {profile?.profileName ?? "—"} | " +
                $"Type: {profile?.modelType}";
        }
    }
}
