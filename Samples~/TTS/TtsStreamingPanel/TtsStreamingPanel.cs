using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Streaming TTS sample with side-by-side comparison.
    /// "Speak (Streaming)" returns a handle as soon as the first sentence
    /// is generated. "Speak (Blocking)" waits for the whole synthesis.
    /// Both timings are reported so the latency win is visible even when
    /// total generation is fast enough that ears can't perceive the gap.
    /// </summary>
    public sealed class TtsStreamingPanel : ISamplePanel
    {
        private ITtsService _service;
        private AudioSource _audio;
        private Action _onBack;

        private TextField _textField;
        private Button _btnSpeakStreaming;
        private Button _btnSpeakBlocking;
        private Button _btnStop;
        private Button _btnBack;
        private Label _statusLabel;
        private Label _streamingResult;
        private Label _blockingResult;

        private TtsPlaybackHandle _handle;
        private CancellationTokenSource _cts;
        private bool _isWorking;

        public void Bind(
            VisualElement root,
            ITtsService service,
            AudioSource audio,
            Action onBack)
        {
            _service = service;
            _audio = audio;
            _onBack = onBack;

            _textField = root.Q<TextField>("textField");
            _btnSpeakStreaming = root.Q<Button>("btnSpeakStreaming");
            _btnSpeakBlocking = root.Q<Button>("btnSpeakBlocking");
            _btnStop = root.Q<Button>("btnStop");
            _btnBack = root.Q<Button>("backButton");
            _statusLabel = root.Q<Label>("statusLabel");
            _streamingResult = root.Q<Label>("streamingResult");
            _blockingResult = root.Q<Label>("blockingResult");

            if (_btnSpeakStreaming != null) _btnSpeakStreaming.clicked += HandleSpeakStreaming;
            if (_btnSpeakBlocking != null) _btnSpeakBlocking.clicked += HandleSpeakBlocking;
            if (_btnStop != null) _btnStop.clicked += HandleStop;
            if (_btnBack != null) _btnBack.clicked += HandleBack;

            UpdateButtons();
            SetStatus(_service != null && _service.IsReady ? "Ready." : "Engine not loaded.");
            SetStreamingResult("");
            SetBlockingResult("");
        }

        public void Unbind()
        {
            CancelAndStop();

            if (_btnSpeakStreaming != null) _btnSpeakStreaming.clicked -= HandleSpeakStreaming;
            if (_btnSpeakBlocking != null) _btnSpeakBlocking.clicked -= HandleSpeakBlocking;
            if (_btnStop != null) _btnStop.clicked -= HandleStop;
            if (_btnBack != null) _btnBack.clicked -= HandleBack;

            _textField = null;
            _btnSpeakStreaming = null;
            _btnSpeakBlocking = null;
            _btnStop = null;
            _btnBack = null;
            _statusLabel = null;
            _streamingResult = null;
            _blockingResult = null;
            _service = null;
            _audio = null;
            _onBack = null;
        }

        // ── Handlers ──

        private async void HandleSpeakStreaming()
        {
            if (_isWorking || _service == null || !_service.IsReady)
                return;

            string text = _textField?.value ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("Enter text first.");
                return;
            }

            CancelAndStop();
            BeginWork("Streaming: generating first chunk…");

            _cts = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _handle = await _service.SpeakStreamingAsync(text, _audio, _cts.Token);
                stopwatch.Stop();

                if (_handle == null)
                {
                    SetStatus("Streaming returned null (gen failed).");
                    return;
                }

                long firstAudioMs = stopwatch.ElapsedMilliseconds;
                SetStatus("Streaming…");
                SetStreamingResult(
                    $"Streaming → first audio in {firstAudioMs} ms{CacheTag(firstAudioMs)}");

                _handle.Completed += OnPlaybackEnd;
                _handle.Stopped += OnPlaybackEnd;
            }
            catch (OperationCanceledException)
            {
                SetStatus("Streaming cancelled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsStreamingPanel streaming error: {ex}");
            }
            finally
            {
                EndWork();
            }
        }

        private async void HandleSpeakBlocking()
        {
            if (_isWorking || _service == null || !_service.IsReady)
                return;

            string text = _textField?.value ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("Enter text first.");
                return;
            }

            CancelAndStop();
            BeginWork("Blocking: generating full audio…");

            _cts = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _handle = await _service.GenerateAndPlayWithHandleAsync(
                    text, _audio, _cts.Token);
                stopwatch.Stop();

                if (_handle == null)
                {
                    SetStatus("Blocking returned null (gen failed).");
                    return;
                }

                long firstAudioMs = stopwatch.ElapsedMilliseconds;
                SetStatus("Playing…");
                SetBlockingResult(
                    $"Blocking → first audio in {firstAudioMs} ms{CacheTag(firstAudioMs)}");

                _handle.Completed += OnPlaybackEnd;
                _handle.Stopped += OnPlaybackEnd;
            }
            catch (OperationCanceledException)
            {
                SetStatus("Blocking cancelled.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsStreamingPanel blocking error: {ex}");
            }
            finally
            {
                EndWork();
            }
        }

        private void HandleStop()
        {
            if (_handle == null && _cts == null)
            {
                SetStatus("Nothing to stop.");
                return;
            }

            CancelAndStop();
            SetStatus("Stopped.");
        }

        private void HandleBack() => _onBack?.Invoke();

        // ── Helpers ──

        private void OnPlaybackEnd()
        {
            if (_handle != null)
            {
                _handle.Completed -= OnPlaybackEnd;
                _handle.Stopped -= OnPlaybackEnd;
                _handle = null;
            }
            SetStatus("Done.");
            UpdateButtons();
        }

        private void CancelAndStop()
        {
            if (_handle != null)
            {
                _handle.Completed -= OnPlaybackEnd;
                _handle.Stopped -= OnPlaybackEnd;
                _handle.Dispose();
                _handle = null;
            }

            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { /* swallow */ }
                _cts.Dispose();
                _cts = null;
            }

            _isWorking = false;
            UpdateButtons();
        }

        private void BeginWork(string status)
        {
            _isWorking = true;
            UpdateButtons();
            SetStatus(status);
        }

        private void EndWork()
        {
            _isWorking = false;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool ready = _service != null && _service.IsReady;
            _btnSpeakStreaming?.SetEnabled(ready && !_isWorking);
            _btnSpeakBlocking?.SetEnabled(ready && !_isWorking);
            _btnStop?.SetEnabled(_handle != null || _isWorking);
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void SetStreamingResult(string text)
        {
            if (_streamingResult != null)
                _streamingResult.text = text;
        }

        private void SetBlockingResult(string text)
        {
            if (_blockingResult != null)
                _blockingResult.text = text;
        }

        /// <summary>
        /// Suffix flagging suspiciously fast results as a likely LRU cache hit.
        /// Real TTS generation needs at least a couple hundred ms even on
        /// fast hardware. The Blocking path goes through `CachedTtsService`
        /// (which caches `Generate*` results), so a second click on the same
        /// text returns instantly. The Streaming path uses
        /// `GenerateWithCallback` which bypasses the cache, so it always
        /// re-generates — the asymmetry can otherwise mislead the comparison.
        /// </summary>
        private static string CacheTag(long firstAudioMs)
        {
            return firstAudioMs < CacheHitThresholdMs
                ? "  (cached hit)"
                : string.Empty;
        }

        private const long CacheHitThresholdMs = 100;
    }
}
