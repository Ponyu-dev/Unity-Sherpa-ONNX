using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root menu panel — lists available VAD samples.
    /// Calls <c>onNavigate</c> with a panel ID when the user taps a card.
    /// </summary>
    public sealed class VadSampleMenu : IVadSamplePanel
    {
        public const string IdDemo = "demo";

        private Button _btnDemo;
        private Label _infoLabel;
        private Action<string> _onNavigate;

        // ── IVadSamplePanel ──

        public void Bind(
            VisualElement root,
            IVadService vadService,
            IAsrService asrService,
            VadAsrPipeline pipeline,
            MicrophoneSource microphone,
            Action onBack)
        {
            // onBack is unused for the root menu.
        }

        /// <summary>
        /// Extended bind that receives a navigation callback
        /// instead of onBack.
        /// </summary>
        public void Bind(
            VisualElement root,
            IVadService vadService,
            IAsrService asrService,
            Action<string> onNavigate)
        {
            _onNavigate = onNavigate;

            _btnDemo = root.Q<Button>("btnDemo");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnDemo != null)
                _btnDemo.clicked += HandleDemo;

            UpdateInfo(vadService, asrService);
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

        private void UpdateInfo(
            IVadService vadService,
            IAsrService asrService)
        {
            if (_infoLabel == null)
                return;

            string vadInfo = BuildVadInfo(vadService);
            string asrInfo = BuildAsrInfo(asrService);

            _infoLabel.text = $"{vadInfo}\n{asrInfo}";
        }

        private static string BuildVadInfo(IVadService service)
        {
            if (service == null || !service.IsReady)
                return "VAD: not loaded";

            var profile = service.ActiveProfile;
            return $"VAD: {profile?.profileName ?? "\u2014"} | " +
                   $"{profile?.modelType}";
        }

        private static string BuildAsrInfo(IAsrService service)
        {
            if (service == null || !service.IsReady)
                return "ASR: not loaded";

            var profile = service.ActiveProfile;
            return $"ASR: {profile?.profileName ?? "\u2014"} | " +
                   $"{profile?.modelType}";
        }
    }
}
