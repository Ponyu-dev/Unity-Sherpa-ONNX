# KWS Runtime Usage Guide

This guide covers how to use Keyword Spotting (KWS) at runtime in your Unity project.
For model import and configuration, see Project Settings > Sherpa-ONNX > KWS.

## Architecture Overview

The KWS system detects predefined keywords in real-time audio.
Designed for always-on background listening with minimal resource usage.

```
IKwsService
    |
    +-- KwsService (POCO, no MonoBehaviour)
            |
            +-- KwsEngine (native KeywordSpotter)
```

| Approach | When to use |
|----------|-------------|
| `KwsOrchestrator` | Quick prototype, no DI framework |
| `KwsService` manual | Full control over lifecycle |
| `KwsService` + VContainer | Production with DI framework |
| `KwsService` + Zenject | Production with Zenject DI |

---

## Quick Start — KwsOrchestrator

The simplest way to add keyword detection. Attach to any GameObject:

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Kws;
using PonyuDev.SherpaOnnx.Kws.Engine;

public class KwsDemo : MonoBehaviour
{
    [SerializeField] private KwsOrchestrator _kws;

    private void OnEnable()
    {
        _kws.OnKeywordDetected += HandleKeyword;
        _kws.Initialized += HandleInitialized;
    }

    private void OnDisable()
    {
        _kws.OnKeywordDetected -= HandleKeyword;
        _kws.Initialized -= HandleInitialized;
    }

    private void HandleInitialized()
    {
        Debug.Log("KWS ready, listening for keywords...");
    }

    private void HandleKeyword(KwsResult result)
    {
        Debug.Log($"Keyword detected: {result.Keyword}");
    }
}
```

`KwsOrchestrator` auto-initializes on Awake:
1. Creates `KwsService` and calls `InitializeAsync()`
2. Starts a session (creates native stream)
3. Creates `MicrophoneSource` and starts recording
4. Routes audio samples to KWS engine each frame

---

## Manual KwsService

For full lifecycle control without MonoBehaviour:

```csharp
using UnityEngine;
using PonyuDev.SherpaOnnx.Kws;
using PonyuDev.SherpaOnnx.Kws.Engine;
using PonyuDev.SherpaOnnx.Common.Audio;

public class KwsManualExample : MonoBehaviour
{
    private IKwsService _kws;
    private MicrophoneSource _mic;

    private async void Awake()
    {
        _kws = new KwsService();
        await _kws.InitializeAsync();

        if (!_kws.IsReady)
        {
            Debug.LogError("KWS failed to initialize");
            return;
        }

        _kws.OnKeywordDetected += HandleKeyword;
        _kws.StartSession();

        _mic = new MicrophoneSource();
        _mic.SamplesAvailable += HandleSamples;
        await _mic.StartRecordingAsync();
    }

    private void HandleSamples(float[] samples)
    {
        int sampleRate = _kws.ActiveProfile?.sampleRate ?? 16000;
        _kws.AcceptSamples(samples, sampleRate);
        _kws.ProcessAvailableFrames();
    }

    private void HandleKeyword(KwsResult result)
    {
        Debug.Log($"Detected: {result.Keyword}");
    }

    private void OnDestroy()
    {
        if (_mic != null)
        {
            _mic.SamplesAvailable -= HandleSamples;
            _mic.Dispose();
        }

        if (_kws != null)
        {
            _kws.OnKeywordDetected -= HandleKeyword;
            _kws.Dispose();
        }
    }
}
```

### Sync vs. Async Initialization

| Method | Platform | Notes |
|--------|----------|-------|
| `Initialize()` | Desktop only | Blocks main thread; fails for LocalZip profiles on Android |
| `InitializeAsync()` | All platforms | Extracts StreamingAssets on Android, then loads |

---

## Custom Keywords

Keywords can be configured in two ways:

### 1. Keywords File (Editor)

Set `keywordsFile` in the profile via Project Settings > KWS.
The file contains one keyword per line in token format:

```
token1 token2 token3 @TAG :boost #threshold
```

- **Tokens** (required): space-separated tokens from the model's `tokens.txt`
- **@TAG** (optional): identifier returned on detection
- **:boost** (optional): keyword-specific score boost (e.g. `:1.5`)
- **#threshold** (optional): keyword-specific detection threshold (e.g. `#0.3`)

### 2. Custom Keywords Text (Editor)

Enter keywords directly in the "Custom Keywords" text field in Project Settings.
Same format as the keywords file. Passed as `keywords_buf` at runtime.
Both sources are merged — you can use file + custom text together.

### Per-Model Token Format

Each model uses a different tokenization. Tokens must match the model's `tokens.txt`:

| Model | Token type | Example |
|-------|------------|---------|
| gigaspeech (EN) | BPE subwords | `▁HE LL O ▁WORLD @HELLO` |
| wenetspeech (ZH) | ppinyin | `n ǐ h ǎo @你好` |
| zh-en (ZH+EN) | phone + ppinyin | `HH AH0 L OW1 @HELLO` / `n ǐ h ǎo @你好` |

Use `sherpa-onnx text2token` to convert plain text to the correct token format.
See [KWS Models Setup](kws-models-setup.md#per-model-tokenization) for full details.

### Token Validation

Keywords are validated against the model vocabulary at two levels:
- **Editor**: tokens not in `tokens.txt` are shown as warnings in real-time
- **Runtime**: `KwsEngine.Load()` blocks loading if invalid tokens are found,
  preventing native crashes

---

## Profile Switching

Switch between KWS profiles at runtime:

```csharp
// By index
_kws.SwitchProfile(1);

// By name
_kws.SwitchProfile("my-kws-model");
```

Note: switching profiles stops the current session, loads the new model,
and requires calling `StartSession()` again.

---

## VContainer Integration

```csharp
using VContainer;
using VContainer.Unity;
using PonyuDev.SherpaOnnx.Kws;

public class KwsLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<KwsService>(Lifetime.Singleton).As<IKwsService>();
    }
}

public class KwsPresenter : IStartable, System.IDisposable
{
    private readonly IKwsService _kws;

    public KwsPresenter(IKwsService kws)
    {
        _kws = kws;
    }

    public async void Start()
    {
        await _kws.InitializeAsync();
        _kws.OnKeywordDetected += HandleKeyword;
        _kws.StartSession();
    }

    public void Dispose()
    {
        _kws.OnKeywordDetected -= HandleKeyword;
        _kws.Dispose();
    }

    private void HandleKeyword(KwsResult result)
    {
        UnityEngine.Debug.Log($"Keyword: {result.Keyword}");
    }
}
```

---

## Zenject Integration

```csharp
using Zenject;
using PonyuDev.SherpaOnnx.Kws;

public class KwsInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IKwsService>()
            .To<KwsService>()
            .AsSingle();
    }
}
```

---

## Android Notes

### StreamingAssets Extraction

On Android, model files are inside the APK and not directly accessible.
Use `InitializeAsync()` which automatically extracts files via
`StreamingAssetsCopier` on first launch.

### LocalZip Profiles

For `ModelSource.LocalZip` profiles, the model archive is extracted from
the APK to `persistentDataPath` on first launch. This saves APK size
but adds a one-time extraction delay.

### Microphone Permission

Add `android.permission.RECORD_AUDIO` to your AndroidManifest.xml.
`MicrophoneSource` requests permission automatically on Android 6+.

---

## API Reference

### IKwsService

| Member | Type | Description |
|--------|------|-------------|
| `IsReady` | `bool` | True when engine is loaded |
| `IsSessionActive` | `bool` | True when streaming session is active |
| `ActiveProfile` | `KwsProfile` | Currently loaded profile |
| `Settings` | `KwsSettingsData` | Loaded settings data |
| `Initialize()` | `void` | Sync init (Desktop only) |
| `InitializeAsync()` | `UniTask` | Async init (all platforms) |
| `LoadProfile(profile)` | `void` | Load a specific profile |
| `SwitchProfile(index)` | `void` | Switch by index |
| `SwitchProfile(name)` | `void` | Switch by name |
| `StartSession()` | `void` | Start streaming session |
| `StopSession()` | `void` | Stop streaming session |
| `AcceptSamples(samples, sampleRate)` | `void` | Feed audio samples |
| `ProcessAvailableFrames()` | `void` | Decode and check for keywords |
| `OnKeywordDetected` | `Action<KwsResult>` | Fires on keyword detection |
| `Dispose()` | `void` | Release all resources |

### KwsResult

| Member | Type | Description |
|--------|------|-------------|
| `Keyword` | `string` | The detected keyword text |
| `IsValid` | `bool` | True when Keyword is not null or empty |

### KwsProfile Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `profileName` | `string` | "New KWS Profile" | Display name |
| `modelType` | `KwsModelType` | Transducer | Model architecture |
| `modelSource` | `ModelSource` | Local | How model is bundled |
| `sampleRate` | `int` | 16000 | Audio sample rate |
| `featureDim` | `int` | 80 | Feature dimension |
| `numThreads` | `int` | 1 | Inference threads |
| `provider` | `string` | "cpu" | ONNX execution provider |
| `allowInt8` | `bool` | false | Allow INT8 quantized models |
| `tokens` | `string` | "" | Path to tokens.txt |
| `encoder` | `string` | "" | Path to encoder model |
| `decoder` | `string` | "" | Path to decoder model |
| `joiner` | `string` | "" | Path to joiner model |
| `keywordsFile` | `string` | "" | Path to keywords file |
| `customKeywords` | `string` | "" | Keywords text (keywords_buf) |
| `maxActivePaths` | `int` | 4 | Max active decoding paths |
| `numTrailingBlanks` | `int` | 1 | Trailing blank frames |
| `keywordsScore` | `float` | 1.0 | Global keyword score |
| `keywordsThreshold` | `float` | 0.25 | Global keyword threshold |

---

## Troubleshooting

### "KWS model directory not found"

Models not imported. Go to Project Settings > Sherpa-ONNX > KWS and import a model.

### "SHERPA_ONNX scripting define is not set"

Add `SHERPA_ONNX` to Player Settings > Scripting Define Symbols.
This is set automatically when you import the SherpaOnnx plugin.

### "KeywordSpotter created with null native handle"

The native constructor returned NULL. Check:
- Model files exist in the expected directory
- `tokens.txt`, encoder, decoder, joiner paths are correct
- Keywords file exists and is not empty (or custom keywords text is set)

### "keywords contain tokens not in the model vocabulary"

Keyword tokens must match the model's `tokens.txt`. Each model uses a different
tokenization scheme (BPE, ppinyin, phone). Using English BPE tokens with a Chinese
model (or vice versa) will be blocked. See [Per-Model Token Format](#per-model-token-format).

### No keywords detected

- Verify keywords file format: tokens must match the model's vocabulary
- Check `keywordsThreshold` — lower values increase sensitivity (default 0.25)
- Check `keywordsScore` — higher values favor keyword detection (default 1.0)
- Ensure microphone is recording and samples reach the engine

### INT8 model crashes

INT8 quantized models require platform support for INT8 ONNX operators.
Set `allowInt8 = true` only if your target device supports it.
Otherwise, use the default (non-INT8) model files.
