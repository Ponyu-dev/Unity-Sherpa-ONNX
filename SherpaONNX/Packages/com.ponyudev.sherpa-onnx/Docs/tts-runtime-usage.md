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
```

| Approach | When to use |
|----------|-------------|
| `TtsOrchestrator` (MonoBehaviour) | Prototyping, no DI framework |
| `TtsService` + VContainer | Production with VContainer DI |
| `TtsService` + Zenject | Production with Zenject DI |
| `TtsService` manual | Custom lifecycle management |

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

## VContainer Integration

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

---

## Zenject Integration

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

### Progress Tracking

Monitor extraction progress on Android (useful for loading screens):

```csharp
var progress = new Progress<float>(p =>
    Debug.Log($"Extracting: {p:P0}"));

await tts.InitializeAsync(progress);
```

### Locale Handling

Some Android devices with European locales use comma as the decimal separator,
which can cause issues with the native sherpa-onnx library. The package includes
a `NativeLocaleGuard` that automatically forces the C locale during native calls.
No action required from the user.

---

## API Reference

### ITtsService

| Category | Method | Description |
|----------|--------|-------------|
| **Lifecycle** | `Initialize()` | Sync init (Desktop only) |
| | `InitializeAsync(progress, ct)` | Async init (all platforms, required on Android) |
| | `LoadProfile(profile)` | Load a specific profile |
| | `SwitchProfile(index)` | Switch by index |
| | `SwitchProfile(name)` | Switch by name |
| **Properties** | `IsReady` | `true` when engine is loaded |
| | `ActiveProfile` | Current `TtsProfile` |
| | `Settings` | All loaded `TtsSettingsData` |
| | `EnginePoolSize` | Get/set concurrent native instances |
| **Generation** | `Generate(text)` | Sync, uses active profile speed/speakerId |
| | `Generate(text, speed, speakerId)` | Sync with explicit parameters |
| | `GenerateAsync(text)` | Background thread generation |
| | `GenerateAsync(text, speed, speakerId)` | Background thread with parameters |
| **Callbacks** | `GenerateWithCallback(...)` | Chunk callback (streaming) |
| | `GenerateWithCallbackProgress(...)` | Chunk callback with progress float |
| | `GenerateWithConfig(...)` | Advanced config (reference audio, numSteps) |
| **Async callbacks** | `GenerateWithCallbackAsync(...)` | Background thread + chunk callback |
| | `GenerateWithCallbackProgressAsync(...)` | Background thread + progress |
| | `GenerateWithConfigAsync(...)` | Background thread + advanced config |

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

### TtsOrchestrator — GenerateAndPlay

| Method | Description |
|--------|-------------|
| `GenerateAndPlay(text)` | Generate + play. Uses pooled objects when cache is configured, otherwise creates AudioSource on the same GameObject. |
| `GenerateAndPlayAsync(text)` | Same but generation runs on background thread |

### ITtsService — GenerateAndPlay Extensions

Extension methods for DI scenarios where `ITtsService` is injected directly.

| Method | Description |
|--------|-------------|
| `GenerateAndPlay(text, audioSource)` | Generate + play via given AudioSource (new AudioClip each time) |
| `GenerateAndPlayAsync(text, audioSource)` | Same but generation runs on background thread |
| `GenerateAndPlay(text, cache, owner)` | Generate + play using pooled AudioClip and AudioSource. Auto-returns to pool when done. |
| `GenerateAndPlayAsync(text, cache, owner)` | Same but generation runs on background thread |

The `cache` parameter is `ITtsCacheControl` (from DI or `TtsOrchestrator.CacheControl`).
The `owner` parameter is any `MonoBehaviour` used to run the return-to-pool coroutine.

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
