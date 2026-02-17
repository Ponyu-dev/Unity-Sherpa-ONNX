# ASR Runtime Usage Guide

This guide covers how to use speech recognition at runtime in your Unity project.
For model configuration and import, see [ASR Models Setup](asr-models-setup.md).

## Architecture Overview

The ASR system provides two modes — **offline** (file recognition) and **online** (streaming):

```
IAsrService (offline)
    |
    +-- AsrService (POCO, no MonoBehaviour)
            |
            +-- AsrEngine (native OfflineRecognizer pool)

IOnlineAsrService (streaming)
    |
    +-- OnlineAsrService (POCO, no MonoBehaviour)
            |
            +-- OnlineAsrEngine (native OnlineRecognizer)

MicrophoneSource (POCO, microphone capture with circular buffer)
```

| Approach | When to use |
|----------|-------------|
| `AsrOrchestrator` / `OnlineAsrOrchestrator` (MonoBehaviour) | Prototyping, no DI framework |
| `AsrService` / `OnlineAsrService` + VContainer | Production with VContainer DI |
| `AsrService` / `OnlineAsrService` + Zenject | Production with Zenject DI |
| `AsrService` / `OnlineAsrService` manual | Custom lifecycle management |

---

## Quick Start — Offline (MonoBehaviour)

### Using AsrOrchestrator

The simplest way to recognize speech from an AudioClip. Add `AsrOrchestrator` to any GameObject:

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Offline;

public class AsrExample : MonoBehaviour
{
    [SerializeField] private AsrOrchestrator _orchestrator;
    [SerializeField] private AudioClip _clip;

    private void Start()
    {
        if (_orchestrator.IsInitialized)
            Recognize();
        else
            _orchestrator.Initialized += Recognize;
    }

    private async void Recognize()
    {
        // Sync: extracts PCM and recognizes on the calling thread
        AsrResult result = _orchestrator.RecognizeFromClip(_clip);
        Debug.Log($"Recognized: {result?.Text}");

        // Async: extraction on main thread, recognition on background
        AsrResult asyncResult = await _orchestrator.RecognizeFromClipAsync(_clip);
        Debug.Log($"Async: {asyncResult?.Text}");
    }

    private void OnDestroy()
    {
        _orchestrator.Initialized -= Recognize;
    }
}
```

`AsrOrchestrator` initializes asynchronously on `Awake`. On Android, this includes
extracting model files from APK to `persistentDataPath`. Use `IsInitialized` or the
`Initialized` event to wait for completion.

### Manual AsrService (no Orchestrator)

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;

public class ManualAsrExample : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;

    private IAsrService _asr;

    private async void Awake()
    {
        _asr = new AsrService();
        await _asr.InitializeAsync();
    }

    public async void Recognize()
    {
        if (_asr == null || !_asr.IsReady || _clip == null)
            return;

        float[] samples = new float[_clip.samples * _clip.channels];
        _clip.GetData(samples, 0);

        AsrResult result = await _asr.RecognizeAsync(samples, _clip.frequency);
        Debug.Log($"Result: {result?.Text}");
    }

    private void OnDestroy()
    {
        _asr?.Dispose();
    }
}
```

---

## Quick Start — Online / Streaming (MonoBehaviour)

### Using OnlineAsrOrchestrator

Real-time speech recognition with microphone. Add `OnlineAsrOrchestrator` to a GameObject:

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;

public class StreamingAsrExample : MonoBehaviour
{
    [SerializeField] private OnlineAsrOrchestrator _orchestrator;

    private MicrophoneSource _mic;

    private void Start()
    {
        if (_orchestrator.IsInitialized)
            SetupMicrophone();
        else
            _orchestrator.Initialized += SetupMicrophone;

        _orchestrator.PartialResultReady += OnPartial;
        _orchestrator.FinalResultReady += OnFinal;
        _orchestrator.EndpointDetected += OnEndpoint;
    }

    private async void SetupMicrophone()
    {
        _mic = new MicrophoneSource(sampleRate: 16000);
        bool started = await _mic.StartRecordingAsync();

        if (started)
            _orchestrator.ConnectMicrophone(_mic);
    }

    public void StopRecording()
    {
        _orchestrator.DisconnectMicrophone();
        _mic?.StopRecording();
    }

    private void OnPartial(OnlineAsrResult result)
    {
        Debug.Log($"Partial: {result.Text}");
    }

    private void OnFinal(OnlineAsrResult result)
    {
        Debug.Log($"Final: {result.Text}");
    }

    private void OnEndpoint()
    {
        Debug.Log("Endpoint detected — stream reset.");
    }

    private void OnDestroy()
    {
        _orchestrator.Initialized -= SetupMicrophone;
        _orchestrator.PartialResultReady -= OnPartial;
        _orchestrator.FinalResultReady -= OnFinal;
        _orchestrator.EndpointDetected -= OnEndpoint;

        _orchestrator.DisconnectMicrophone();
        _mic?.Dispose();
    }
}
```

`OnlineAsrOrchestrator` automatically:
- Subscribes to `MicrophoneSource.SamplesAvailable` via `ConnectMicrophone()`
- Feeds audio to the engine and calls `ProcessAvailableFrames()` each frame
- Forwards `PartialResultReady`, `FinalResultReady`, `EndpointDetected` events
- Resets the stream on endpoint detection when `_autoResetOnEndpoint` is enabled (default)

### Manual OnlineAsrService (no Orchestrator)

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;

public class ManualStreamingExample : MonoBehaviour
{
    private IOnlineAsrService _asr;
    private MicrophoneSource _mic;

    private async void Awake()
    {
        _asr = new OnlineAsrService();
        await _asr.InitializeAsync();

        _asr.PartialResultReady += OnPartial;
        _asr.FinalResultReady += OnFinal;
        _asr.EndpointDetected += OnEndpoint;

        _mic = new MicrophoneSource(sampleRate: 16000);
        bool started = await _mic.StartRecordingAsync();

        if (started)
        {
            _mic.SamplesAvailable += OnMicSamples;
            _asr.StartSession();
        }
    }

    private void OnMicSamples(float[] samples)
    {
        if (!_asr.IsSessionActive)
            return;

        _asr.AcceptSamples(samples, _mic.SampleRate);
        _asr.ProcessAvailableFrames();
    }

    private void OnPartial(OnlineAsrResult r) => Debug.Log($"Partial: {r.Text}");
    private void OnFinal(OnlineAsrResult r) => Debug.Log($"Final: {r.Text}");

    private void OnEndpoint()
    {
        _asr.ResetStream();
    }

    private void OnDestroy()
    {
        _mic.SamplesAvailable -= OnMicSamples;
        _asr.PartialResultReady -= OnPartial;
        _asr.FinalResultReady -= OnFinal;
        _asr.EndpointDetected -= OnEndpoint;

        _mic?.Dispose();
        _asr?.Dispose();
    }
}
```

---

## Recognition Examples

### Switching Profiles

```csharp
// By name
_asr.SwitchProfile("sherpa-onnx-zipformer-small-en-2023-06-26");

// By index
_asr.SwitchProfile(0);

// Recognize with new profile
AsrResult result = _asr.Recognize(samples, sampleRate);
```

### Engine Pool Size (offline only)

Multiple native recognizer instances allow concurrent recognition:

```csharp
_asr.EnginePoolSize = 4; // up to 4 parallel recognitions
```

### AudioResampler

If your audio sample rate differs from the model's expected rate (usually 16 kHz):

```csharp
using PonyuDev.SherpaOnnx.Common.Audio;

// Resample mono audio
float[] resampled = AudioResampler.Resample(samples, fromRate: 44100, toRate: 16000);

// Downmix stereo to mono + resample
float[] mono16k = AudioResampler.ResampleMono(
    stereoSamples, channels: 2, fromRate: 48000, toRate: 16000);
```

---

## VContainer Integration

### Installer

```csharp
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common.Audio;
using VContainer;
using VContainer.Unity;

public class AsrLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Offline ASR
        builder.Register<AsrService>(Lifetime.Singleton)
            .As<IAsrService>();

        // Online (streaming) ASR
        builder.Register<OnlineAsrService>(Lifetime.Singleton)
            .As<IOnlineAsrService>();

        // Microphone (shared instance)
        builder.Register<MicrophoneSource>(Lifetime.Singleton)
            .WithParameter("sampleRate", 16000);

        // Async initialization
        builder.RegisterEntryPoint<AsrInitializer>();
    }
}
```

### Async Initialization

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using VContainer.Unity;

public class AsrInitializer : IAsyncStartable
{
    private readonly IAsrService _offline;
    private readonly IOnlineAsrService _online;

    public AsrInitializer(IAsrService offline, IOnlineAsrService online)
    {
        _offline = offline;
        _online = online;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _offline.InitializeAsync(ct: ct);
        await _online.InitializeAsync(ct: ct);
    }
}
```

### Presenter Example

```csharp
using System;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using VContainer;
using VContainer.Unity;

public class DictationPresenter : IStartable, IDisposable
{
    private readonly IAsrService _asr;

    [Inject]
    public DictationPresenter(IAsrService asr)
    {
        _asr = asr;
    }

    public void Start() { }

    public async void RecognizeClip(float[] samples, int sampleRate)
    {
        AsrResult result = await _asr.RecognizeAsync(samples, sampleRate);
        if (result != null && result.IsValid)
            OnRecognized(result.Text);
    }

    private void OnRecognized(string text)
    {
        // Update UI or model
    }

    public void Dispose()
    {
        // IAsrService lifetime is managed by the container
    }
}
```

### Streaming Presenter Example

```csharp
using System;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;
using VContainer;
using VContainer.Unity;

public class LiveCaptionPresenter : IStartable, IDisposable
{
    private readonly IOnlineAsrService _asr;
    private readonly MicrophoneSource _mic;

    [Inject]
    public LiveCaptionPresenter(
        IOnlineAsrService asr,
        MicrophoneSource mic)
    {
        _asr = asr;
        _mic = mic;
    }

    public void Start()
    {
        _asr.PartialResultReady += OnPartial;
        _asr.FinalResultReady += OnFinal;
        _asr.EndpointDetected += OnEndpoint;
    }

    public async void StartCapture()
    {
        bool ok = await _mic.StartRecordingAsync();
        if (!ok) return;

        _mic.SamplesAvailable += OnMicSamples;
        _asr.StartSession();
    }

    public void StopCapture()
    {
        _mic.SamplesAvailable -= OnMicSamples;
        _mic.StopRecording();
        _asr.StopSession();
    }

    private void OnMicSamples(float[] samples)
    {
        if (!_asr.IsSessionActive) return;
        _asr.AcceptSamples(samples, _mic.SampleRate);
        _asr.ProcessAvailableFrames();
    }

    private void OnPartial(OnlineAsrResult r)
    {
        // Update partial caption UI
    }

    private void OnFinal(OnlineAsrResult r)
    {
        // Append final text to transcript
    }

    private void OnEndpoint()
    {
        _asr.ResetStream();
    }

    public void Dispose()
    {
        _asr.PartialResultReady -= OnPartial;
        _asr.FinalResultReady -= OnFinal;
        _asr.EndpointDetected -= OnEndpoint;
        StopCapture();
    }
}
```

---

## Zenject Integration

### Installer

```csharp
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Common.Audio;
using Zenject;

public class AsrInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Offline ASR
        Container.Bind<IAsrService>()
            .To<AsrService>()
            .AsSingle();

        // Online (streaming) ASR
        Container.Bind<IOnlineAsrService>()
            .To<OnlineAsrService>()
            .AsSingle();

        // Microphone
        Container.Bind<MicrophoneSource>()
            .AsSingle()
            .WithArguments(16000); // sampleRate

        // Initialization
        Container.BindInterfacesTo<AsrInitializer>()
            .AsSingle();
    }
}
```

### Initializer

```csharp
using System;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Online;
using Zenject;

public class AsrInitializer : IInitializable, IDisposable
{
    private readonly IAsrService _offline;
    private readonly IOnlineAsrService _online;

    public AsrInitializer(IAsrService offline, IOnlineAsrService online)
    {
        _offline = offline;
        _online = online;
    }

    public async void Initialize()
    {
        await _offline.InitializeAsync();
        await _online.InitializeAsync();
    }

    public void Dispose()
    {
        _offline.Dispose();
        _online.Dispose();
    }
}
```

### Usage with [Inject]

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Asr.Online;
using PonyuDev.SherpaOnnx.Asr.Online.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;
using Zenject;

public class SubtitleController : MonoBehaviour
{
    [Inject] private IOnlineAsrService _asr;
    [Inject] private MicrophoneSource _mic;

    private void OnEnable()
    {
        _asr.PartialResultReady += OnPartial;
        _asr.FinalResultReady += OnFinal;
        _asr.EndpointDetected += OnEndpoint;
    }

    public async void StartListening()
    {
        bool ok = await _mic.StartRecordingAsync();
        if (!ok) return;

        _mic.SamplesAvailable += OnMicSamples;
        _asr.StartSession();
    }

    public void StopListening()
    {
        _mic.SamplesAvailable -= OnMicSamples;
        _mic.StopRecording();
        _asr.StopSession();
    }

    private void OnMicSamples(float[] samples)
    {
        if (!_asr.IsSessionActive) return;
        _asr.AcceptSamples(samples, _mic.SampleRate);
        _asr.ProcessAvailableFrames();
    }

    private void OnPartial(OnlineAsrResult r) { /* update subtitle */ }
    private void OnFinal(OnlineAsrResult r) { /* finalize subtitle */ }
    private void OnEndpoint() { _asr.ResetStream(); }

    private void OnDisable()
    {
        _asr.PartialResultReady -= OnPartial;
        _asr.FinalResultReady -= OnFinal;
        _asr.EndpointDetected -= OnEndpoint;
        StopListening();
    }
}
```

---

## Android Notes

### Automatic File Extraction

On Android, `StreamingAssets` files live inside the APK archive and are not
accessible via `System.IO.File`. The package handles this automatically:

1. At build time, a manifest of all SherpaOnnx files is generated
2. On first launch, files are extracted from APK to `persistentDataPath`
3. Subsequent launches skip extraction (version marker check)

**You must use `InitializeAsync()` on Android.** The synchronous `Initialize()`
cannot extract files from the APK.

### Microphone Permission

`MicrophoneSource` requests microphone permission automatically on Android
(configurable via constructor parameter `requestPermission`). If permission is
denied, `StartRecordingAsync()` returns `false`.

### Progress Tracking

Monitor extraction progress on Android (useful for loading screens):

```csharp
var progress = new Progress<float>(p =>
    Debug.Log($"Extracting: {p:P0}"));

await _asr.InitializeAsync(progress);
```

---

## API Reference

### IAsrService (offline)

| Category | Method | Description |
|----------|--------|-------------|
| **Lifecycle** | `Initialize()` | Sync init (Desktop only) |
| | `InitializeAsync(progress, ct)` | Async init (all platforms, required on Android) |
| | `LoadProfile(profile)` | Load a specific profile |
| | `SwitchProfile(index)` | Switch by index |
| | `SwitchProfile(name)` | Switch by name |
| **Properties** | `IsReady` | `true` when engine is loaded |
| | `ActiveProfile` | Current `AsrProfile` |
| | `Settings` | All loaded `AsrSettingsData` |
| | `EnginePoolSize` | Get/set concurrent native instances |
| **Recognition** | `Recognize(samples, sampleRate)` | Sync recognition, returns `AsrResult` |
| | `RecognizeAsync(samples, sampleRate)` | Background thread recognition |

### IOnlineAsrService (streaming)

| Category | Method | Description |
|----------|--------|-------------|
| **Lifecycle** | `Initialize()` | Sync init (Desktop only) |
| | `InitializeAsync(progress, ct)` | Async init (all platforms, required on Android) |
| | `LoadProfile(profile)` | Load a specific profile |
| | `SwitchProfile(index)` | Switch by index |
| | `SwitchProfile(name)` | Switch by name |
| **Properties** | `IsReady` | `true` when engine is loaded |
| | `IsSessionActive` | `true` during active recognition session |
| | `ActiveProfile` | Current `OnlineAsrProfile` |
| | `Settings` | All loaded `OnlineAsrSettingsData` |
| **Session** | `StartSession()` | Begin streaming recognition |
| | `StopSession()` | End streaming recognition |
| **Audio** | `AcceptSamples(samples, sampleRate)` | Feed PCM audio samples |
| | `ProcessAvailableFrames()` | Process buffered audio, fire result events |
| | `ResetStream()` | Reset decoder state (call after endpoint) |
| **Events** | `PartialResultReady` | Fires with intermediate recognition text |
| | `FinalResultReady` | Fires with final text when endpoint detected |
| | `EndpointDetected` | Fires when a speech endpoint is detected |

### AsrResult (offline)

| Member | Type | Description |
|--------|------|-------------|
| `Text` | `string` | Recognized text |
| `Tokens` | `string[]` | Individual tokens (may be null) |
| `Timestamps` | `float[]` | Per-token timestamps in seconds (may be null) |
| `Durations` | `float[]` | Per-token durations in seconds (may be null) |
| `IsValid` | `bool` | `true` when Text is not empty |

### OnlineAsrResult (streaming)

| Member | Type | Description |
|--------|------|-------------|
| `Text` | `string` | Recognized text so far |
| `Tokens` | `string[]` | Individual tokens (may be null) |
| `Timestamps` | `float[]` | Per-token timestamps in seconds (may be null) |
| `IsFinal` | `bool` | `true` when endpoint was detected |
| `IsValid` | `bool` | `true` when Text is not empty |

### MicrophoneSource

| Category | Member | Description |
|----------|--------|-------------|
| **Properties** | `IsRecording` | `true` while capturing |
| | `DeviceName` | Microphone device name (null = default) |
| | `SampleRate` | Capture sample rate (default 16000) |
| **Methods** | `StartRecordingAsync(ct)` | Request permission and start capture |
| | `StopRecording()` | Stop capture |
| | `ReadNewSamples()` | Pull model: get new samples since last call |
| | `ReadAllSamples()` | Get entire circular buffer |
| | `Dispose()` | Stop and release resources |
| **Events** | `SamplesAvailable` | Push model: fires each frame with new PCM samples |
| | `RecordingStopped` | Fires when recording stops |

### AsrOrchestrator (MonoBehaviour)

| Member | Description |
|--------|-------------|
| `Service` | `IAsrService` — the underlying service |
| `IsInitialized` | `true` after async initialization completes |
| `Initialized` | Event: fires once after initialization |
| `RecognizeFromClip(clip)` | Extract PCM from AudioClip and recognize (sync) |
| `RecognizeFromClipAsync(clip)` | Same but recognition runs on background thread |

### OnlineAsrOrchestrator (MonoBehaviour)

| Member | Description |
|--------|-------------|
| `Service` | `IOnlineAsrService` — the underlying service |
| `IsInitialized` | `true` after async initialization completes |
| `Initialized` | Event: fires once after initialization |
| `ConnectMicrophone(mic)` | Wire MicrophoneSource to the streaming pipeline |
| `DisconnectMicrophone()` | Disconnect and stop forwarding audio |
| `PartialResultReady` | Forwarded from the service |
| `FinalResultReady` | Forwarded from the service |
| `EndpointDetected` | Forwarded from the service |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `AsrService is not initialized` | Call `Initialize()` or `InitializeAsync()` before `Recognize()` |
| `No active profile found` | Set an active profile in Project Settings > Sherpa-ONNX > ASR |
| SIGSEGV crash on Android | Ensure you use `InitializeAsync()`, not `Initialize()` |
| `StartRecordingAsync` returns false | Microphone permission denied or no devices found |
| No partial results appear | Ensure `StartSession()` is called and `SamplesAvailable` is wired |
| Endpoint never fires | Check endpoint detection settings in the online profile |
| `Recognize()` returns null | Check logs for engine errors; verify model files exist |
| Audio sounds too slow/fast | Use `AudioResampler` to match the model's expected sample rate |
