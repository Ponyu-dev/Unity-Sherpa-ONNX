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

        private Button _btnFile;
        private Button _btnStream;
        private Label _infoLabel;
        private Action<string> _onNavigate;

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

            _btnFile = root.Q<Button>("btnFile");
            _btnStream = root.Q<Button>("btnStream");
            _infoLabel = root.Q<Label>("infoLabel");

            if (_btnFile != null)
                _btnFile.clicked += HandleFile;
            if (_btnStream != null)
                _btnStream.clicked += HandleStream;

            UpdateInfo(offlineService, onlineService);
        }

        public void Unbind()
        {
            if (_btnFile != null)
                _btnFile.clicked -= HandleFile;
            if (_btnStream != null)
                _btnStream.clicked -= HandleStream;

            _btnFile = null;
            _btnStream = null;
            _infoLabel = null;
            _onNavigate = null;
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

        // ── Helpers ──

        private void UpdateInfo(
            IAsrService offlineService,
            IOnlineAsrService onlineService)
        {
            if (_infoLabel == null)
                return;

            string offlineInfo = BuildProfileInfo("Offline", offlineService);
            string onlineInfo = BuildProfileInfo("Online", onlineService);

            _infoLabel.text = $"{offlineInfo}\n{onlineInfo}";
        }

        private static string BuildProfileInfo(
            string label,
            IAsrService service)
        {
            if (service == null || !service.IsReady)
                return $"{label}: not loaded";

            var profile = service.ActiveProfile;
            return $"{label}: {profile?.profileName ?? "—"} | " +
                   $"{profile?.modelType}";
        }

        private static string BuildProfileInfo(
            string label,
            IOnlineAsrService service)
        {
            if (service == null || !service.IsReady)
                return $"{label}: not loaded";

            var profile = service.ActiveProfile;
            return $"{label}: {profile?.profileName ?? "—"} | " +
                   $"{profile?.modelType}";
        }
    }
}
