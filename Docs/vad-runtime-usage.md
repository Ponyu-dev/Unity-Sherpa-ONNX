# VAD Runtime Usage Guide

This guide covers how to use Voice Activity Detection at runtime in your Unity project.
For model import and configuration, see [VAD Models Setup](vad-models-setup.md).

## Architecture Overview

The VAD system detects speech in real-time audio. It can be used standalone
or combined with ASR for efficient speech recognition.

```
IVadService
    |
    +-- VadService (POCO, no MonoBehaviour)
            |
            +-- VadEngine (native VoiceActivityDetector)

VadAsrPipeline (combines IVadService + IAsrService)
    |
    +-- Ring buffer → VAD → speech segments → ASR
```

| Approach | When to use |
|----------|-------------|
| `VadService` standalone | Detect speech/silence boundaries |
| `VadAsrPipeline` | Run ASR only on speech segments (saves resources) |
| `VadService` + VContainer | Production with DI framework |
| `VadService` + Zenject | Production with Zenject DI |

---

## Quick Start — Standalone VAD

### Manual VadService

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Vad;
using PonyuDev.SherpaOnnx.Vad.Engine;

public class VadExample : MonoBehaviour
{
    private IVadService _vad;

    private async void Awake()
    {
        _vad = new VadService();
        await _vad.InitializeAsync();

        _vad.OnSpeechStart += HandleSpeechStart;
        _vad.OnSpeechEnd += HandleSpeechEnd;
        _vad.OnSegment += HandleSegment;
    }

    /// <summary>
    /// Call this each frame with microphone samples.
    /// Samples are fed in WindowSize chunks internally.
    /// </summary>
    public void FeedAudio(float[] samples)
    {
        if (!_vad.IsReady) return;

        _vad.AcceptWaveform(samples);
    }

    private void HandleSpeechStart()
    {
        Debug.Log("Speech started");
    }

    private void HandleSpeechEnd()
    {
        Debug.Log("Speech ended");
    }

    private void HandleSegment(VadSegment segment)
    {
        Debug.Log($"Segment: {segment.Duration:F2}s, " +
                  $"{segment.Samples.Length} samples");
    }

    private void OnDestroy()
    {
        if (_vad != null)
        {
            _vad.OnSpeechStart -= HandleSpeechStart;
            _vad.OnSpeechEnd -= HandleSpeechEnd;
            _vad.OnSegment -= HandleSegment;
        }

        _vad?.Dispose();
    }
}
```

---

## Quick Start — VAD + ASR Pipeline

### VadAsrPipeline

The pipeline combines VAD and ASR: VAD filters silence, and only speech
segments are sent to the recognizer. This saves resources compared to
running ASR on the entire audio stream.

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Vad;

public class VadAsrExample : MonoBehaviour
{
    private IVadService _vad;
    private IAsrService _asr;
    private VadAsrPipeline _pipeline;

    private async void Awake()
    {
        _vad = new VadService();
        _asr = new AsrService();

        await _vad.InitializeAsync();
        await _asr.InitializeAsync();

        _pipeline = new VadAsrPipeline(_vad, _asr);
        _pipeline.OnSpeechStart += HandleSpeechStart;
        _pipeline.OnSpeechEnd += HandleSpeechEnd;
        _pipeline.OnResult += HandleResult;
    }

    /// <summary>
    /// Feed arbitrary-length audio. The pipeline handles
    /// ring buffering and window-sized chunking internally.
    /// </summary>
    public void FeedAudio(float[] samples)
    {
        _pipeline?.AcceptSamples(samples);
    }

    /// <summary>Call when recording stops to flush pending speech.</summary>
    public void StopRecording()
    {
        _pipeline?.Flush();
    }

    private void HandleSpeechStart()
    {
        Debug.Log("Speech started");
    }

    private void HandleSpeechEnd()
    {
        Debug.Log("Speech ended");
    }

    private void HandleResult(AsrResult result)
    {
        Debug.Log($"Recognized: {result.Text}");
    }

    private void OnDestroy()
    {
        if (_pipeline != null)
        {
            _pipeline.OnSpeechStart -= HandleSpeechStart;
            _pipeline.OnSpeechEnd -= HandleSpeechEnd;
            _pipeline.OnResult -= HandleResult;
        }

        _pipeline?.Dispose();
        _asr?.Dispose();
        _vad?.Dispose();
    }
}
```

### Pipeline with MicrophoneSource

Full example: microphone capture → VAD → ASR.

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Vad;

public class MicVadAsrExample : MonoBehaviour
{
    private IVadService _vad;
    private IAsrService _asr;
    private VadAsrPipeline _pipeline;
    private MicrophoneSource _mic;

    private async void Awake()
    {
        _vad = new VadService();
        _asr = new AsrService();

        await _vad.InitializeAsync();
        await _asr.InitializeAsync();

        _pipeline = new VadAsrPipeline(_vad, _asr);
        _pipeline.OnResult += HandleResult;
        _pipeline.OnSpeechStart += HandleSpeechStart;
        _pipeline.OnSpeechEnd += HandleSpeechEnd;

        var micSettings = await MicrophoneSettingsLoader.LoadAsync();
        _mic = new MicrophoneSource(micSettings);
        _mic.SamplesAvailable += HandleMicSamples;

        bool started = await _mic.StartRecordingAsync();
        if (!started)
            Debug.LogError("Microphone unavailable");
    }

    private void HandleMicSamples(float[] samples)
    {
        _pipeline?.AcceptSamples(samples);
    }

    private void HandleSpeechStart()
    {
        Debug.Log("Speech started");
    }

    private void HandleSpeechEnd()
    {
        Debug.Log("Speech ended");
    }

    private void HandleResult(AsrResult result)
    {
        Debug.Log($"Recognized: {result.Text}");
    }

    private void OnDestroy()
    {
        if (_pipeline != null)
        {
            _pipeline.OnSpeechStart -= HandleSpeechStart;
            _pipeline.OnSpeechEnd -= HandleSpeechEnd;
            _pipeline.OnResult -= HandleResult;
        }

        if (_mic != null)
            _mic.SamplesAvailable -= HandleMicSamples;

        _mic?.Dispose();
        _pipeline?.Dispose();
        _asr?.Dispose();
        _vad?.Dispose();
    }
}
```

---

## Switching Profiles

```csharp
// By name
_vad.SwitchProfile("silero_vad");

// By index
_vad.SwitchProfile(0);
```

## Disk Usage (extracted profile directories)

On Android every active profile lands in
`Application.persistentDataPath/SherpaOnnx/vad-models/{profileName}/`
regardless of `ModelSource`:

| Source   | How it gets there |
|----------|-------------------|
| `Local`  | Lazy per-profile extraction from APK on first `LoadProfile` (only that profile's files). |
| `Remote` | Editor-time import puts files into StreamingAssets; on Android they extract the same way as `Local`. |
| `LocalZip` | Per-profile zip is unpacked the first time it is loaded. |

Old extractions stay on disk after `SwitchProfile` so a re-switch does
not pay the re-extract cost. `IVadService` implements `IModelDiskUsage`
— host code can inspect and free that space without knowing about
`LocalZipExtractor` or path constants.

```csharp
// What is on disk
foreach (var name in vad.GetExtractedProfiles())
    Debug.Log($"{name}: {vad.GetExtractedProfileSizeBytes(name) / (1024 * 1024)} MB");

// Delete one stale profile
vad.TryDeleteExtractedProfile("old-silero");

// Or sweep everything that is no longer in vad-settings.json
int removed = vad.CleanupUnusedExtractedProfiles();
```

`GetExtractedProfiles()` returns every profile that has an extraction
marker on disk regardless of source (LocalZip's `.zip-extracted` and
Local/Remote's `.profile-extracted` are both recognised).

**Keep only active on disk.** Toggle **Project Settings → Sherpa-ONNX → VAD
→ Disk Usage → Keep only active profile on disk**. On every successful
`InitializeAsync` and on every `SwitchProfile(...)` the runtime removes
every registered profile's extraction except the active one. Off by
default. Implied automatically when **Only active profile in build** is on.
On non-Android platforms nothing is extracted, so the toggle is a no-op.

> ⚠️ **After upgrading the plugin from a pre-per-profile-extraction
> version**, regenerate the manifest once via
> `Tools → SherpaOnnx → Rebuild StreamingAssets Manifest`. Old manifests
> have a flat `files` list and trigger a single full extraction at first
> launch (the runtime detects this and falls back gracefully); the new
> manifest format is what enables per-profile lazy extraction and
> per-profile cleanup.

---

## VContainer Integration

### Installer

```csharp
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Vad;
using VContainer;
using VContainer.Unity;

public class VadAsrLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // VAD
        builder.Register<VadService>(Lifetime.Singleton)
            .As<IVadService>();

        // Offline ASR
        builder.Register<AsrService>(Lifetime.Singleton)
            .As<IAsrService>();

        // Pipeline
        builder.Register<VadAsrPipeline>(Lifetime.Singleton);

        // Microphone
        builder.Register<MicrophoneSettingsData>(Lifetime.Singleton);
        builder.Register<MicrophoneSource>(Lifetime.Singleton);

        // Async initialization
        builder.RegisterEntryPoint<VadAsrInitializer>();
    }
}
```

### Async Initialization

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Vad;
using VContainer.Unity;

public class VadAsrInitializer : IAsyncStartable
{
    private readonly IVadService _vad;
    private readonly IAsrService _asr;
    private readonly MicrophoneSettingsData _micSettings;

    public VadAsrInitializer(
        IVadService vad,
        IAsrService asr,
        MicrophoneSettingsData micSettings)
    {
        _vad = vad;
        _asr = asr;
        _micSettings = micSettings;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        var loaded = await MicrophoneSettingsLoader.LoadAsync(ct);
        _micSettings.sampleRate = loaded.sampleRate;
        _micSettings.clipLengthSec = loaded.clipLengthSec;
        _micSettings.micStartTimeoutSec = loaded.micStartTimeoutSec;
        _micSettings.resamplingMode = loaded.resamplingMode;

        await _vad.InitializeAsync(ct: ct);
        await _asr.InitializeAsync(ct: ct);
    }
}
```

### Presenter Example

```csharp
using System;
using PonyuDev.SherpaOnnx.Asr.Offline.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Vad;
using VContainer;
using VContainer.Unity;

public class VadAsrPresenter : IStartable, IDisposable
{
    private readonly VadAsrPipeline _pipeline;
    private readonly MicrophoneSource _mic;

    [Inject]
    public VadAsrPresenter(
        VadAsrPipeline pipeline,
        MicrophoneSource mic)
    {
        _pipeline = pipeline;
        _mic = mic;
    }

    public void Start()
    {
        _pipeline.OnSpeechStart += HandleSpeechStart;
        _pipeline.OnSpeechEnd += HandleSpeechEnd;
        _pipeline.OnResult += HandleResult;
    }

    public async void StartCapture()
    {
        bool ok = await _mic.StartRecordingAsync();
        if (!ok) return;

        _mic.SamplesAvailable += HandleMicSamples;
    }

    public void StopCapture()
    {
        _mic.SamplesAvailable -= HandleMicSamples;
        _mic.StopRecording();
        _pipeline.Flush();
    }

    private void HandleMicSamples(float[] samples)
    {
        _pipeline.AcceptSamples(samples);
    }

    private void HandleSpeechStart()
    {
        // Update UI: show "listening" indicator
    }

    private void HandleSpeechEnd()
    {
        // Update UI: hide "listening" indicator
    }

    private void HandleResult(AsrResult result)
    {
        // Append recognized text to transcript
    }

    public void Dispose()
    {
        _pipeline.OnSpeechStart -= HandleSpeechStart;
        _pipeline.OnSpeechEnd -= HandleSpeechEnd;
        _pipeline.OnResult -= HandleResult;
        StopCapture();
    }
}
```

---

## Zenject Integration

### Installer

```csharp
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Audio;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Vad;
using Zenject;

public class VadAsrInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // VAD
        Container.Bind<IVadService>()
            .To<VadService>()
            .AsSingle();

        // Offline ASR
        Container.Bind<IAsrService>()
            .To<AsrService>()
            .AsSingle();

        // Pipeline
        Container.Bind<VadAsrPipeline>()
            .AsSingle();

        // Microphone
        Container.Bind<MicrophoneSettingsData>()
            .AsSingle();
        Container.Bind<MicrophoneSource>()
            .AsSingle();

        // Initialization
        Container.BindInterfacesTo<VadAsrInitializer>()
            .AsSingle();
    }
}
```

### Initializer

```csharp
using System;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Common.Audio.Config;
using PonyuDev.SherpaOnnx.Vad;
using Zenject;

public class VadAsrInitializer : IInitializable, IDisposable
{
    private readonly IVadService _vad;
    private readonly IAsrService _asr;
    private readonly MicrophoneSettingsData _micSettings;

    public VadAsrInitializer(
        IVadService vad,
        IAsrService asr,
        MicrophoneSettingsData micSettings)
    {
        _vad = vad;
        _asr = asr;
        _micSettings = micSettings;
    }

    public async void Initialize()
    {
        var loaded = await MicrophoneSettingsLoader.LoadAsync();
        _micSettings.sampleRate = loaded.sampleRate;
        _micSettings.clipLengthSec = loaded.clipLengthSec;
        _micSettings.micStartTimeoutSec = loaded.micStartTimeoutSec;
        _micSettings.resamplingMode = loaded.resamplingMode;

        await _vad.InitializeAsync();
        await _asr.InitializeAsync();
    }

    public void Dispose()
    {
        _vad.Dispose();
        _asr.Dispose();
    }
}
```

---

## Android Notes

### Automatic File Extraction

On Android, `StreamingAssets` files are inside the APK archive and need extraction.
The package handles this automatically on `InitializeAsync()`:

1. At build time, a manifest of all SherpaOnnx files is generated
2. On first launch, files are extracted from APK to `persistentDataPath`
3. Subsequent launches skip extraction (version marker check)

**You must use `InitializeAsync()` on Android.** The synchronous `Initialize()`
cannot extract files from the APK.

### Microphone Permission

Ensure `RECORD_AUDIO` permission is in your `AndroidManifest.xml`.
The build validator checks this when ASR is enabled.

### Initialization Progress (`ProfileReadyEvent`)

`IVadService.InitializeAsync` exposes a single semantic callback:

```csharp
UniTask InitializeAsync(
    Action<ProfileReadyEvent> onEvent = null,
    CancellationToken ct = default);
```

Phases fire in order — `Download` (Remote only) → `Extract` (Remote /
LocalZip / Local-on-Android) → `Init` — each carrying its own 0..100
percent in `Percent`. Terminal phases are `Ready` (success) and
`Failed` (retries exhausted or unrecoverable I/O error).

| Phase | When | Notes |
|-------|------|-------|
| `Download` | Remote profile, every chunk written to disk | `Url` set; `Percent` 0..100 |
| `DownloadRetrying` | Network error before retry | `RetryAttempt` 1..N, `Message` = previous error |
| `Extract` | Decompressing zip / tar.gz | Tar streams report 0% then 100%; zips report per-entry |
| `Init` | Native engine ctor on the thread pool | Single 0% before, 100% after |
| `Failed` | Pipeline aborted | `Error` + `Message` describe what |
| `Ready` | Service fully usable | `Percent = 100` |

VAD models are tiny (Silero ≈ 2 MB, TEN-VAD ≈ 1 MB), so for a VAD-only
loading screen `Extract` and `Download` finish almost instantly. The
single-bar weighting still works — just expect `Init` to dominate
visually for VAD.

#### Per-phase status text

```csharp
await _vad.InitializeAsync(OnVadEvent);

void OnVadEvent(ProfileReadyEvent e)
{
    string text = e.Phase switch
    {
        ProfileReadyPhase.Download         => $"Downloading model: {e.Percent}%",
        ProfileReadyPhase.DownloadRetrying => $"Network issue, retrying ({e.RetryAttempt})…",
        ProfileReadyPhase.Extract          => $"Extracting model: {e.Percent}%",
        ProfileReadyPhase.Init             => "Initializing engine…",
        ProfileReadyPhase.Failed           => $"Failed: {e.Message}",
        ProfileReadyPhase.Ready            => "Ready",
        _ => null,
    };
    _statusLabel.text = text;
}
```

#### Single 0..100 unified bar (VAD + ASR pipeline)

When initializing the `VadAsrPipeline`, run both services through the
same bar with `Extract` weighted heaviest (ASR archives are larger):

```csharp
const float DownloadWeight = 0.20f;
const float ExtractWeight  = 0.70f;
const float InitWeight     = 0.10f;

float _vadDl, _vadEx, _vadInit;
float _asrDl, _asrEx, _asrInit;

await _vad.InitializeAsync(e => Track(e, ref _vadDl, ref _vadEx, ref _vadInit));
await _asr.InitializeAsync(e => Track(e, ref _asrDl, ref _asrEx, ref _asrInit));

void Track(ProfileReadyEvent e, ref float dl, ref float ex, ref float init)
{
    switch (e.Phase)
    {
        case ProfileReadyPhase.Download: dl = e.Percent / 100f; break;
        case ProfileReadyPhase.Extract:  ex = e.Percent / 100f; break;
        case ProfileReadyPhase.Init:     init = e.Percent / 100f; break;
        case ProfileReadyPhase.Ready:    dl = ex = init = 1f; break;
    }
    UpdateBar();
}

void UpdateBar()
{
    float vad = _vadDl * DownloadWeight + _vadEx * ExtractWeight + _vadInit * InitWeight;
    float asr = _asrDl * DownloadWeight + _asrEx * ExtractWeight + _asrInit * InitWeight;
    _progressBar.value = Mathf.Clamp01((vad + asr) * 0.5f) * 100f;
}
```

#### Failure handling

```csharp
await _vad.InitializeAsync(e =>
{
    if (e.Phase == ProfileReadyPhase.Failed)
        Debug.LogError($"[VAD] {e.Message}\n{e.Error}");
});

if (!_vad.IsReady)
{
    // Show a retry button — InitializeAsync can be called again.
}
```

---

## API Reference

### IVadService

| Category | Member | Description |
|----------|--------|-------------|
| **Lifecycle** | `Initialize()` | Sync init (Desktop only) |
| | `InitializeAsync(onEvent, ct)` | Async init (all platforms, required on Android). `onEvent` receives `ProfileReadyEvent` (Download / Extract / Init / Ready / Failed). |
| | `LoadProfile(profile)` | Load a specific VAD profile |
| | `SwitchProfile(index)` | Switch by index (sync). |
| | `SwitchProfile(name)` | Switch by name (sync). |
| | `SwitchProfileAsync(index, ct)` | Async switch — native engine ctor on the thread pool, UI thread stays free. Re-emits `ProfileReadyEvent` (Init / Ready / Failed). |
| | `SwitchProfileAsync(name, ct)` | Async switch by name. |
| | `IsProfileAvailable(name)` | `true` when the profile is reachable on disk (or downloadable for Remote with a URL). |
| **Properties** | `IsReady` | `true` when engine is loaded |
| | `ActiveProfile` | Current `VadProfile` |
| | `Settings` | All loaded `VadSettingsData` |
| | `WindowSize` | Audio window size in samples for the current model |
| **Detection** | `AcceptWaveform(samples)` | Feed exactly `WindowSize` samples |
| | `IsSpeechDetected()` | `true` if speech is active |
| | `DrainSegments()` | Get and remove completed speech segments |
| | `Flush()` | Finalize pending speech (call when recording stops) |
| | `Reset()` | Clear detector state |
| **Events** | `OnSegment` | Fires with `VadSegment` when speech segment completes |
| | `OnSpeechStart` | Fires when speech begins |
| | `OnSpeechEnd` | Fires when speech ends |

### VadAsrPipeline

| Category | Member | Description |
|----------|--------|-------------|
| **Properties** | `IsReady` | `true` when both VAD and ASR are ready |
| | `WindowSize` | VAD window size in samples |
| **Audio** | `AcceptSamples(samples)` | Feed arbitrary-length audio (handles buffering) |
| | `Flush()` | Flush VAD buffer |
| | `Reset()` | Reset VAD state and ring buffer |
| **Events** | `OnResult` | Fires with `AsrResult` from recognized speech |
| | `OnSpeechStart` | Passthrough from VAD |
| | `OnSpeechEnd` | Passthrough from VAD |

### VadSegment

| Member | Type | Description |
|--------|------|-------------|
| `StartSample` | `int` | Start sample index in the original waveform |
| `Samples` | `float[]` | PCM audio of the speech segment |
| `Duration` | `float` | Duration in seconds |
| `StartTime` | `float` | Start time in seconds |

### VadProfile

| Field | Default | Description |
|-------|---------|-------------|
| `profileName` | `"New VAD Profile"` | Display name |
| `modelType` | `SileroVad` | `SileroVad` or `TenVad` |
| `sampleRate` | `16000` | Expected sample rate in Hz |
| `numThreads` | `1` | Inference threads |
| `provider` | `"cpu"` | Execution provider |
| `threshold` | `0.5` | Speech probability threshold |
| `minSilenceDuration` | `0.5` | Minimum silence to end speech (seconds) |
| `minSpeechDuration` | `0.25` | Minimum speech to count as valid (seconds) |
| `maxSpeechDuration` | `5.0` | Maximum speech segment length (seconds) |
| `model` | `""` | Model file name (e.g. `silero_vad.onnx`) |
| `windowSize` | `512` | Samples per window (512 for Silero, 256 for TEN-VAD) |
| `bufferSizeInSeconds` | `60` | Internal buffer capacity |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `VadService is not initialized` | Call `Initialize()` or `InitializeAsync()` before feeding audio |
| `No active profile found` | Set an active profile in Project Settings > Sherpa-ONNX > VAD |
| SIGSEGV crash on Android | Ensure you use `InitializeAsync()`, not `Initialize()` |
| No segments detected | Check `threshold` value; lower values detect more speech |
| Segments are too short | Decrease `minSilenceDuration` to keep adjacent speech together |
| Segments are too long | Decrease `maxSpeechDuration` to split long utterances |
| Wrong window size | Silero VAD uses 512, TEN-VAD uses 256 samples |
| Pipeline `OnResult` never fires | Ensure both VAD and ASR are initialized and ready |
| Pipeline not receiving audio | Check `AcceptSamples()` is called with non-empty data |
