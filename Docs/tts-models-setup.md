# TTS Models Setup Guide

This guide covers how to configure text-to-speech models using the Editor UI provided by **Unity-Sherpa-ONNX**.

## Opening the Settings

**Project Settings > Sherpa-ONNX > TTS**

The TTS settings window has three areas:

- **Import section** — download and extract model archives by URL
- **Profile list** — manage multiple TTS profiles
- **Profile detail** — configure model paths, parameters, and deployment options

## Supported Model Types

| Type | Description |
|------|-------------|
| Vits | VITS-based models (including Piper voices) |
| Matcha | MatchaTTS acoustic model + vocoder |
| Kokoro | Kokoro multi-voice model |
| Kitten | Kitten TTS model |
| ZipVoice | Zipformer-based voice synthesis |
| Pocket | PocketTTS multi-component model |

Model type is auto-detected from the archive name during import:

| Archive name prefix / keyword | Detected type |
|-------------------------------|---------------|
| `vits-*` | Vits |
| `matcha-*` | Matcha |
| `kokoro-*` | Kokoro |
| `*kitten*` | Kitten |
| `*zipformer*`, `*zip-voice*`, `*zipvoice*` | ZipVoice |
| `*pocket*` | Pocket |

## Importing a Model

1. Click **Import from URL** to expand the import section
2. Paste the model archive URL (`.tar.bz2`, `.tar.gz`, or `.zip`)
3. For **Matcha** models, select a vocoder from the dropdown (Vocos 22 kHz is recommended)
4. Optionally enable **Use int8 models** if the archive contains quantized variants
5. Click **Import**

The importer downloads the archive, extracts it to `Assets/StreamingAssets/SherpaOnnx/tts-models/{name}/`, creates a profile, and auto-configures all model paths.

Pre-trained models are available at the
[sherpa-onnx TTS models page](https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models).

## Profile Management

### Creating a Profile

Click the **+** button below the profile list. A new profile named "New Profile" is added and selected.

### Removing a Profile

Select a profile and click the **-** button. The profile and its model directory are deleted.

### Active Profile

Use the **Active profile** dropdown above the list to select which profile the runtime TTS system will use. This value is serialized to `tts-settings.json` at build time.

## Profile Detail Fields

### Identity

| Field | Description |
|-------|-------------|
| Profile name | Display name; also used as the model folder name |
| Model type | Vits, Matcha, Kokoro, Kitten, ZipVoice, or Pocket |
| Model source | Local, Remote, or LocalZip (see [Deployment Options](#deployment-options)) |

### Generation

| Field | Default | Description |
|-------|---------|-------------|
| Speaker ID | 0 | Speaker index for multi-speaker models |
| Speed | 1.0 | Playback speed multiplier |

### Text Processing

| Field | Default | Description |
|-------|---------|-------------|
| Rule FSTs | _(empty)_ | Comma-separated paths to `.fst` text normalization files |
| Rule FARs | _(empty)_ | Comma-separated paths to `.far` text normalization files |
| Max sentences | 1 | Maximum sentences processed per call |
| Silence scale | 0.2 | Scale factor for silence between sentences |

### Runtime

| Field | Default | Description |
|-------|---------|-------------|
| Threads | 1 | Number of inference threads |
| Provider | cpu | ONNX Runtime execution provider |

### Model-Specific Fields

Each model type shows its own section with paths to `.onnx` files, token files, lexicons, and type-specific parameters (noise scale, length scale, etc.). These fields are filled automatically during import.

## Auto-Configure

If a model directory exists, the **Auto-configure paths** button appears at the top of the detail panel. Clicking it scans the model folder and fills all path fields automatically:

- Finds `.onnx` model files
- Locates `tokens.txt`, lexicon files, `voices.bin`
- Detects `espeak-ng-data` and `dict` subdirectories
- Finds `.fst` and `.far` text normalization rules
- Sets default scale parameters for the detected model type

## Int8 Model Switching

When both normal and int8-quantized `.onnx` files exist in the model directory, a toggle button appears:

- **Use int8 models** (blue) — switch to quantized variants for faster inference
- **Use normal models** (grey) — switch back to full-precision models

Int8 variants are detected by `int8` in the file name (e.g. `model.int8.onnx`). The switcher updates the relevant path fields and preserves all other settings.

## Matcha Vocoder Selection

Matcha models require a separate vocoder. The vocoder selector appears in the Matcha settings section and during import:

| Vocoder | Description |
|---------|-------------|
| Vocos 22 kHz | Recommended; fast and compact |
| HiFi-GAN v1 | Classic HiFi-GAN vocoder |
| HiFi-GAN v2 | Improved variant |
| HiFi-GAN v3 | Latest variant |

Click **Download** to fetch the selected vocoder. The old vocoder file is replaced automatically.

## Deployment Options

### Local (default)

Model files stay in `Assets/StreamingAssets/SherpaOnnx/tts-models/{profileName}/` and are included in the build as-is.

### Remote

Set the **Base URL** in the Remote section. At runtime, the app downloads the model archive from:

```
{baseUrl}/{profileName}.zip
```

Use this when models are too large to ship with the app binary.

### LocalZip

Model files are zipped automatically at build time and placed in StreamingAssets. On first launch, the app extracts the archive to `persistentDataPath`.

Use the **Pack to zip (test)** button to verify the zip process in the Editor. **Delete zip** removes the test archive.

The build processor handles zip/restore automatically:
- **Pre-build**: zips the model folder, backs up originals
- **Post-build**: restores originals, removes zip files

## Cache Settings

The cache section configures object pooling for TTS playback:

| Pool | Default size | Description |
|------|-------------|-------------|
| OfflineTts | 4 | Raw audio buffer pool (float arrays) |
| AudioClip | 4 | Unity AudioClip object pool |
| AudioSource | 2 | AudioSource component pool for parallel playback |

Each pool can be enabled or disabled independently.

## File Structure

```
Assets/StreamingAssets/SherpaOnnx/
  tts-settings.json          # Serialized profiles and cache config
  tts-models/
    {profileName}/           # Model files for each profile
      model.onnx
      tokens.txt
      ...
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Auto-configure paths" button missing | Import or manually place model files in the profile's model directory |
| Int8 switch button not shown | No int8 variant found; ensure both `model.onnx` and `model.int8.onnx` exist |
| Vocoder download fails | Check network connection; vocoder files are hosted on GitHub releases |
| Pack to zip fails | Ensure the model directory exists and contains files |
