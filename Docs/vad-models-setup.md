# VAD Models Setup Guide

This guide covers how to configure Voice Activity Detection models using the Editor UI provided by **Unity-Sherpa-ONNX**.

For runtime usage examples, see [VAD Runtime Usage](vad-runtime-usage.md).

## Opening the Settings

**Project Settings > Sherpa-ONNX > VAD**

The VAD settings window has three areas:

- **Import section** — download a VAD model file by URL
- **Profile list** — manage multiple VAD profiles
- **Profile detail** — configure model paths, thresholds, and runtime options

## Supported Model Types

| Type | Window Size | Description |
|------|-------------|-------------|
| SileroVad | 512 samples | Silero VAD — compact and widely used |
| TenVad | 256 samples | TEN-VAD — alternative lightweight model |

Model type is auto-detected from the file name during import.

### Auto-Detection Keywords

| File name keyword | Detected type |
|-------------------|---------------|
| `silero` | SileroVad |
| `ten` | TenVad |

## Importing a Model

VAD models are distributed as single `.onnx` files (not archives).

1. Click **Import from URL** to expand the import section
2. Paste the model file URL (direct link to `.onnx` file)
3. Click **Import**

The importer downloads the file to `Assets/StreamingAssets/SherpaOnnx/vad-models/{name}/`, creates a profile, and auto-configures the model path.

### Download URLs

Pre-trained models are available from the sherpa-onnx releases:

**Silero VAD:**
```
https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/silero_vad.onnx
```

**TEN-VAD:**
```
https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/ten-vad.onnx
```

For more information, see the
[sherpa-onnx VAD documentation](https://k2-fsa.github.io/sherpa/onnx/vad/index.html).

## Include VAD in Build

The **Include VAD in Build** toggle at the top of the settings controls whether
VAD model files and settings are included in the build output:

- **Enabled** (default) — VAD settings JSON and `vad-models/` directory are included
- **Disabled** — VAD files are excluded from the build, reducing app size

This is useful when VAD is only needed during development or when you want to
ship separate builds with and without VAD support.

## Profile Management

### Creating a Profile

Click the **+** button below the profile list. A new profile is added and selected.

### Removing a Profile

Select a profile and click the **-** button. The profile and its model directory are deleted.

### Active Profile

Use the **Active profile** dropdown above the list to select which profile the runtime VAD system will use. This value is serialized to `vad-settings.json` at build time.

## Profile Detail Fields

### Identity

| Field | Description |
|-------|-------------|
| Profile name | Display name; also used as the model folder name |
| Model type | SileroVad or TenVad |

### Thresholds

| Field | Default | Description |
|-------|---------|-------------|
| Threshold | 0.5 | Speech probability threshold (0.0–1.0). Lower values detect more speech |
| Min silence duration | 0.5 | Minimum silence duration (seconds) to end a speech segment |
| Min speech duration | 0.25 | Minimum speech duration (seconds) to count as valid speech |
| Max speech duration | 5.0 | Maximum speech segment length (seconds); longer speech is split |

### Runtime

| Field | Default | Description |
|-------|---------|-------------|
| Sample rate | 16000 | Expected input audio sample rate in Hz |
| Window size | 512 | Samples per detection window (512 for Silero, 256 for TEN-VAD) |
| Threads | 1 | Number of inference threads |
| Provider | cpu | ONNX Runtime execution provider |
| Buffer size (seconds) | 60 | Internal circular buffer capacity |

### Model

| Field | Description |
|-------|-------------|
| Model | Path to the `.onnx` model file (e.g. `silero_vad.onnx`) |

## Auto-Configure

If a model directory exists, the **Auto-configure paths** button appears at the top of the detail panel. Clicking it scans the model folder and fills the model path automatically by finding the first `.onnx` file.

## Window Size and Model Type

When changing the model type in the profile detail, the window size is automatically adjusted:

- **SileroVad** → 512 samples
- **TenVad** → 256 samples

Using the wrong window size for a model type will produce incorrect results.

## File Structure

```
Assets/StreamingAssets/SherpaOnnx/
  vad-settings.json            # Serialized profiles and active index
  vad-models/
    {profileName}/             # Model file for each profile
      silero_vad.onnx
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Auto-configure paths" button missing | Import or manually place the `.onnx` file in the profile's model directory |
| Import fails | Ensure the URL points directly to an `.onnx` file, not an archive |
| Wrong model type detected | Change the model type manually in the profile detail panel |
| Engine fails to load at runtime | Verify the model path in `vad-settings.json` matches the actual file location |
| Detection seems off | Adjust the threshold: lower values (e.g. 0.3) are more sensitive, higher values (e.g. 0.7) are stricter |
| Segments too short or fragmented | Increase `minSilenceDuration` to merge nearby speech segments |
| Segments too long | Decrease `maxSpeechDuration` to split long utterances |
| Wrong window size | Ensure SileroVad uses 512 and TenVad uses 256; auto-adjusts on model type change |
