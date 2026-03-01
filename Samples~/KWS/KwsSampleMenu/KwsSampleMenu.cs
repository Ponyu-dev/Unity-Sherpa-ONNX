using System;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Kws;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root menu panel — lists available KWS samples.
    /// Calls <c>onNavigate</c> with a panel ID when the user taps a card.
    /// </summary>
    public sealed class KwsSampleMenu : IKwsSamplePanel
    {
        public const string IdDemo = "demo";

        private Button _btnDemo;
        private Label _infoLabel;
        private Action<string> _onNavigate;

        // ── IKwsSamplePanel ──

        public void Bind(VisualElement root, IKwsService kwsService, MicrophoneSource microphone, Action onBack)
        {
            // onBack is unused for the root menu.
        }

        /// <summary>
        /// Extended bind that receives a navigation callback.
        /// </summary>
        public void Bind(VisualElement root, IKwsService kwsService, Action<string> onNavigate)
        {
            _onNavigate = onNavigate;

            _btnDemo = root.Q<Button>("btnDemo");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnDemo != null)
                _btnDemo.clicked += HandleDemo;

            UpdateInfo(kwsService);
        }

        public void Unbind()
        {
            if (_btnDemo != null)
                _btnDemo.clicked -= HandleDemo;

            _btnDemo = null;
            _infoLabel = null;
            _onNavigate = null;
        }

        // ── Handlers ──

        private void HandleDemo()
        {
            _onNavigate?.Invoke(IdDemo);
        }

        // ── Helpers ──

        private void UpdateInfo(IKwsService service)
        {
            if (_infoLabel == null)
                return;

            if (service == null || !service.IsReady)
            {
                _infoLabel.text = "KWS: not loaded";
                return;
            }

            var profile = service.ActiveProfile;
            _infoLabel.text = $"KWS: {profile?.profileName ?? "\u2014"} | {profile?.modelType}";
        }
    }
}
