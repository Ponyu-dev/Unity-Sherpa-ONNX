using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root menu panel — lists available ASR samples.
    /// Calls <c>onNavigate</c> with a panel ID when the user taps a card.
    /// </summary>
    public sealed class AsrSampleMenu : IAsrSamplePanel
    {
        public const string IdFile = "file";
        public const string IdStream = "stream";
        public const string IdCombined = "combined";

        private Button _btnFile;
        private Button _btnStream;
        private Button _btnCombined;
        private Label _infoLabel;
        private Action<string> _onNavigate;
        private IAsrService _offlineService;
        private IOnlineAsrService _onlineService;

        // ── IAsrSamplePanel ──

        public void Bind(
            VisualElement root,
            IAsrService offlineService,
            IOnlineAsrService onlineService,
            MicrophoneSource microphone,
            AudioClip sampleClip,
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
            IAsrService offlineService,
            IOnlineAsrService onlineService,
            Action<string> onNavigate)
        {
            _onNavigate = onNavigate;
            _offlineService = offlineService;
            _onlineService = onlineService;

            _btnFile = root.Q<Button>("btnFile");
            _btnStream = root.Q<Button>("btnStream");
            _btnCombined = root.Q<Button>("btnCombined");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnFile != null)
                _btnFile.clicked += HandleFile;
            if (_btnStream != null)
                _btnStream.clicked += HandleStream;
            if (_btnCombined != null)
                _btnCombined.clicked += HandleCombined;

            AsrInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            AsrInitProgressBus.Changed -= HandleInitProgressChanged;

            if (_btnFile != null)
                _btnFile.clicked -= HandleFile;
            if (_btnStream != null)
                _btnStream.clicked -= HandleStream;
            if (_btnCombined != null)
                _btnCombined.clicked -= HandleCombined;

            _btnFile = null;
            _btnStream = null;
            _btnCombined = null;
            _infoLabel = null;
            _onNavigate = null;
            _offlineService = null;
            _onlineService = null;
        }

        // ── Handlers ──

        private void HandleFile()
        {
            _onNavigate?.Invoke(IdFile);
        }

        private void HandleStream()
        {
            _onNavigate?.Invoke(IdStream);
        }

        private void HandleCombined()
        {
            _onNavigate?.Invoke(IdCombined);
        }

        // ── Helpers ──

        private void HandleInitProgressChanged()
        {
            if (_infoLabel == null)
                return;

            string offlineLine = AsrSampleStatusUtil.BuildOfflineCurrent(_offlineService);
            string onlineLine = AsrSampleStatusUtil.BuildOnlineCurrent(_onlineService);
            _infoLabel.text = $"{offlineLine}\n{onlineLine}";
        }
    }
}
