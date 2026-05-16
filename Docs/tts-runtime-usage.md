# TTS Runtime Usage Guide

This guide covers how to use text-to-speech at runtime in your Unity project.
For model configuration and import, see [TTS Models Setup](tts-models-setup.md).

## Architecture Overview

The TTS system follows a layered POCO architecture suitable for any DI framework:

```
ITtsService (interface)
    |
    +-- TtsService (POCO, no MonoBehaviour)
    |       |
    |       +-- TtsEngine (native OfflineTts pool)
    |
    +-- CachedTtsService (decorator: LRU cache + AudioClip/AudioSource pools)

Playback (POCO):
    TtsPlaybackHandle — Stop / StopAsync(fade) / Completed / Stopped
    StreamingTtsClip  — ring-buffered AudioClip wrapper for streaming
    SentenceSplitter  — text → sentences for queued playback
```

| Approach | When to use |
|----------|-------------|
| `TtsOrchestrator` (MonoBehaviour) | Prototyping, no DI framework |
| `TtsService` + VContainer | Production with VContainer DI |
| `TtsService` + Zenject | Production with Zenject DI |
| `TtsService` manual | Custom lifecycle management |

For control over playback (cancel, stop, fade, queue, streaming), see
[Playback Control & Cancellation](#playback-control--cancellation) below.

---

## Quick Start (MonoBehaviour)

### Using TtsOrchestrator

The simplest way to get started. Add `TtsOrchestrator` to any GameObject:

1. Create an empty GameObject
2. Add the `TtsOrchestrator` component

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Tts;

public class TtsExample : MonoBehaviour
{
    [SerializeField] private TtsOrchestrator _orchestrator;

    private void Start()
    {
        if (_orchestrator.IsInitialized)
            Speak();
        else
            _orchestrator.Initialized += Speak;
    }

    private void Speak()
    {
        // GenerateAndPlay: generates speech and plays it using pooled
        // AudioClip + AudioSource from the cache. Objects are returned
        // to the pool automatically when playback finishes.
        _orchestrator.GenerateAndPlay("Hello world!");
    }

    private void OnDestroy()
    {
        _orchestrator.Initialized -= Speak;
    }
}
```

`TtsOrchestrator` initializes asynchronously on `Awake`. On Android, this includes
extracting model files from APK to `persistentDataPath`. Use `IsInitialized` or the
`Initialized` event to wait for completion.

### Manual TtsService (no TtsOrchestrator)

```csharp
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts;

public class ManualTtsExample : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    private ITtsService _tts;

    private async void Awake()
    {
        _tts = new TtsService();
        await _tts.InitializeAsync();
    }

    public void Speak(string text)
    {
        // Simple variant: generates and plays via the given AudioSource.
        // Creates a new AudioClip each call (no pooling).
        _tts.GenerateAndPlay(text, _audioSource);
    }

    private void OnDestroy()
    {
        _tts?.Dispose();
    }
}
```

---

## Generation Examples

### Generate and Play (recommended)

The simplest way — one call does everything:

```csharp
// TtsOrchestrator — simplest
_orchestrator.GenerateAndPlay("Hello!");
await _orchestrator.GenerateAndPlayAsync("Hello!");

// ITtsService extension — with pooling (DI scenarios)
_tts.GenerateAndPlay("Hello!", _cache, this);

// ITtsService extension — with your own AudioSource (no pooling)
_tts.GenerateAndPlay("Hello!", _audioSource);
```

### Manual Generation (when you need the result)

Use `Generate()` directly when you need to process the audio data yourself:

```csharp
var result = tts.Generate("Hello world!");
if (result == null) return;

// Access raw samples
float[] pcm = result.Samples;
int sampleRate = result.SampleRate;
float duration = result.DurationSeconds;

// Or create AudioClip manually
var clip = result.ToAudioClip("my-clip");
audioSource.PlayOneShot(clip);
```

### Async Manual Generation

```csharp
var result = await tts.GenerateAsync("Hello world!");
if (result == null) return;

// ToAudioClip must be called on the main thread
audioSource.PlayOneShot(result.ToAudioClip());
```

### Callback with Progress

Receive audio chunks as they are generated:

```csharp
var result = tts.GenerateWithCallbackProgress(
    "Long text to synthesize...",
    speed: 1.0f,
    speakerId: 0,
    callback: (samples, count, progress) =>
    {
        Debug.Log($"Progress: {progress:P0}");
        return 1; // return 0 to stop early
    });
```

### Switching Profiles

```csharp
// By name
_orchestrator.Service.SwitchProfile("vits-piper-en_US-amy-medium");

// By index
_orchestrator.Service.SwitchProfile(0);

// Generate with new profile
_orchestrator.GenerateAndPlay("Now using a different voice.");
```

### Disk Usage (extracted profile directories)

On Android every active profile lands in
`Application.persistentDataPath/SherpaOnnx/tts-models/{profileName}/`,
regardless of `ModelSource`:

| Source   | How it gets there |
|----------|-------------------|
| `Local`  | Lazy per-profile extraction from APK on first `LoadProfile` (only that profile's files; the rest stays bundled). |
| `Remote` | Editor-time import puts files into StreamingAssets; on Android they extract the same way as `Local`. |
| `LocalZip` | Per-profile zip is unpacked the first time it is loaded. |

As users switch profiles, old extractions stay on disk so a re-switch
does not pay the re-extract cost. `ITtsService` implements
`IModelDiskUsage` so the host project can inspect and free that space
without knowing about `LocalZipExtractor` or path constants.

```csharp
// What is on disk
foreach (var name in tts.GetExtractedProfiles())
{
    long bytes = tts.GetExtractedProfileSizeBytes(name);
    Debug.Log($"{name}: {bytes / (1024 * 1024)} MB");
}

// Delete one stale profile
tts.TryDeleteExtractedProfile("old-vits-piper");

// Or sweep everything that is no longer in tts-settings.json
int removed = tts.CleanupUnusedExtractedProfiles();
Debug.Log($"Freed {removed} unused profile(s).");
```

`GetExtractedProfiles()` returns every profile that has an extraction
marker on disk, no matter which source produced it (LocalZip's
`.zip-extracted` and Local/Remote's `.profile-extracted` are both
recognised).

**Keep only active on disk.** Toggle **Project Settings → Sherpa-ONNX → TTS →
Disk Usage → Keep only active profile on disk** (or set
`TtsSettingsData.keepOnlyActiveProfile = true`). On every successful
`InitializeAsync` and on every successful `SwitchProfile(...)`, the runtime
sweeps `persistentDataPath/SherpaOnnx/tts-models/` and removes every
extracted profile registered in TTS settings except the currently active
one. Off by default — leave it off when you alternate between profiles
often, on when ship-size or device-storage matters more than re-switch
latency. Implied automatically when **Only active profile in build** is on
(the build only ships one profile, so keeping more than one extracted
makes no sense). On non-Android platforms profiles are not extracted
(StreamingAssets are already on the filesystem), so this toggle is a no-op
there.

> ⚠️ **After upgrading the plugin from a pre-per-profile-extraction
> version**, regenerate the manifest once via
> `Tools → SherpaOnnx → Rebuild StreamingAssets Manifest`. Old manifests
> have a flat `files` list and trigger a single full extraction at first
> launch (the runtime detects this and falls back gracefully); the new
> manifest format is what enables per-profile lazy extraction and
> per-profile cleanup.

### Custom Speed and Speaker

```csharp
// Slow speech, speaker 2
var result = tts.Generate("Slowly spoken text.", speed: 0.7f, speakerId: 2);
```

---

## Caching

### Configuration

Cache settings are defined in `tts-settings.json` under the `cache` section.
Configure them in **Project Settings > Sherpa-ONNX > TTS > Cache Settings**.

| Field | Default | Description |
|-------|---------|-------------|
| `offlineTtsEnabled` | `true` | Enable native engine pool |
| `offlineTtsPoolSize` | `4` | Concurrent native OfflineTts instances |
| `resultCacheEnabled` | `true` | LRU cache for generated audio |
| `resultCacheSize` | `8` | Max cached TtsResult entries |
| `audioClipEnabled` | `true` | AudioClip object pool |
| `audioClipPoolSize` | `4` | Max pooled AudioClips |
| `audioSourceEnabled` | `true` | AudioSource component pool |
| `audioSourcePoolSize` | `4` | Max pooled AudioSources |

### How It Works

When `cache` is present in `tts-settings.json`, `TtsOrchestrator` automatically wraps
`TtsService` with `CachedTtsService`. This adds three layers:

| Layer | What it does |
|-------|-------------|
| **Result cache** | LRU memoization — repeated `Generate("same text")` returns instantly |
| **AudioClip pool** | Reuses AudioClip objects instead of creating new ones |
| **AudioSource pool** | Reuses AudioSource components for parallel playback |

The `ITtsService` interface stays the same — caching is transparent. `Generate()` and
`GenerateAsync()` are automatically cached. Callback methods are forwarded without caching.

### GenerateAndPlay with Pools (recommended)

`GenerateAndPlay` handles the full pipeline — generate, rent from pool, play,
return to pool when done:

```csharp
// TtsOrchestrator: one call does everything
_orchestrator.GenerateAndPlay("Hello!");

// Or via ITtsService extension (DI scenarios)
_tts.GenerateAndPlay("Hello!", _cache, this);
```

### Manual Rent/Return

For advanced scenarios where you need direct control over pooled objects:

```csharp
// _tts and _cache obtained from TtsOrchestrator on init (see Quick Start)
var result = _tts.Generate("Hello!");

// Rent from pool
var clip = _cache.RentClip(result);
var source = _cache.RentSource();

source.clip = clip;
source.Play();

// Return when done
StartCoroutine(ReturnAfterPlay(source, clip));

private IEnumerator ReturnAfterPlay(AudioSource source, AudioClip clip)
{
    yield return new WaitWhile(() => source.isPlaying);
    _cache.ReturnSource(source);
    _cache.ReturnClip(clip);
}
```

### Runtime Cache Control

```csharp
// _cache obtained from TtsOrchestrator on init (see Quick Start)

// Toggle caches
_cache.ResultCacheEnabled = false; // disabling clears the cache
_cache.AudioClipPoolEnabled = true;

// Resize
_cache.ResultCacheMaxSize = 16;

// Clear
_cache.ClearAll();

// Inspect
Debug.Log($"Cached results: {_cache.ResultCacheCount}");
Debug.Log($"Available clips: {_cache.AudioClipAvailableCount}");
```

---

## Playback Control & Cancellation

The package provides four progressively more flexible APIs for playing TTS audio.
Pick the simplest one that fits the use case:

| API | When to use | Returns |
|-----|-------------|---------|
| `GenerateAndPlay` / `GenerateAndPlayAsync` | Fire-and-forget short phrase | `TtsResult` |
| `GenerateAndPlayWithHandleAsync` | Need stop / fade / completion events | `TtsPlaybackHandle` |
| `SpeakStreamingAsync` | Long single sentence — start playing while still synthesizing | `TtsPlaybackHandle` |
| `Speak` | Long paragraphs — sentence-level pre-gen + queue | `UniTask` |

All `*Async` methods accept a `CancellationToken` and throw
`OperationCanceledException` when triggered. The service-level CTS is also
cancelled in `Dispose()`, so any in-flight gen aborts cleanly when the service
goes away.

### Cancellation

```csharp
var cts = new CancellationTokenSource();

try
{
    var result = await _tts.GenerateAsync("Long text...", cts.Token);
    // ...
}
catch (OperationCanceledException)
{
    Debug.Log("Generation cancelled.");
}

// Elsewhere — typically a Stop button or scene unload:
cts.Cancel(); // aborts the in-flight gen within ~1 callback tick
```

Sherpa-onnx's native call returns 0 from its callback on cancellation — the only
native-correct way to abort mid-synthesis. The wrapper does this transparently;
you just pass the token.

### Playback handles

`GenerateAndPlayWithHandleAsync` returns a `TtsPlaybackHandle` that lets you
stop, fade, or observe completion:

```csharp
var handle = await _orchestrator.GenerateAndPlayWithHandleAsync("Hello world!");
if (handle == null) return; // engine not ready or gen failed

handle.Completed += () => Debug.Log("Played to the end.");
handle.Stopped   += () => Debug.Log("Stopped explicitly.");

if (handle.IsPlaying) { /* mid-playback */ }
```

#### Instant stop

```csharp
handle.Stop(); // cuts audio immediately, runs cleanup
```

#### Fade-out stop

```csharp
await handle.StopAsync(fadeSeconds: 0.5f);
// Exponential 500 ms fade-out (sounds natural for voice), then full stop.
// Uses unscaled time so it works on Time.timeScale = 0.
```

### StopAll across handles

`TtsOrchestrator` tracks every handle it produces. To stop everything at once
(e.g. on game-state change, dialogue interrupt, or scene unload):

```csharp
await _orchestrator.StopAll(fadeSeconds: 0.3f);
// Fades every active handle in parallel, then stops them.
```

`ActivePlaybackCount` exposes the current count (useful for inspector / UI).

### Playback mode (Overlap vs Exclusive)

For non-pooled `GenerateAndPlay(text, source)`, the optional `mode` parameter
controls how multiple sequential calls behave on the same `AudioSource`:

```csharp
// Overlap (default, backwards-compatible) — uses PlayOneShot.
// Multiple calls can play simultaneously on the same source.
_tts.GenerateAndPlay("Bing!", _audioSource);
_tts.GenerateAndPlay("Bong!", _audioSource); // plays on top of "Bing!"

// Exclusive — new clip interrupts the previous one.
_tts.GenerateAndPlay("Bing!", _audioSource, TtsPlaybackMode.Exclusive);
_tts.GenerateAndPlay("Bong!", _audioSource, TtsPlaybackMode.Exclusive);
// "Bing!" gets cut off when "Bong!" starts.
```

`TtsOrchestrator` exposes this as `DefaultPlaybackMode` — both as a SerializeField
on the component and as a runtime property. Pooled paths always behave like
Exclusive because each playback rents its own dedicated source.

### Streaming playback (low first-audio latency)

For long single sentences, `SpeakStreamingAsync` returns a handle as soon as
sherpa-onnx emits the first chunk — playback starts before generation completes:

```csharp
var handle = await _orchestrator.SpeakStreamingAsync(
    "This is a long sentence and the audio starts playing while the rest is still being synthesized.",
    ct);
```

The win is most visible when the underlying model emits per-chunk callbacks
during synthesis. For models that emit one big callback at the end of each
sentence (typical for VITS-Piper), the latency improvement is small for a
single sentence — use `Speak` instead for paragraphs.

> ⚠️ **IL2CPP (iOS / Android / IL2CPP Standalone):** native chunk-by-chunk
> streaming relies on a P/Invoke callback that IL2CPP cannot marshal — under
> IL2CPP `SpeakStreamingAsync` falls back to non-streaming generation
> (functional but no first-audio-latency benefit). Use **Sentence queue**
> below for the equivalent low-latency long-text experience on every
> scripting backend.

### Sentence queue (long paragraphs) — recommended for IL2CPP

For multi-sentence text, `Speak` splits at punctuation (Latin `.!?` and CJK
`。！？`) and queues per-sentence generation+playback. While sentence N plays,
sentence N+1 is generated in the background — virtually no gap between them.
**Pure C#, no native callbacks** — works on every scripting backend including
IL2CPP, where it is the recommended low-latency path:

```csharp
await _orchestrator.Speak(
    "First sentence. Second sentence. Third sentence.",
    ct,
    lookAhead: 1);
```

`lookAhead` (default 1) is the size of the sliding pre-gen window. Bump it to
2-4 for heavier models (Matcha, Kokoro, voice cloning) or slower hardware
(Android), where gen time approaches playback time and `lookAhead = 1` would
leave audible gaps between sentences. For real parallelism, the engine pool
should match: `EnginePoolSize >= lookAhead`.

`SentenceSplitter.Split(text)` is exposed publicly for users who want to drive
their own queue.

### Disposal during playback

`TtsService.Dispose()` cancels its internal CTS, so any in-flight generation
returns `OperationCanceledException` cleanly — no SEGV, no orphan native call.
`TtsOrchestrator.OnDestroy()` disposes all tracked handles first, then the
service. Clip / source cleanup runs through each handle's cleanup callback,
so pooled paths return resources to the pool and non-pooled paths destroy the
clip.

---

## VContainer Integration

<details>
<summary>Click to expand — installer, async startup, decorator wiring, presenter usage</summary>

### Installer

```csharp
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Data;
using VContainer;
using VContainer.Unity;

public class TtsLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Core service
        builder.Register<TtsService>(Lifetime.Singleton)
            .As<ITtsService>();

        // Async initialization
        builder.RegisterEntryPoint<TtsInitializer>();
    }
}
```

### Async Initialization

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts;
using VContainer.Unity;

public class TtsInitializer : IAsyncStartable
{
    private readonly ITtsService _tts;

    public TtsInitializer(ITtsService tts)
    {
        _tts = tts;
    }

    public async UniTask StartAsync(CancellationToken ct)
    {
        await _tts.InitializeAsync(ct: ct);
    }
}
```

### With CachedTtsService Decorator

```csharp
protected override void Configure(IContainerBuilder builder)
{
    // Inner service (not exposed directly)
    builder.Register<TtsService>(Lifetime.Singleton);

    // Cached decorator as ITtsService
    builder.Register<CachedTtsService>(Lifetime.Singleton)
        .WithParameter<TtsCacheSettings>(new TtsCacheSettings
        {
            resultCacheSize = 16,
            audioClipPoolSize = 8
        })
        .As<ITtsService>()
        .As<ITtsCacheControl>();

    builder.RegisterEntryPoint<TtsInitializer>();
}
```

### Presenter Example

```csharp
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using UnityEngine;
using VContainer;

public class DialoguePresenter
{
    private readonly ITtsService _tts;
    private readonly ITtsCacheControl _cache;
    private readonly MonoBehaviour _owner;

    [Inject]
    public DialoguePresenter(
        ITtsService tts,
        ITtsCacheControl cache,
        MonoBehaviour owner)
    {
        _tts = tts;
        _cache = cache;
        _owner = owner;
    }

    public void SpeakLine(string line)
    {
        _tts.GenerateAndPlay(line, _cache, _owner);
    }

    public async void SpeakLineAsync(string line)
    {
        await _tts.GenerateAndPlayAsync(line, _cache, _owner);
    }
}
```

</details>

---

## Zenject Integration

<details>
<summary>Click to expand — installer, async startup, decorator wiring, [Inject] usage</summary>

### Installer

```csharp
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using PonyuDev.SherpaOnnx.Tts.Data;
using Zenject;

public class TtsInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Core service
        Container.Bind<TtsService>()
            .AsSingle();

        // Cached decorator as ITtsService + ITtsCacheControl
        Container.Bind(typeof(ITtsService), typeof(ITtsCacheControl))
            .To<CachedTtsService>()
            .AsSingle()
            .WithArguments(new TtsCacheSettings
            {
                resultCacheSize = 16,
                audioClipPoolSize = 8
            });

        // Initialization
        Container.BindInterfacesTo<TtsInitializer>()
            .AsSingle();
    }
}
```

### Initializer

```csharp
using System;
using Cysharp.Threading.Tasks;
using PonyuDev.SherpaOnnx.Tts;
using Zenject;

public class TtsInitializer : IInitializable, IDisposable
{
    private readonly ITtsService _tts;

    public TtsInitializer(ITtsService tts)
    {
        _tts = tts;
    }

    public async void Initialize()
    {
        await _tts.InitializeAsync();
    }

    public void Dispose()
    {
        _tts.Dispose();
    }
}
```

### Usage with [Inject]

```csharp
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Tts.Cache;
using UnityEngine;
using Zenject;

public class NpcSpeaker : MonoBehaviour
{
    [Inject] private ITtsService _tts;
    [Inject] private ITtsCacheControl _cache;

    public void Say(string text)
    {
        _tts.GenerateAndPlay(text, _cache, this);
    }

    public async void SayAsync(string text)
    {
        await _tts.GenerateAndPlayAsync(text, _cache, this);
    }
}
```

</details>

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

### Initialization Progress (`ProfileReadyEvent`)

`InitializeAsync` exposes a single semantic callback for the whole
readiness pipeline:

```csharp
UniTask InitializeAsync(
    Action<ProfileReadyEvent> onEvent = null,
    CancellationToken ct = default);
```

Phases are emitted in order — `Download` (Remote only) → `Extract`
(Remote / LocalZip / Local-on-Android) → `Init` — each carrying its own
0..100 percent in `Percent`. Terminal phases are `Ready` (success) and
`Failed` (retries exhausted or unrecoverable I/O error).

| Phase | When | Notes |
|-------|------|-------|
| `Download` | Remote profile, every chunk written to disk | `Url` set; `Percent` 0..100 |
| `DownloadRetrying` | Network error before retry | `RetryAttempt` 1..N, `Message` = previous error |
| `Extract` | Decompressing zip / tar.gz | Tar streams report 0% then 100%; zips report per-entry |
| `Init` | Native engine ctor on the thread pool | Single 0% before, 100% after — opaque native call |
| `Failed` | Pipeline aborted | `Error` + `Message` describe what |
| `Ready` | Service is fully usable | Last event on success, `Percent = 100` |

`ProfileReadyEvent` is a `readonly struct`, so the callback fires with
zero allocations per event.

#### Per-phase status text

Simplest pattern — drive a single status label off the phase:

```csharp
await tts.InitializeAsync(OnTtsEvent);

void OnTtsEvent(ProfileReadyEvent e)
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

#### Single 0..100 unified bar

For a loading screen with one progress bar, weight the phases. Extract
is the longest step (largest models can take 10+ s to unpack on
Android), so it gets the heaviest weight:

```csharp
// Tune weights to match your profile mix. Defaults below assume Remote
// or LocalZip on Android — Extract dominates total wall time.
const float DownloadWeight = 0.20f;
const float ExtractWeight  = 0.70f;
const float InitWeight     = 0.10f;

float _downloadDone, _extractDone, _initDone;

void OnTtsEvent(ProfileReadyEvent e)
{
    switch (e.Phase)
    {
        case ProfileReadyPhase.Download: _downloadDone = e.Percent / 100f; break;
        case ProfileReadyPhase.Extract:  _extractDone  = e.Percent / 100f; break;
        case ProfileReadyPhase.Init:     _initDone     = e.Percent / 100f; break;
        case ProfileReadyPhase.Ready:    _downloadDone = _extractDone = _initDone = 1f; break;
    }

    float total =
        _downloadDone * DownloadWeight +
        _extractDone  * ExtractWeight  +
        _initDone     * InitWeight;

    _progressBar.value = Mathf.Clamp01(total) * 100f;
}
```

For `Local` or `LocalZip` profiles where there is no download, drop
`DownloadWeight` to `0` and rebalance Extract / Init (e.g. `0.85` /
`0.15`).

#### Failure handling

```csharp
await tts.InitializeAsync(e =>
{
    if (e.Phase == ProfileReadyPhase.Failed)
        Debug.LogError($"[TTS] {e.Message}\n{e.Error}");
});

if (!tts.IsReady)
{
    // Show a retry button — the service stays alive and InitializeAsync
    // can be called again once the user reconnects.
}
```

### Locale Handling

Some Android devices with European locales use comma as the decimal separator,
which can cause issues with the native sherpa-onnx library. The package includes
a `NativeLocaleGuard` that automatically forces the C locale during native calls.
No action required from the user.

---

## API Reference

### ITtsService

All `*Async` methods accept an optional `CancellationToken ct = default` and
throw `OperationCanceledException` on cancel. The token is omitted from rows
below for brevity.

| Category | Method | Description |
|----------|--------|-------------|
| **Lifecycle** | `Initialize()` | Sync init (Desktop only) |
| | `InitializeAsync(onEvent, ct)` | Async init (all platforms, required on Android). `onEvent` receives `ProfileReadyEvent` (Download / Extract / Init / Ready / Failed). |
| | `LoadProfile(profile)` | Load a specific profile |
| | `SwitchProfile(index)` | Switch by index |
| | `SwitchProfile(name)` | Switch by name |
| **Properties** | `IsReady` | `true` when engine is loaded |
| | `ActiveProfile` | Current `TtsProfile` |
| | `Settings` | All loaded `TtsSettingsData` |
| | `SampleRate` | Loaded engine's sample rate in Hz, `0` if not loaded |
| | `EnginePoolSize` | Get/set concurrent native instances |
| **Generation** | `Generate(text)` | Sync, uses active profile speed/speakerId |
| | `Generate(text, speed, speakerId)` | Sync with explicit parameters |
| | `GenerateAsync(text, ct)` | Background thread generation |
| | `GenerateAsync(text, speed, speakerId, ct)` | Background thread with parameters |
| **Callbacks** ⚠️ | `GenerateWithCallback(...)` | Chunk callback (streaming) |
| | `GenerateWithCallbackProgress(...)` | Chunk callback with progress float |
| | `GenerateWithConfig(...)` | Advanced config (reference audio, numSteps) |
| **Async callbacks** ⚠️ | `GenerateWithCallbackAsync(..., ct)` | Background thread + chunk callback |
| | `GenerateWithCallbackProgressAsync(..., ct)` | Background thread + progress |
| | `GenerateWithConfigAsync(..., ct)` | Background thread + advanced config |

> ⚠️ **IL2CPP note for callback APIs:** sherpa-onnx C# bindings wrap user
> callbacks in closures that point to instance methods, which IL2CPP cannot
> marshal to native code. On IL2CPP builds (iOS / Android / IL2CPP
> Standalone) the plugin transparently falls back to the callback-less
> `Generate(...)` path — the audio is still produced (whole result at the
> end), but per-chunk progress callbacks are **not invoked** and a one-time
> warning is logged. For low-latency long-text playback on IL2CPP, use
> `ITtsService.Speak(text, audio, ct, lookAhead)` (sentence-queue, pure C#).

### TtsResult

| Member | Type | Description |
|--------|------|-------------|
| `Samples` | `float[]` | Raw PCM mono float32 data |
| `SampleRate` | `int` | Sample rate in Hz (e.g. 22050) |
| `NumSamples` | `int` | Number of samples |
| `DurationSeconds` | `float` | Audio duration |
| `IsValid` | `bool` | Has valid samples |
| `ToAudioClip(name)` | `AudioClip` | Create Unity AudioClip (main thread only) |
| `Clone()` | `TtsResult` | Deep copy of samples |

### TtsPlaybackHandle

Returned by handle-based playback APIs (`GenerateAndPlayWithHandleAsync`,
`SpeakStreamingAsync`). POCO — no MonoBehaviour requirement.

| Member | Type | Description |
|--------|------|-------------|
| `Result` | `TtsResult` | Generated audio. `null` for streaming clips (audio fills in over time). |
| `Source` | `AudioSource` | The source playing the clip |
| `Clip` | `AudioClip` | The clip set on the source |
| `IsPlaying` | `bool` | True while audio is actively playing |
| `IsStopped` | `bool` | True after `Stop` / `StopAsync` runs or playback ends naturally |
| `Completed` | `event Action` | Fires once on natural end of playback |
| `Stopped` | `event Action` | Fires once on explicit `Stop` / `StopAsync` / `Dispose` |
| `Stop()` | — | Immediate stop + cleanup. Idempotent. |
| `StopAsync(fadeSeconds)` | `UniTask` | Exponential fade-out using unscaled time, then full stop |
| `Dispose()` | — | Equivalent to `Stop()` |

### TtsPlaybackMode

Enum for non-pooled `GenerateAndPlay(text, source)`:

| Value | Behavior |
|-------|----------|
| `Overlap` (default) | Uses `AudioSource.PlayOneShot`. Multiple TTS clips can play simultaneously on the same source. Backwards-compatible. |
| `Exclusive` | Sets `AudioSource.clip` and calls `Play`. New clip interrupts the previous one on the same source. Recommended for chat-bot / dialogue UX. |

### TtsOrchestrator — playback methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GenerateAndPlay(text)` | `TtsResult` | Fire-and-forget. Uses pooled objects when cache configured. |
| `GenerateAndPlayAsync(text)` | `Task<TtsResult>` | Same but background-thread generation. |
| `GenerateAndPlayWithHandleAsync(text, ct)` | `Task<TtsPlaybackHandle>` | Returns a handle for stop / fade / events. Auto-tracked for `StopAll`. |
| `SpeakStreamingAsync(text, ct)` | `UniTask<TtsPlaybackHandle>` | First audio plays as soon as the first sherpa-onnx chunk arrives. |
| `Speak(text, ct, lookAhead)` | `UniTask` | Splits text into sentences and queues per-sentence playback with sliding pre-gen window. |
| `StopAll(fadeSeconds)` | `UniTask` | Stops every active handle. Fades them in parallel if `fadeSeconds > 0`. |
| `ActivePlaybackCount` | `int` | Number of currently-tracked handles. |
| `DefaultPlaybackMode` | `TtsPlaybackMode` | Mode used by the non-pooled `GenerateAndPlay` fallback. SerializeField + runtime property. |

### ITtsService — GenerateAndPlay Extensions

Extension methods for DI scenarios where `ITtsService` is injected directly.

| Method | Description |
|--------|-------------|
| `GenerateAndPlay(text, audioSource, mode = Overlap)` | Generate + play via given AudioSource. New AudioClip each call, auto-disposed after playback. |
| `GenerateAndPlayAsync(text, audioSource, mode = Overlap)` | Same but generation on background thread. |
| `GenerateAndPlay(text, cache, owner)` | Pooled AudioClip + AudioSource. Auto-returns to pool. |
| `GenerateAndPlayAsync(text, cache, owner)` | Pooled, background-thread generation. |
| `GenerateAndPlayWithHandleAsync(text, audioSource, ct)` | Returns `TtsPlaybackHandle`. Always Exclusive mode (handles need a single voice). |
| `GenerateAndPlayWithHandleAsync(text, cache, ct)` | Same on a pooled source. Source returns to pool on stop/complete. |
| `SpeakStreamingAsync(text, audioSource, ct)` | Streaming variant — first audio plays as soon as first chunk arrives. |
| `SpeakStreamingAsync(text, cache, ct)` | Streaming on a pooled source. |
| `Speak(text, audioSource, ct, onHandleStarted, lookAhead)` | Sentence-queue playback. `onHandleStarted` fires per-sentence (used by orchestrator for `Track`). |

The `cache` parameter is `ITtsCacheControl` (from DI or `TtsOrchestrator.CacheControl`).
The `owner` parameter is any `MonoBehaviour` used to run the return-to-pool coroutine
(only the legacy non-handle variants need it).

### SentenceSplitter

Static utility used internally by `Speak` and exposed publicly.

| Method | Description |
|--------|-------------|
| `Split(text)` | Returns trimmed, non-empty sentences. Recognises Latin (`. ! ?`) and CJK (`。 ！ ？`) terminators. |

### ITtsCacheControl

Available via `TtsOrchestrator.CacheControl` or by casting `CachedTtsService`.

| Category | Member | Description |
|----------|--------|-------------|
| **Toggle** | `ResultCacheEnabled` | Enable/disable LRU result cache |
| | `AudioClipPoolEnabled` | Enable/disable AudioClip pool |
| | `AudioSourcePoolEnabled` | Enable/disable AudioSource pool |
| **Sizes** | `ResultCacheMaxSize` | Max cached results (evicts LRU) |
| | `AudioClipPoolMaxSize` | Max pooled clips |
| | `AudioSourcePoolMaxSize` | Max pooled sources |
| **Counts** | `ResultCacheCount` | Current cached results |
| | `AudioClipAvailableCount` | Available clips in pool |
| | `AudioSourceAvailableCount` | Available sources in pool |
| **Clear** | `ClearAll()` | Clear all caches |
| | `ClearResultCache()` | Clear only results |
| | `ClearClipPool()` | Clear only clips |
| | `ClearSourcePool()` | Clear only sources |
| **Rent/Return** | `RentClip(result)` | Get AudioClip from pool, filled with result data |
| | `ReturnClip(clip)` | Return clip to pool |
| | `RentSource()` | Get idle AudioSource |
| | `ReturnSource(source)` | Return source (stops playback) |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `TtsService is not initialized` | Call `Initialize()` or `InitializeAsync()` before `Generate()` |
| `No active profile found` | Set an active profile in Project Settings > Sherpa-ONNX > TTS |
| SIGSEGV crash on Android | Ensure you use `InitializeAsync()`, not `Initialize()` |
| `Manifest is empty or failed to load` | Rebuild manifest: Tools > SherpaOnnx > Rebuild StreamingAssets Manifest |
| `silenceScale '-0.000' is too small` | Update to latest package version (includes locale fix) |
| `Generate()` returns null | Check logs for engine errors; verify model files exist |
| Cache not working | Ensure `cache` section exists in `tts-settings.json` |
| `ToAudioClip()` throws | Must be called on the main thread, not from `Task.Run` |
| `OperationCanceledException` after cancel | Expected behavior — wrap async calls in `try/catch (OperationCanceledException)` and treat as a clean exit. Service-level CTS is also cancelled in `Dispose()`, so disposing during in-flight gen produces this exception too. |
| Streaming sample plays only first word | You hit a model that doesn't emit per-chunk callbacks (typical for VITS-Piper on a single sentence). Use `Speak` instead — it splits the text in C# and queues per-sentence playback. |
| Audible gaps between sentences in `Speak` | Generation is slower than playback. Increase `lookAhead` to 2-4 and ensure `EnginePoolSize >= lookAhead` so multiple sentences pre-generate in parallel. |
