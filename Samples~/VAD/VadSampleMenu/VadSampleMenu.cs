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
        private IVadService _vadService;
        private IAsrService _asrService;

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
            _vadService = vadService;
            _asrService = asrService;

            _btnDemo = root.Q<Button>("btnDemo");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnDemo != null)
                _btnDemo.clicked += HandleDemo;

            VadInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            VadInitProgressBus.Changed -= HandleInitProgressChanged;

            if (_btnDemo != null)
                _btnDemo.clicked -= HandleDemo;

            _btnDemo = null;
            _infoLabel = null;
            _onNavigate = null;
            _vadService = null;
            _asrService = null;
        }

        // ── Handlers ──

        private void HandleDemo()
        {
            _onNavigate?.Invoke(IdDemo);
        }

        // ── Helpers ──

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
