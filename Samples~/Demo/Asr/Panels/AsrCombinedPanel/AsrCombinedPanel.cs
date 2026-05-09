using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Engine;
using UnityEngine;
using UnityEngine.UIElements;

namespace PonyuDev.SherpaOnnx.Samples
{
    /// <summary>
    /// Smoke-test panel for concurrent audio paths:
    /// looped music, one-shot SFX, TTS playback, and STT capture
    /// running at the same time. Verifies that the silent-AudioSource
    /// keep-alive in <see cref="MicrophoneSource"/> actually keeps the
    /// mic recording while other audio is mixed on iOS / Android.
    /// </summary>
    public sealed class AsrCombinedPanel : IDemoView
    {
        private const string DefaultTtsPhrase = "Hello, this is a concurrent audio test.";
        private const float SfxFrequencyHz = 880f;

        private IOnlineAsrService _asr;
        private MicrophoneSource _microphone;
        private TtsService _tts;
        private IDemoNavigator _nav;

        private GameObject _audioRoot;
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _ttsSource;
        private AudioClip _musicClip;
        private AudioClip _sfxClip;

        private Button _backButton;
        private Button _musicToggleButton;
        private Button _sfxButton;
        private Button _speakButton;
        private Button _sttToggleButton;
        private Label _statusLabel;
        private Label _partialLabel;
        private Label _levelLabel;
        private ScrollView _transcriptScroll;
        private TextField _ttsTextField;

        private bool _isRecording;
        private bool _ttsBusy;
        private bool _ttsReady;
        private CancellationTokenSource _ttsCts;

        // ── IDemoView ──

        public void Bind(VisualElement root, DemoServices services, IDemoNavigator nav)
        {
            _asr = services?.OnlineAsr;
            _microphone = services?.Microphone;
            _musicClip = services?.SampleClip; // Assets/Samples/ASR/Audio/0.wav from the navigator
            _nav = nav;

            _backButton = root.Q<Button>("backButton");
            _musicToggleButton = root.Q<Button>("musicToggleButton");
            _sfxButton = root.Q<Button>("sfxButton");
            _speakButton = root.Q<Button>("speakButton");
            _sttToggleButton = root.Q<Button>("sttToggleButton");
            _statusLabel = root.Q<Label>("statusLabel");
            _partialLabel = root.Q<Label>("partialLabel");
            _levelLabel = root.Q<Label>("levelLabel");
            _transcriptScroll = root.Q<ScrollView>("transcriptScroll");
            _ttsTextField = root.Q<TextField>("ttsTextField");
            if (_ttsTextField != null)
                _ttsTextField.value = DefaultTtsPhrase;

            if (_backButton != null) _backButton.clicked += HandleBack;
            if (_musicToggleButton != null) _musicToggleButton.clicked += HandleMusicToggle;
            if (_sfxButton != null) _sfxButton.clicked += HandlePlaySfx;
            if (_speakButton != null) _speakButton.clicked += HandleSpeak;
            if (_sttToggleButton != null) _sttToggleButton.clicked += HandleSttToggle;

            BuildAudio();
            SubscribeAsr();
            // ASR readiness shown in the status line; TTS readiness is handled
            // separately by EnsureTtsReadyAsync below (toggles the Speak button).
            AsrInitProgressBus.Changed += HandleInitProgressChanged;
            HandleInitProgressChanged();

            // Eagerly load + warm up TTS so the first Speak click is fast.
            // Cold init is the real "frozen" cost — gating Speak until the
            // engine is ready is friendlier than hanging on first click.
            if (_speakButton != null)
            {
                _speakButton.SetEnabled(false);
                _speakButton.text = "Loading TTS...";
            }
            _ttsCts = new CancellationTokenSource();
            EnsureTtsReadyAsync(_ttsCts.Token).Forget();
        }

        public void Unbind()
        {
            AsrInitProgressBus.Changed -= HandleInitProgressChanged;
            StopRecordingIfActive();
            UnsubscribeAsr();

            _ttsCts?.Cancel();
            _ttsCts?.Dispose();
            _ttsCts = null;

            _ttsReady = false;
            _tts?.Dispose();
            _tts = null;

            DestroyAudio();

            if (_backButton != null) _backButton.clicked -= HandleBack;
            if (_musicToggleButton != null) _musicToggleButton.clicked -= HandleMusicToggle;
            if (_sfxButton != null) _sfxButton.clicked -= HandlePlaySfx;
            if (_speakButton != null) _speakButton.clicked -= HandleSpeak;
            if (_sttToggleButton != null) _sttToggleButton.clicked -= HandleSttToggle;

            _backButton = null;
            _musicToggleButton = null;
            _sfxButton = null;
            _speakButton = null;
            _sttToggleButton = null;
            _statusLabel = null;
            _partialLabel = null;
            _levelLabel = null;
            _transcriptScroll = null;
            _ttsTextField = null;

            _asr = null;
            _microphone = null;
            _nav = null;
        }

        // ── Audio setup ──

        private void BuildAudio()
        {
            _audioRoot = new GameObject("[SherpaOnnx] CombinedSampleAudio");
            _audioRoot.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

            _musicSource = _audioRoot.AddComponent<AudioSource>();
            _sfxSource = _audioRoot.AddComponent<AudioSource>();
            _ttsSource = _audioRoot.AddComponent<AudioSource>();

            _sfxClip = MakeSfxClip("CombinedSfxTone", SfxFrequencyHz, 0.18f);

            // Music clip is the same WAV the navigator passes to AsrFilePanel
            // for offline recognition. It is an asset, not a runtime clip,
            // so DestroyAudio must not Destroy() it.
            _musicSource.clip = _musicClip;
            _musicSource.loop = true;
            _musicSource.volume = 0.5f;

            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.4f;

            if (_musicClip == null && _musicToggleButton != null)
            {
                _musicToggleButton.SetEnabled(false);
                _musicToggleButton.text = "Music clip missing";
            }
        }

        private void DestroyAudio()
        {
            if (_musicSource != null)
                _musicSource.Stop();

            if (_audioRoot != null)
            {
                UnityEngine.Object.Destroy(_audioRoot);
                _audioRoot = null;
            }

            _musicSource = null;
            _sfxSource = null;
            _ttsSource = null;

            // _musicClip is an asset reference (sampleClip from navigator),
            // do not Destroy it. Just drop the reference.
            _musicClip = null;

            if (_sfxClip != null)
            {
                UnityEngine.Object.Destroy(_sfxClip);
                _sfxClip = null;
            }
        }

        // One-shot sine with quick fade-in / fade-out so it does not click.
        private static AudioClip MakeSfxClip(string name, float freqHz, float lengthSec, int sampleRate = 44100)
        {
            int totalSamples = Mathf.Max(1, Mathf.CeilToInt(lengthSec * sampleRate));
            int fadeSamples = Mathf.Min(totalSamples / 4, sampleRate / 50); // ~20 ms
            var data = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float env = 1f;
                if (i < fadeSamples)
                    env = i / (float)fadeSamples;
                else if (i > totalSamples - fadeSamples)
                    env = (totalSamples - i) / (float)fadeSamples;
                data[i] = 0.5f * env * Mathf.Sin(2f * Mathf.PI * freqHz * i / sampleRate);
            }

            var clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── Music / SFX ──

        private void HandleMusicToggle()
        {
            if (_musicSource == null) return;

            if (_musicSource.isPlaying)
            {
                _musicSource.Stop();
                _musicToggleButton.text = "Play Music Loop";
                SetStatus("Music stopped.");
            }
            else
            {
                _musicSource.Play();
                _musicToggleButton.text = "Stop Music Loop";
                SetStatus("Music playing.");
            }
        }

        private void HandlePlaySfx()
        {
            if (_sfxSource == null || _sfxClip == null) return;
            _sfxSource.PlayOneShot(_sfxClip);
        }

        // ── TTS ──

        // Load TTS so the Speak button can be enabled when the engine is ready.
        // Plain async usage — same pattern any consumer would write.
        private async UniTaskVoid EnsureTtsReadyAsync(CancellationToken ct)
        {
            try
            {
                _tts = new TtsService();
                await _tts.InitializeAsync(ct: ct);

                if (ct.IsCancellationRequested) return;

                if (!_tts.IsReady)
                {
                    SetStatus("TTS engine failed to load.");
                    return;
                }

                _ttsReady = true;
                if (_speakButton != null)
                {
                    _speakButton.SetEnabled(true);
                    _speakButton.text = "Speak";
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] AsrCombinedPanel TTS init: {ex}");
                SetStatus($"TTS init error: {ex.Message}");
            }
        }

        private async void HandleSpeak()
        {
            if (_ttsBusy) return;
            if (!_ttsReady || _tts == null || !_tts.IsReady)
            {
                SetStatus("TTS still loading, please wait.");
                return;
            }
            if (_ttsSource == null)
            {
                SetStatus("TTS audio source missing.");
                return;
            }

            _ttsBusy = true;
            _speakButton?.SetEnabled(false);
            try
            {
                string phrase = string.IsNullOrWhiteSpace(_ttsTextField?.value)
                    ? DefaultTtsPhrase
                    : _ttsTextField.value;

                SetStatus("Speaking...");
                await _tts.GenerateAndPlayAsync(phrase, _ttsSource, TtsPlaybackMode.Overlap);
                SetStatus("TTS finished.");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SetStatus($"TTS error: {ex.Message}");
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] AsrCombinedPanel TTS: {ex}");
            }
            finally
            {
                _ttsBusy = false;
                _speakButton?.SetEnabled(true);
            }
        }

        // ── STT ──

        private async void HandleSttToggle()
        {
            if (_isRecording)
            {
                StopRecordingIfActive();
                return;
            }

            if (_asr == null || !_asr.IsReady)
            {
                SetStatus("Online ASR engine not loaded.");
                return;
            }

            if (_microphone == null)
            {
                SetStatus("No microphone available.");
                return;
            }

            _sttToggleButton?.SetEnabled(false);
            SetStatus("Starting microphone...");
            try
            {
                _asr.StartSession();
                _microphone.SamplesAvailable += HandleMicSamples;
                bool started = await _microphone.StartRecordingAsync();
                if (!started)
                {
                    _microphone.SamplesAvailable -= HandleMicSamples;
                    _asr.StopSession();
                    SetStatus("Microphone failed to start.");
                    return;
                }

                _isRecording = true;
                _sttToggleButton.text = "Stop STT";
                SetStatus("Listening — speak now.");
            }
            catch (Exception ex)
            {
                SetStatus($"STT error: {ex.Message}");
                SherpaOnnxLog.RuntimeError($"[SherpaOnnx] AsrCombinedPanel STT: {ex}");
            }
            finally
            {
                _sttToggleButton?.SetEnabled(true);
            }
        }

        private void HandleMicSamples(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return;

            UpdateLevel(samples);

            if (_asr == null || !_asr.IsSessionActive)
                return;

            _asr.AcceptSamples(samples, _microphone.SampleRate);
            _asr.ProcessAvailableFrames();
        }

        private void HandlePartial(OnlineAsrResult result)
        {
            if (_partialLabel != null)
                _partialLabel.text = result.Text;
        }

        private void HandleFinal(OnlineAsrResult result)
        {
            if (_partialLabel != null)
                _partialLabel.text = string.Empty;
            AppendTranscript(result.Text);
        }

        private void HandleEndpoint()
        {
            _asr?.ResetStream();
        }

        // ── Helpers ──

        private void HandleBack()
        {
            _nav?.Back();
        }

        private void StopRecordingIfActive()
        {
            if (!_isRecording) return;

            if (_microphone != null)
            {
                _microphone.SamplesAvailable -= HandleMicSamples;
                _microphone.StopRecording();
            }
            _asr?.StopSession();

            _isRecording = false;
            if (_sttToggleButton != null)
                _sttToggleButton.text = "Start STT";
            SetStatus("STT stopped.");
            if (_levelLabel != null)
                _levelLabel.text = "Mic level: —";
        }

        private void SubscribeAsr()
        {
            if (_asr == null) return;
            _asr.PartialResultReady += HandlePartial;
            _asr.FinalResultReady += HandleFinal;
            _asr.EndpointDetected += HandleEndpoint;
        }

        private void UnsubscribeAsr()
        {
            if (_asr == null) return;
            _asr.PartialResultReady -= HandlePartial;
            _asr.FinalResultReady -= HandleFinal;
            _asr.EndpointDetected -= HandleEndpoint;
        }

        private void UpdateLevel(float[] samples)
        {
            float maxAbs = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float abs = samples[i] < 0f ? -samples[i] : samples[i];
                if (abs > maxAbs) maxAbs = abs;
            }
            if (_levelLabel != null)
                _levelLabel.text = $"Mic level: {maxAbs:F3}";
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.text = text;
        }

        private void HandleInitProgressChanged()
        {
            SetStatus(AsrSampleStatusUtil.BuildOnlineCurrent(_asr));
        }

        private void AppendTranscript(string text)
        {
            if (_transcriptScroll == null || string.IsNullOrWhiteSpace(text))
                return;
            var line = new Label(text);
            line.AddToClassList("stream-transcript-line");
            _transcriptScroll.contentContainer.Add(line);
        }
    }
}
