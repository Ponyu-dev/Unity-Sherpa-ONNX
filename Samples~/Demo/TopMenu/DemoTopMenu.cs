using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Root view of the unified demo. Three large cards (TTS / ASR /
    /// VAD); each card carries its own per-service status line that
    /// reflects the latest <c>ProfileReadyEvent</c> emitted on the
    /// corresponding init-progress bus.
    ///
    /// Status text is delegated to the existing per-module status
    /// utilities so wording stays in sync with each module's
    /// sub-menu / panels. The view re-reads the buses on every
    /// <c>Changed</c> event and lazily on <see cref="Bind"/> so the
    /// labels are correct even if the user navigates away and comes
    /// back after init finished.
    /// </summary>
    public sealed class DemoTopMenu : IDemoView
    {
        private Button _cardTts;
        private Button _cardAsr;
        private Button _cardVad;
        private Label _ttsStatus;
        private Label _asrStatus;
        private Label _vadStatus;

        private DemoServices _services;
        private IDemoNavigator _nav;

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _services = services;
            _nav = nav;

            _cardTts = root.Q<Button>("cardTts");
            _cardAsr = root.Q<Button>("cardAsr");
            _cardVad = root.Q<Button>("cardVad");
            _ttsStatus = root.Q<Label>("ttsStatus");
            _asrStatus = root.Q<Label>("asrStatus");
            _vadStatus = root.Q<Label>("vadStatus");

            if (_cardTts != null)
                _cardTts.clicked += HandleTtsClicked;
            if (_cardAsr != null)
                _cardAsr.clicked += HandleAsrClicked;
            if (_cardVad != null)
                _cardVad.clicked += HandleVadClicked;

            TtsInitProgressBus.Changed += HandleTtsChanged;
            AsrInitProgressBus.Changed += HandleAsrChanged;
            VadInitProgressBus.Changed += HandleVadChanged;

            // Render the current state immediately — the buses may
            // have published events before this view bound.
            HandleTtsChanged();
            HandleAsrChanged();
            HandleVadChanged();
        }

        public void Unbind()
        {
            TtsInitProgressBus.Changed -= HandleTtsChanged;
            AsrInitProgressBus.Changed -= HandleAsrChanged;
            VadInitProgressBus.Changed -= HandleVadChanged;

            if (_cardTts != null)
                _cardTts.clicked -= HandleTtsClicked;
            if (_cardAsr != null)
                _cardAsr.clicked -= HandleAsrClicked;
            if (_cardVad != null)
                _cardVad.clicked -= HandleVadClicked;

            _cardTts = null;
            _cardAsr = null;
            _cardVad = null;
            _ttsStatus = null;
            _asrStatus = null;
            _vadStatus = null;

            _services = null;
            _nav = null;
        }

        private void HandleTtsClicked() => _nav?.NavigateTo(DemoNavigator.IdTts);
        private void HandleAsrClicked() => _nav?.NavigateTo(DemoNavigator.IdAsr);
        private void HandleVadClicked() => _nav?.NavigateTo(DemoNavigator.IdVad);

        private void HandleTtsChanged()
        {
            if (_ttsStatus == null)
                return;
            _ttsStatus.text = TtsSampleStatusUtil.BuildCurrent(_services?.Tts);
            ApplyFailedClass(_ttsStatus, TtsInitProgressBus.IsFailed);
        }

        private void HandleAsrChanged()
        {
            if (_asrStatus == null)
                return;
            string offline = AsrSampleStatusUtil.BuildOfflineCurrent(_services?.OfflineAsr);
            string online = AsrSampleStatusUtil.BuildOnlineCurrent(_services?.OnlineAsr);
            _asrStatus.text = $"{offline}\n{online}";
            ApplyFailedClass(_asrStatus,
                AsrInitProgressBus.OfflineFailed || AsrInitProgressBus.OnlineFailed);
        }

        private void HandleVadChanged()
        {
            if (_vadStatus == null)
                return;
            _vadStatus.text = VadSampleStatusUtil.BuildVadLine(_services?.Vad);
            ApplyFailedClass(_vadStatus, VadInitProgressBus.VadFailed);
        }

        private static void ApplyFailedClass(Label label, bool failed)
        {
            const string cls = "is-failed";
            if (failed)
                label.AddToClassList(cls);
            else
                label.RemoveFromClassList(cls);
        }
    }
}
