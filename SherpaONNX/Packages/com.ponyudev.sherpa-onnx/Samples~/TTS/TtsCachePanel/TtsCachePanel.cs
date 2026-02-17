using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Cache control sample — set pool/cache sizes at runtime,
    /// play three phrases in parallel via AudioSource pool,
    /// observe per-cache stats, timing and generation info live.
    /// </summary>
    public sealed class TtsCachePanel : ISamplePanel
    {
        private static readonly string[] Phrases =
        {
            "Hello world",
            "How are you today",
            "This is a cache test",
        };

        private ITtsService _service;
        private ITtsCacheControl _cache;
        private AudioSource _fallbackAudio;
        private Action _onBack;
        private VisualElement _root;

        // ── Size fields ──
        private TextField _sizeEnginePool;
        private TextField _sizeResult;
        private TextField _sizeClip;
        private TextField _sizeSource;

        // ── Per-cache stat labels ──
        private Label _statEngine;
        private Label _statResult;
        private Label _statClip;
        private Label _statSource;

        // ── Per-cache clear buttons ──
        private Button _clearResult;
        private Button _clearClip;
        private Button _clearSource;

        // ── Playback ──
        private Button _btnPhrase1;
        private Button _btnPhrase2;
        private Button _btnPhrase3;
        private Button _btnGenerateAll;
        private Button _clearAllButton;
        private Button _backButton;
        private Label _timingLabel;
        private Label _statusLabel;
        private Label _infoLabel;

        private IVisualElementScheduledItem _statsSchedule;

        // ── Per-cache timing ──
        private long _lastGenMs;
        private long _lastResultMs;
        private long _lastClipMs;
        private long _lastSourceMs;

        // ── ISamplePanel ──

        public void Bind(
            VisualElement root,
            ITtsService service,
            AudioSource audio,
            Action onBack)
        {
            _root = root;
            _service = service;
            _cache = service as ITtsCacheControl;
            _fallbackAudio = audio;
            _onBack = onBack;

            QueryElements(root);
            InitValues();
            SubscribeAll();

            _statsSchedule = _root?.schedule
                .Execute(UpdateAllStats).Every(200);

            UpdateInfo();
            UpdateAllStats();

            if (_cache == null)
                SetStatus("No cache — service is not CachedTtsService.");
        }

        public void Unbind()
        {
            _statsSchedule?.Pause();
            UnsubscribeAll();
            NullifyAll();
        }

        // ── Query ──

        private void QueryElements(VisualElement root)
        {
            _sizeEnginePool = root.Q<TextField>("sizeEnginePool");
            _sizeResult = root.Q<TextField>("sizeResultCache");
            _sizeClip = root.Q<TextField>("sizeClipPool");
            _sizeSource = root.Q<TextField>("sizeSourcePool");

            _statEngine = root.Q<Label>("statEngine");
            _statResult = root.Q<Label>("statResult");
            _statClip = root.Q<Label>("statClip");
            _statSource = root.Q<Label>("statSource");

            _clearResult = root.Q<Button>("clearResult");
            _clearClip = root.Q<Button>("clearClip");
            _clearSource = root.Q<Button>("clearSource");

            _btnPhrase1 = root.Q<Button>("btnPhrase1");
            _btnPhrase2 = root.Q<Button>("btnPhrase2");
            _btnPhrase3 = root.Q<Button>("btnPhrase3");
            _btnGenerateAll = root.Q<Button>("btnGenerateAll");
            _clearAllButton = root.Q<Button>("clearAllButton");
            _backButton = root.Q<Button>("backButton");
            _timingLabel = root.Q<Label>("timingLabel");
            _statusLabel = root.Q<Label>("statusLabel");
            _infoLabel = root.Q<Label>("infoLabel");
        }

        private void InitValues()
        {
            if (_sizeEnginePool != null && _service != null)
                _sizeEnginePool.value = _service.EnginePoolSize.ToString();

            if (_cache == null)
                return;

            if (_sizeResult != null)
                _sizeResult.value = _cache.ResultCacheMaxSize.ToString();
            if (_sizeClip != null)
                _sizeClip.value = _cache.AudioClipPoolMaxSize.ToString();
            if (_sizeSource != null)
                _sizeSource.value = _cache.AudioSourcePoolMaxSize.ToString();
        }

        // ── Subscribe / Unsubscribe ──

        private void SubscribeAll()
        {
            _sizeEnginePool?.RegisterValueChangedCallback(OnSizeEnginePool);
            _sizeResult?.RegisterValueChangedCallback(OnSizeResult);
            _sizeClip?.RegisterValueChangedCallback(OnSizeClip);
            _sizeSource?.RegisterValueChangedCallback(OnSizeSource);

            if (_clearResult != null) _clearResult.clicked += HandleClearResult;
            if (_clearClip != null) _clearClip.clicked += HandleClearClip;
            if (_clearSource != null) _clearSource.clicked += HandleClearSource;

            if (_btnPhrase1 != null) _btnPhrase1.clicked += HandlePhrase1;
            if (_btnPhrase2 != null) _btnPhrase2.clicked += HandlePhrase2;
            if (_btnPhrase3 != null) _btnPhrase3.clicked += HandlePhrase3;
            if (_btnGenerateAll != null) _btnGenerateAll.clicked += HandleGenerateAll;
            if (_clearAllButton != null) _clearAllButton.clicked += HandleClearAll;
            if (_backButton != null) _backButton.clicked += HandleBack;
        }

        private void UnsubscribeAll()
        {
            _sizeEnginePool?.UnregisterValueChangedCallback(OnSizeEnginePool);
            _sizeResult?.UnregisterValueChangedCallback(OnSizeResult);
            _sizeClip?.UnregisterValueChangedCallback(OnSizeClip);
            _sizeSource?.UnregisterValueChangedCallback(OnSizeSource);

            if (_clearResult != null) _clearResult.clicked -= HandleClearResult;
            if (_clearClip != null) _clearClip.clicked -= HandleClearClip;
            if (_clearSource != null) _clearSource.clicked -= HandleClearSource;

            if (_btnPhrase1 != null) _btnPhrase1.clicked -= HandlePhrase1;
            if (_btnPhrase2 != null) _btnPhrase2.clicked -= HandlePhrase2;
            if (_btnPhrase3 != null) _btnPhrase3.clicked -= HandlePhrase3;
            if (_btnGenerateAll != null) _btnGenerateAll.clicked -= HandleGenerateAll;
            if (_clearAllButton != null) _clearAllButton.clicked -= HandleClearAll;
            if (_backButton != null) _backButton.clicked -= HandleBack;
        }

        private void NullifyAll()
        {
            _sizeEnginePool = null;
            _sizeResult = null;
            _sizeClip = null;
            _sizeSource = null;
            _statEngine = null;
            _statResult = null;
            _statClip = null;
            _statSource = null;
            _clearResult = null;
            _clearClip = null;
            _clearSource = null;
            _btnPhrase1 = null;
            _btnPhrase2 = null;
            _btnPhrase3 = null;
            _btnGenerateAll = null;
            _clearAllButton = null;
            _backButton = null;
            _timingLabel = null;
            _statusLabel = null;
            _infoLabel = null;
            _service = null;
            _cache = null;
            _fallbackAudio = null;
            _onBack = null;
            _root = null;
        }

        // ── Size handlers ──
        // Value 0 = disabled, >= 1 = enabled with that capacity.

        private void OnSizeEnginePool(ChangeEvent<string> evt)
        {
            if (!TryParseNonNegative(evt.newValue, out int val) || val < 1)
                return;
            if (_service != null)
                _service.EnginePoolSize = val;
            SetStatus($"Engine pool: {val}");
        }

        private void OnSizeResult(ChangeEvent<string> evt)
        {
            if (!TryParseNonNegative(evt.newValue, out int val))
                return;
            if (_cache == null)
                return;

            if (val == 0)
            {
                _cache.ResultCacheEnabled = false;
                SetStatus("Result cache: OFF");
            }
            else
            {
                _cache.ResultCacheEnabled = true;
                _cache.ResultCacheMaxSize = val;
                SetStatus($"Result cache: {val}");
            }
        }

        private void OnSizeClip(ChangeEvent<string> evt)
        {
            if (!TryParseNonNegative(evt.newValue, out int val))
                return;
            if (_cache == null)
                return;

            if (val == 0)
            {
                _cache.AudioClipPoolEnabled = false;
                SetStatus("Clip pool: OFF");
            }
            else
            {
                _cache.AudioClipPoolEnabled = true;
                _cache.AudioClipPoolMaxSize = val;
                SetStatus($"Clip pool: {val}");
            }
        }

        private void OnSizeSource(ChangeEvent<string> evt)
        {
            if (!TryParseNonNegative(evt.newValue, out int val))
                return;
            if (_cache == null)
                return;

            if (val == 0)
            {
                _cache.AudioSourcePoolEnabled = false;
                SetStatus("Source pool: OFF");
            }
            else
            {
                _cache.AudioSourcePoolEnabled = true;
                _cache.AudioSourcePoolMaxSize = val;
                SetStatus($"Source pool: {val}");
            }
        }

        // ── Per-cache clear ──

        private void HandleClearResult()
        {
            _cache?.ClearResultCache();
            _lastResultMs = 0;
            SetStatus("Result cache cleared.");
        }

        private void HandleClearClip()
        {
            _cache?.ClearClipPool();
            _lastClipMs = 0;
            SetStatus("Clip pool cleared.");
        }

        private void HandleClearSource()
        {
            _cache?.ClearSourcePool();
            _lastSourceMs = 0;
            SetStatus("Source pool cleared.");
        }

        private void HandleClearAll()
        {
            _cache?.ClearAll();
            _lastGenMs = 0;
            _lastResultMs = 0;
            _lastClipMs = 0;
            _lastSourceMs = 0;
            SetStatus("All caches cleared.");
            SetTiming("");
        }

        // ── Phrase buttons ──

        private void HandlePhrase1() => GenerateAndPlay(0);
        private void HandlePhrase2() => GenerateAndPlay(1);
        private void HandlePhrase3() => GenerateAndPlay(2);

        private async void GenerateAndPlay(int index)
        {
            if (_service == null || !_service.IsReady)
                return;

            string text = Phrases[index];
            SetStatus($"Generating: \"{text}\"...");

            try
            {
                var sw = Stopwatch.StartNew();
                var result = await _service.GenerateAsync(text);
                sw.Stop();

                if (result == null || !result.IsValid)
                {
                    SetStatus($"No audio for phrase {index + 1}.");
                    return;
                }

                _lastGenMs = sw.ElapsedMilliseconds;
                _lastResultMs = sw.ElapsedMilliseconds;

                bool isCacheHit = sw.ElapsedMilliseconds < 5;
                string hitLabel = isCacheHit ? "CACHE HIT" : "generated";

                var swPlay = Stopwatch.StartNew();
                PlayResult(result, index);
                swPlay.Stop();
                _lastSourceMs = swPlay.ElapsedMilliseconds;

                SetStatus(
                    $"Phrase {index + 1}: {hitLabel} " +
                    $"in {sw.ElapsedMilliseconds}ms " +
                    $"({result.DurationSeconds:F1}s audio)");

                result.Dispose();
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsCachePanel error: {ex}");
            }
        }

        // ── Generate All 3 ──

        private async void HandleGenerateAll()
        {
            if (_service == null || !_service.IsReady)
                return;

            SetStatus("Generating all 3 phrases in parallel...");

            try
            {
                var sw = Stopwatch.StartNew();

                var t0 = _service.GenerateAsync(Phrases[0]);
                var t1 = _service.GenerateAsync(Phrases[1]);
                var t2 = _service.GenerateAsync(Phrases[2]);

                var results = await Task.WhenAll(t0, t1, t2);
                sw.Stop();

                _lastGenMs = sw.ElapsedMilliseconds;
                _lastResultMs = sw.ElapsedMilliseconds;

                int played = 0;
                var swPlay = Stopwatch.StartNew();
                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] != null && results[i].IsValid)
                    {
                        PlayResult(results[i], i);
                        played++;
                    }
                    results[i]?.Dispose();
                }
                swPlay.Stop();
                _lastSourceMs = swPlay.ElapsedMilliseconds;

                bool likelyCached = sw.ElapsedMilliseconds < 10;
                string hitLabel = likelyCached ? " (ALL CACHE HITS)" : "";

                SetStatus(
                    $"All 3 done in {sw.ElapsedMilliseconds}ms" +
                    $"{hitLabel}, playing {played}");
                SetTiming(
                    $"Total: {sw.ElapsedMilliseconds}ms for 3 phrases" +
                    $"{hitLabel}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsCachePanel error: {ex}");
            }
        }

        // ── Playback ──

        private void PlayResult(TtsResult result, int index)
        {
            // Try pooled clip first, fall back to new allocation.
            var clip = _cache?.RentClip(result)
                       ?? result.ToAudioClip($"tts-phrase-{index}");

            var source = _cache?.RentSource() ?? _fallbackAudio;
            source.clip = clip;
            source.Play();
        }

        private void HandleBack() => _onBack?.Invoke();

        // ── Stats (updated every 200ms) ──

        private void UpdateAllStats()
        {
            if (_cache == null)
                return;

            int engines = _service?.EnginePoolSize ?? 0;
            string engineTime = _lastGenMs > 0
                ? $"  |  last gen: {_lastGenMs}ms" : "";
            SetLabel(_statEngine, $"Pool: {engines}{engineTime}");

            if (_cache.ResultCacheEnabled)
            {
                int rc = _cache.ResultCacheCount;
                int rm = _cache.ResultCacheMaxSize;
                string resultTime = _lastResultMs > 0
                    ? $"  |  last: {_lastResultMs}ms" : "";
                SetLabel(_statResult, $"{rc}/{rm} entries{resultTime}");
            }
            else
            {
                SetLabel(_statResult, "OFF (set > 0 to enable)");
            }

            if (_cache.AudioClipPoolEnabled)
            {
                int cc = _cache.AudioClipAvailableCount;
                int cm = _cache.AudioClipPoolMaxSize;
                SetLabel(_statClip, $"{cc}/{cm} available");
            }
            else
            {
                SetLabel(_statClip, "OFF (set > 0 to enable)");
            }

            if (_cache.AudioSourcePoolEnabled)
            {
                int sa = _cache.AudioSourceAvailableCount;
                int sm = _cache.AudioSourcePoolMaxSize;
                string sourceTime = _lastSourceMs > 0
                    ? $"  |  last play: {_lastSourceMs}ms" : "";
                SetLabel(_statSource, $"{sa}/{sm} idle{sourceTime}");
            }
            else
            {
                SetLabel(_statSource, "OFF (set > 0 to enable)");
            }
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void SetTiming(string text)
        {
            if (_timingLabel != null)
                _timingLabel.text = text;
        }

        private void UpdateInfo()
        {
            if (_infoLabel == null)
                return;

            if (_service == null || !_service.IsReady)
            {
                _infoLabel.text = "Engine not loaded.";
                return;
            }

            var profile = _service.ActiveProfile;
            _infoLabel.text =
                $"Profile: {profile?.profileName ?? "---"} | " +
                $"Type: {profile?.modelType}";
        }

        private static void SetLabel(Label label, string text)
        {
            if (label != null)
                label.text = text;
        }

        private static bool TryParseNonNegative(string text, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (!int.TryParse(text, out result))
                return false;
            return result >= 0;
        }
    }
}
