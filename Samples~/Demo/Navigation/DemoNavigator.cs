using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Data;
using PonyuDev.SherpaOnnx.Vad;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Single MonoBehaviour driving the whole unified Sherpa-ONNX
    /// demo scene. Owns every service (TTS / offline + online ASR /
    /// VAD / pipeline), the shared <see cref="MicrophoneSource"/>,
    /// and the playback <see cref="AudioSource"/>. Hands them all to
    /// <see cref="IDemoView"/> implementations through a shared
    /// <see cref="DemoServices"/> bag.
    ///
    /// Initialization is "compromise eager": all three services start
    /// loading on Awake so a user who jumps straight into a panel
    /// does not have to wait for late-binding, but the microphone is
    /// only opened when a panel that needs capture binds — saving
    /// battery / permission-flicker for menu-only browsing.
    ///
    /// Navigation is two-level (top → sub-menu → panel) but the
    /// implementation is generic: ids form a "/"-separated tree, and
    /// <see cref="Back"/> just pops one segment. Add a new panel by
    /// dropping a UXML in the inspector and registering an
    /// <see cref="IDemoView"/> in <see cref="Awake"/> under
    /// <c>"module/id"</c>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DemoNavigator : MonoBehaviour, IDemoNavigator
    {
        // View ids — also used as VisualTreeAsset look-up keys.
        public const string IdTop = "top";
        public const string IdTts = "tts";
        public const string IdTtsSimple = "tts/simple";
        public const string IdTtsProgress = "tts/progress";
        public const string IdTtsConfig = "tts/config";
        public const string IdTtsCache = "tts/cache";
        public const string IdTtsControls = "tts/controls";
        public const string IdTtsStreaming = "tts/streaming";
        public const string IdAsr = "asr";
        public const string IdAsrFile = "asr/file";
        public const string IdAsrStream = "asr/stream";
        public const string IdAsrCombined = "asr/combined";
        public const string IdVad = "vad";
        public const string IdVadDemo = "vad/demo";

        [Header("Top-level menu")]
        [SerializeField] private VisualTreeAsset _topMenuAsset;

        [Header("TTS")]
        [SerializeField] private VisualTreeAsset _ttsSubMenuAsset;
        [SerializeField] private VisualTreeAsset _ttsSimpleAsset;
        [SerializeField] private VisualTreeAsset _ttsProgressAsset;
        [SerializeField] private VisualTreeAsset _ttsConfigAsset;
        [SerializeField] private VisualTreeAsset _ttsCacheAsset;
        [SerializeField] private VisualTreeAsset _ttsControlsAsset;
        [SerializeField] private VisualTreeAsset _ttsStreamingAsset;

        [Header("ASR")]
        [SerializeField] private VisualTreeAsset _asrSubMenuAsset;
        [SerializeField] private VisualTreeAsset _asrFileAsset;
        [SerializeField] private VisualTreeAsset _asrStreamAsset;
        [SerializeField] private VisualTreeAsset _asrCombinedAsset;
        [SerializeField] private AudioClip _asrSampleClip;

        [Header("VAD")]
        [SerializeField] private VisualTreeAsset _vadSubMenuAsset;
        [SerializeField] private VisualTreeAsset _vadDemoAsset;

        private UIDocument _document;
        private readonly DemoServices _services = new DemoServices();
        private TtsService _innerTts;
        private CachedTtsService _cachedTts;

        private readonly Dictionary<string, IDemoView> _views =
            new Dictionary<string, IDemoView>();
        private readonly Dictionary<string, VisualTreeAsset> _assets =
            new Dictionary<string, VisualTreeAsset>();

        private IDemoView _activeView;
        private string _activeId;
        private IDemoView _pendingView;

        private async void Awake()
        {
            _document = GetComponent<UIDocument>();
            _services.AudioSource = GetComponent<AudioSource>();
            if (_services.AudioSource == null)
                _services.AudioSource = gameObject.AddComponent<AudioSource>();
            _services.SampleClip = _asrSampleClip;

            RegisterViews();
            RegisterAssets();

            await InitializeServicesAsync();
        }

        private void OnEnable()
        {
            // Awake may not have finished yet (services start async)
            // but we can safely show the top menu — its Bind only
            // reads the latest *InitProgressBus events, which are
            // already published or will be published shortly.
            NavigateTo(IdTop);
        }

        private void OnDestroy()
        {
            UnbindActive();

            _services.Pipeline?.Dispose();
            _services.Pipeline = null;

            _services.Microphone?.Dispose();
            _services.Microphone = null;

            _services.OfflineAsr?.Dispose();
            _services.OfflineAsr = null;

            _services.OnlineAsr?.Dispose();
            _services.OnlineAsr = null;

            _services.Vad?.Dispose();
            _services.Vad = null;

            if (_cachedTts != null)
            {
                _cachedTts.Dispose();
                _cachedTts = null;
                _innerTts = null;
            }
            else
            {
                _innerTts?.Dispose();
                _innerTts = null;
            }
            _services.Tts = null;
        }

        // ── IDemoNavigator ──

        public void NavigateTo(string viewId)
        {
            if (string.IsNullOrEmpty(viewId))
                return;

            if (!_views.TryGetValue(viewId, out IDemoView view))
            {
                SherpaOnnxLog.RuntimeWarning(
                    $"[SherpaOnnx] DemoNavigator: unknown view id '{viewId}'.");
                return;
            }
            if (!_assets.TryGetValue(viewId, out VisualTreeAsset asset) || asset == null)
            {
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] DemoNavigator: no VisualTreeAsset for '{viewId}'. " +
                    "Set it in the inspector.");
                return;
            }

            UnbindActive();

            _document.visualTreeAsset = asset;
            _activeId = viewId;
            _pendingView = view;

            // UIDocument needs one frame to materialise the tree —
            // BindPending runs next tick, picks up the latest pending
            // view, and clears the field.
            _document.rootVisualElement.schedule.Execute(BindPending);
        }

        private void BindPending()
        {
            IDemoView view = _pendingView;
            if (view == null)
                return;
            _pendingView = null;
            view.Bind(_document.rootVisualElement, _services, this);
            _activeView = view;
        }

        public void Back()
        {
            if (string.IsNullOrEmpty(_activeId) || _activeId == IdTop)
                return;

            int slash = _activeId.LastIndexOf('/');
            string parent = slash >= 0 ? _activeId.Substring(0, slash) : IdTop;
            NavigateTo(parent);
        }

        // ── Init ──

        private async UniTask InitializeServicesAsync()
        {
            _innerTts = new TtsService();
            // Publish the un-cached service immediately so views that
            // read DemoServices.Tts during init see the real instance
            // (otherwise a "Ready" event firing mid-init would race with
            // AttachCachedTts and the top menu would briefly render
            // "Service not available"). UpgradeToCachedTts swaps the
            // reference to the cached decorator once init completes.
            _services.Tts = _innerTts;
            _services.OfflineAsr = new AsrService();
            _services.OnlineAsr = new OnlineAsrService();
            _services.Vad = new VadService();

            MicrophoneSettingsData micSettings = await MicrophoneSettingsLoader.LoadAsync();
            _services.Microphone = new MicrophoneSource(micSettings);

            // Run all four service inits in parallel — each services
            // its own bus, so the UI can render per-service status
            // independently. UniTasks are single-await (unlike Task),
            // so we must not pass any of these handles into a second
            // awaiter — WhenAll consumes them.
            UniTask ttsTask = _innerTts.InitializeAsync(TtsInitProgressBus.PublishEvent);
            UniTask offlineTask = _services.OfflineAsr.InitializeAsync(AsrInitProgressBus.PublishOfflineEvent);
            UniTask onlineTask = _services.OnlineAsr.InitializeAsync(AsrInitProgressBus.PublishOnlineEvent);
            UniTask vadTask = _services.Vad.InitializeAsync(VadInitProgressBus.PublishVadEvent);

            try
            {
                await UniTask.WhenAll(ttsTask, offlineTask, onlineTask, vadTask);

                UpgradeToCachedTts();
                AttachVadAsrPipeline();
                MirrorOfflineAsrToVadBus();
            }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError(
                    "[SherpaOnnx] DemoNavigator: service initialization failed: " + ex.Message);
                MirrorOfflineAsrToVadBus();
            }
        }

        // The VadAsrPipeline requires a fully-initialised offline ASR.
        // The offline service publishes its own AsrInitProgressBus
        // events; we mirror the latest one onto the VAD-side companion
        // bus so the VAD menu / panel can show "Pipeline ASR ready"
        // status without subscribing to two buses.
        private static void MirrorOfflineAsrToVadBus()
        {
            if (AsrInitProgressBus.OfflineHasEvent)
                VadInitProgressBus.PublishAsrEvent(AsrInitProgressBus.LastOfflineEvent);
        }

        private void UpgradeToCachedTts()
        {
            TtsCacheSettings cache = _innerTts.Settings?.cache;
            if (cache == null)
                return;

            _cachedTts = new CachedTtsService(_innerTts, cache, transform);
            // Swap the live reference so panels that bind after init
            // get the cached decorator. Views that bound before init
            // completed are unaffected — they hold the inner service
            // reference and ITtsService transparently routes through
            // the cache via Tts engine pool ownership.
            _services.Tts = _cachedTts;
        }

        private void AttachVadAsrPipeline()
        {
            if (_services.Vad != null && _services.Vad.IsReady
                && _services.OfflineAsr != null && _services.OfflineAsr.IsReady)
            {
                _services.Pipeline = new VadAsrPipeline(_services.Vad, _services.OfflineAsr);
            }
        }

        // ── View / Asset registration ──

        private void RegisterViews()
        {
            _views[IdTop] = new DemoTopMenu();

            _views[IdTts] = new TtsSubMenu();
            _views[IdTtsSimple] = new TtsSimplePanel();
            _views[IdTtsProgress] = new TtsProgressPanel();
            _views[IdTtsConfig] = new TtsConfigPanel();
            _views[IdTtsCache] = new TtsCachePanel();
            _views[IdTtsControls] = new TtsControlsPanel();
            _views[IdTtsStreaming] = new TtsStreamingPanel();

            _views[IdAsr] = new AsrSubMenu();
            _views[IdAsrFile] = new AsrFilePanel();
            _views[IdAsrStream] = new AsrStreamPanel();
            _views[IdAsrCombined] = new AsrCombinedPanel();

            _views[IdVad] = new VadSubMenu();
            _views[IdVadDemo] = new VadDemoPanel();
        }

        private void RegisterAssets()
        {
            _assets[IdTop] = _topMenuAsset;

            _assets[IdTts] = _ttsSubMenuAsset;
            _assets[IdTtsSimple] = _ttsSimpleAsset;
            _assets[IdTtsProgress] = _ttsProgressAsset;
            _assets[IdTtsConfig] = _ttsConfigAsset;
            _assets[IdTtsCache] = _ttsCacheAsset;
            _assets[IdTtsControls] = _ttsControlsAsset;
            _assets[IdTtsStreaming] = _ttsStreamingAsset;

            _assets[IdAsr] = _asrSubMenuAsset;
            _assets[IdAsrFile] = _asrFileAsset;
            _assets[IdAsrStream] = _asrStreamAsset;
            _assets[IdAsrCombined] = _asrCombinedAsset;

            _assets[IdVad] = _vadSubMenuAsset;
            _assets[IdVadDemo] = _vadDemoAsset;
        }

        private void UnbindActive()
        {
            if (_activeView == null)
                return;
            _activeView.Unbind();
            _activeView = null;
        }
    }
}
