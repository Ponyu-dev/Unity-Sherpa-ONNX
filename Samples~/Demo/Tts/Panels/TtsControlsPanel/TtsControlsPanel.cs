using System;
using System.Collections.Generic;
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
    /// Runtime Controls sample — demonstrates the handle-based playback API
    /// (<see cref="TtsPlaybackHandle"/>) with cancellation, fade-out stop,
    /// and parallel-handle StopAll. Use this scene to regression-test the
    /// runtime-controls branch of the TTS module.
    /// </summary>
    public sealed class TtsControlsPanel : IDemoView
    {
        private ITtsService _service;
        private AudioSource _audio;
        private AudioSource _audioSecondary; // for parallel-handle demo
        private IDemoNavigator _nav;

        private TextField _textField;
        private IntegerField _fadeMsField;
        private Button _btnGenerate;
        private Button _btnCancelGen;
        private Button _btnStopLast;
        private Button _btnStopAll;
        private Button _btnStopLastInstant;
        private Button _btnStopAllInstant;
        private Button _btnGenerateTwo;
        private Button _btnBack;
        private Label _statusLabel;
        private Label _activeLabel;
        private Label _historyLabel;

        private readonly List<TtsPlaybackHandle> _handles = new();
        private CancellationTokenSource _activeCts;
        private bool _isGenerating;
        private readonly List<string> _history = new();
        private const int HistoryMaxLines = 10;

        // ── IDemoView ──

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _service = services?.Tts;
            _audio = services?.AudioSource;
            _nav = nav;

            // Spin up a sibling AudioSource so the parallel-handle demo
            // doesn't interrupt the first handle's playback.
            _audioSecondary = _audio.gameObject.AddComponent<AudioSource>();
            _audioSecondary.playOnAwake = false;

            _textField = root.Q<TextField>("textField");
            _fadeMsField = root.Q<IntegerField>("fadeMsField");
            _btnGenerate = root.Q<Button>("btnGenerate");
            _btnCancelGen = root.Q<Button>("btnCancelGen");
            _btnStopLast = root.Q<Button>("btnStopLast");
            _btnStopAll = root.Q<Button>("btnStopAll");
            _btnStopLastInstant = root.Q<Button>("btnStopLastInstant");
            _btnStopAllInstant = root.Q<Button>("btnStopAllInstant");
            _btnGenerateTwo = root.Q<Button>("btnGenerateTwo");
            _btnBack = root.Q<Button>("backButton");
            _statusLabel = root.Q<Label>("statusLabel");
            _activeLabel = root.Q<Label>("activeLabel");
            _historyLabel = root.Q<Label>("historyLabel");

            if (_btnGenerate != null) _btnGenerate.clicked += HandleGenerate;
            if (_btnCancelGen != null) _btnCancelGen.clicked += HandleCancelGeneration;
            if (_btnStopLast != null) _btnStopLast.clicked += HandleStopLastWithFade;
            if (_btnStopAll != null) _btnStopAll.clicked += HandleStopAllWithFade;
            if (_btnStopLastInstant != null) _btnStopLastInstant.clicked += HandleStopLastInstant;
            if (_btnStopAllInstant != null) _btnStopAllInstant.clicked += HandleStopAllInstant;
            if (_btnGenerateTwo != null) _btnGenerateTwo.clicked += HandleGenerateTwoParallel;
            if (_btnBack != null) _btnBack.clicked += HandleBack;

            UpdateButtons();
            UpdateActiveLabel();

            TtsInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();
        }

        public void Unbind()
        {
            TtsInitProgressBus.Changed -= HandleInitProgressChanged;

            // Hard-stop everything still in flight before tearing down.
            CancelGeneration();
            for (int i = _handles.Count - 1; i >= 0; i--)
                _handles[i]?.Dispose();
            _handles.Clear();

            if (_audioSecondary != null)
            {
                UnityEngine.Object.Destroy(_audioSecondary);
                _audioSecondary = null;
            }

            if (_btnGenerate != null) _btnGenerate.clicked -= HandleGenerate;
            if (_btnCancelGen != null) _btnCancelGen.clicked -= HandleCancelGeneration;
            if (_btnStopLast != null) _btnStopLast.clicked -= HandleStopLastWithFade;
            if (_btnStopAll != null) _btnStopAll.clicked -= HandleStopAllWithFade;
            if (_btnStopLastInstant != null) _btnStopLastInstant.clicked -= HandleStopLastInstant;
            if (_btnStopAllInstant != null) _btnStopAllInstant.clicked -= HandleStopAllInstant;
            if (_btnGenerateTwo != null) _btnGenerateTwo.clicked -= HandleGenerateTwoParallel;
            if (_btnBack != null) _btnBack.clicked -= HandleBack;

            _textField = null;
            _fadeMsField = null;
            _btnGenerate = null;
            _btnCancelGen = null;
            _btnStopLast = null;
            _btnStopAll = null;
            _btnStopLastInstant = null;
            _btnStopAllInstant = null;
            _btnGenerateTwo = null;
            _btnBack = null;
            _statusLabel = null;
            _activeLabel = null;
            _historyLabel = null;
            _service = null;
            _audio = null;
            _nav = null;
        }

        // ── Handlers ──

        private async void HandleGenerate()
        {
            if (!CheckReady() || _isGenerating)
                return;

            string text = GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("Enter text first.");
                return;
            }

            await GenerateAndTrackAsync(text, _audio, "A");
        }

        private async void HandleGenerateTwoParallel()
        {
            if (!CheckReady() || _isGenerating)
                return;

            string text = GetText();
            if (string.IsNullOrWhiteSpace(text))
            {
                SetStatus("Enter text first.");
                return;
            }

            // Two parallel generations on separate AudioSources so both clips
            // can play simultaneously and StopAll can fade them in parallel.
            var t1 = GenerateAndTrackAsync(text, _audio, "A");
            var t2 = GenerateAndTrackAsync(text, _audioSecondary, "B");
            await UniTask.WhenAll(t1, t2);
        }

        private void HandleCancelGeneration()
        {
            if (_activeCts == null)
            {
                SetStatus("No active generation to cancel.");
                return;
            }

            try
            {
                _activeCts.Cancel();
                AppendHistory("Cancel requested on active generation.");
            }
            catch (Exception ex)
            {
                AppendHistory($"Cancel failed: {ex.Message}");
            }
        }

        private async void HandleStopLastWithFade()
        {
            if (_handles.Count == 0)
            {
                SetStatus("No active playback to stop.");
                return;
            }

            var handle = _handles[_handles.Count - 1];
            float fadeSec = GetFadeSeconds();
            AppendHistory(
                $"Stop last with fade={fadeSec:F2}s (handles before: {_handles.Count}).");
            await handle.StopAsync(fadeSec);
            UpdateActiveLabel();
        }

        private async void HandleStopAllWithFade()
        {
            if (_handles.Count == 0)
            {
                SetStatus("No active playback to stop.");
                return;
            }

            float fadeSec = GetFadeSeconds();
            AppendHistory(
                $"StopAll with fade={fadeSec:F2}s (handles before: {_handles.Count}).");

            var snapshot = _handles.ToArray();
            var tasks = new UniTask[snapshot.Length];
            for (int i = 0; i < snapshot.Length; i++)
                tasks[i] = snapshot[i].StopAsync(fadeSec);

            await UniTask.WhenAll(tasks);
            UpdateActiveLabel();
        }

        private void HandleStopLastInstant()
        {
            if (_handles.Count == 0)
            {
                SetStatus("No active playback to stop.");
                return;
            }

            var handle = _handles[_handles.Count - 1];
            AppendHistory(
                $"Stop last (instant) (handles before: {_handles.Count}).");
            handle.Stop();
            UpdateActiveLabel();
        }

        private void HandleStopAllInstant()
        {
            if (_handles.Count == 0)
            {
                SetStatus("No active playback to stop.");
                return;
            }

            AppendHistory(
                $"StopAll (instant) (handles before: {_handles.Count}).");

            // Snapshot to avoid mutation while iterating (handle.Stopped
            // event removes itself from _handles).
            var snapshot = _handles.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i].Stop();

            UpdateActiveLabel();
        }

        private void HandleBack() => _nav?.Back();

        // ── Core generation flow ──

        private async UniTask GenerateAndTrackAsync(string text, AudioSource source, string label)
        {
            // Reuse a single CTS for the active generation phase. If a
            // previous gen is still running, the new one shares it.
            _activeCts ??= new CancellationTokenSource();
            var ctsLocal = _activeCts;

            _isGenerating = true;
            UpdateButtons();
            SetStatus($"Generating '{label}'…");

            TtsPlaybackHandle handle = null;
            try
            {
                handle = await _service.GenerateAndPlayWithHandleAsync(
                    text, source, ctsLocal.Token);

                if (handle == null)
                {
                    SetStatus($"Handle '{label}' was null (gen failed).");
                    AppendHistory($"Handle '{label}': gen returned null.");
                    return;
                }

                Track(handle, label);
                SetStatus($"Playing '{label}'.");
                AppendHistory(
                    $"Handle '{label}' started ({handle.Result.DurationSeconds:F2}s).");
            }
            catch (OperationCanceledException)
            {
                SetStatus($"Generation '{label}' cancelled.");
                AppendHistory($"Handle '{label}': OperationCanceledException.");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                AppendHistory($"Handle '{label}': {ex.GetType().Name}: {ex.Message}");
                SherpaOnnxLog.RuntimeError(
                    $"[SherpaOnnx] TtsControlsPanel error: {ex}");
            }
            finally
            {
                _isGenerating = false;
                ResetCtsIfDone();
                UpdateButtons();
            }
        }

        private void Track(TtsPlaybackHandle handle, string label)
        {
            _handles.Add(handle);
            UpdateActiveLabel();
            // HandleWatcher subscribes in its ctor and unsubscribes
            // itself the first time Completed or Stopped fires — keeps
            // the (handle, label) pair captured without a closure.
            new HandleWatcher(this, handle, label);
        }

        // Notified by HandleWatcher exactly once per playback handle:
        // either "Completed naturally" or "Stopped explicitly".
        internal void OnHandleFinished(
            TtsPlaybackHandle handle, string label, string reason)
        {
            _handles.Remove(handle);
            AppendHistory(
                $"Handle '{label}' {reason}. (active: {_handles.Count})");
            UpdateActiveLabel();
        }

        // Per-handle subscriber so Track does not have to allocate a
        // closure. One instance per playback; lifetime is bounded by
        // the handle's Completed / Stopped events.
        private sealed class HandleWatcher
        {
            private readonly TtsControlsPanel _owner;
            private readonly TtsPlaybackHandle _handle;
            private readonly string _label;

            internal HandleWatcher(
                TtsControlsPanel owner,
                TtsPlaybackHandle handle,
                string label)
            {
                _owner = owner;
                _handle = handle;
                _label = label;
                _handle.Completed += HandleCompleted;
                _handle.Stopped += HandleStopped;
            }

            private void HandleCompleted() => Finish("Completed naturally");
            private void HandleStopped() => Finish("Stopped explicitly");

            private void Finish(string reason)
            {
                _handle.Completed -= HandleCompleted;
                _handle.Stopped -= HandleStopped;
                _owner.OnHandleFinished(_handle, _label, reason);
            }
        }

        private void CancelGeneration()
        {
            try { _activeCts?.Cancel(); } catch { /* ignore */ }
            _activeCts?.Dispose();
            _activeCts = null;
        }

        private void ResetCtsIfDone()
        {
            // If no other gens are pending using this CTS (only the just-finished
            // one), drop it so the next Generate gets a fresh source.
            if (_isGenerating)
                return;

            _activeCts?.Dispose();
            _activeCts = null;
        }

        // ── UI helpers ──

        private bool CheckReady()
        {
            if (_service != null && _service.IsReady)
                return true;

            SetStatus("Engine not loaded.");
            return false;
        }

        private string GetText() => _textField?.value ?? "";

        private float GetFadeSeconds()
        {
            int ms = _fadeMsField?.value ?? 500;
            if (ms < 0) ms = 0;
            return ms / 1000f;
        }

        private void UpdateButtons()
        {
            _btnGenerate?.SetEnabled(!_isGenerating);
            _btnGenerateTwo?.SetEnabled(!_isGenerating);
            _btnCancelGen?.SetEnabled(_isGenerating);
        }

        private void UpdateActiveLabel()
        {
            if (_activeLabel != null)
                _activeLabel.text = $"Active handles: {_handles.Count}";
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void HandleInitProgressChanged()
        {
            SetStatus(TtsSampleStatusUtil.BuildCurrent(_service));
        }

        private void AppendHistory(string line)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss.fff");
            _history.Add($"[{ts}] {line}");
            if (_history.Count > HistoryMaxLines)
                _history.RemoveAt(0);

            if (_historyLabel != null)
                _historyLabel.text = string.Join("\n", _history);
        }
    }
}
