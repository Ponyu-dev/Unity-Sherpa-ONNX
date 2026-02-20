# ASR Models Setup Guide

This guide covers how to configure speech recognition models using the Editor UI provided by **Unity-Sherpa-ONNX**.

The ASR settings window has two tabs — **Offline** (file-based recognition) and **Online** (real-time streaming). Both follow the same workflow.

## Opening the Settings

**Project Settings > Sherpa-ONNX > ASR**

Each tab has three areas:

- **Import section** — download and extract model archives by URL
- **Profile list** — manage multiple ASR profiles
- **Profile detail** — configure model paths, parameters, and runtime options

## Supported Model Types

### Offline (file recognition)

| Type | Description |
|------|-------------|
| Transducer | Zipformer / Conformer transducer models |
| Paraformer | Paraformer encoder-only models |
| Whisper | OpenAI Whisper models |
| SenseVoice | SenseVoice multilingual models |
| Moonshine | Moonshine multi-component models |
| NemoCtc | NVIDIA NeMo CTC models |
| ZipformerCtc | Zipformer CTC models |
| Tdnn | TDNN models |
| FireRedAsr | FireRedASR encoder-decoder models |
| Dolphin | Dolphin single-model ASR |
| Canary | Canary encoder-decoder with language selection |
| WenetCtc | WeNet CTC models |
| Omnilingual | Omnilingual single-model ASR |
| MedAsr | Medical ASR models |
| FunAsrNano | FunASR Nano LLM-based models |

### Online (streaming recognition)

| Type | Description |
|------|-------------|
| Transducer | Streaming Zipformer / Conformer transducer |
| Paraformer | Streaming Paraformer encoder-decoder |
| Zipformer2Ctc | Zipformer2 CTC streaming model |
| NemoCtc | NVIDIA NeMo CTC streaming model |
| ToneCtc | ToneCTC streaming model |

Model type is auto-detected from the archive name during import.

## Importing a Model

1. Select the **Offline** or **Online** tab
2. Click **Import from URL** to expand the import section
3. Paste the model archive URL (`.tar.bz2`, `.tar.gz`, or `.zip`)
4. Optionally enable **Use int8 models** if the archive contains quantized variants
5. Click **Import**

The importer downloads the archive, extracts it to `Assets/StreamingAssets/SherpaOnnx/asr-models/{name}/`, creates a profile, and auto-configures all model paths.

Pre-trained models are available at the
[sherpa-onnx ASR models page](https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models).

### Auto-Detection Keywords

| Archive name keyword | Detected type |
|----------------------|---------------|
| `whisper` | Whisper |
| `paraformer` | Paraformer |
| `sense-voice`, `sensevoice` | SenseVoice |
| `moonshine` | Moonshine |
| `fire-red`, `firered` | FireRedAsr |
| `dolphin` | Dolphin |
| `canary` | Canary |
| `wenet` | WenetCtc |
| `omnilingual` | Omnilingual |
| `med-asr`, `medasr` | MedAsr |
| `fun-asr`, `funasrnano` | FunAsrNano |
| `tdnn` | Tdnn |
| `nemo` + `ctc` | NemoCtc |
| `zipformer` + `ctc` | ZipformerCtc |
| `transducer`, `zipformer` | Transducer |

If detection from the archive name fails, the importer scans the extracted ONNX files as a fallback.

## Profile Management

### Creating a Profile

Click the **+** button below the profile list. A new profile is added and selected.

### Removing a Profile

Select a profile and click the **-** button. The profile and its model directory are deleted.

### Active Profile

Use the **Active profile** dropdown above the list to select which profile the runtime ASR system will use. This value is serialized to the settings JSON at build time.

## Profile Detail Fields

### Identity

| Field | Description |
|-------|-------------|
| Profile name | Display name; also used as the model folder name |
| Model type | Architecture type (see tables above) |

### Runtime

| Field | Default | Description |
|-------|---------|-------------|
| Threads | 1 | Number of inference threads |
| Provider | cpu | ONNX Runtime execution provider |
| Tokens | _(empty)_ | Path to `tokens.txt` file |

### Feature

| Field | Default | Description |
|-------|---------|-------------|
| Sample rate | 16000 | Expected input audio sample rate |
| Feature dim | 80 | Feature extraction dimension |

### Recognizer

| Field | Default | Description |
|-------|---------|-------------|
| Decoding method | greedy_search | `greedy_search` or `modified_beam_search` |
| Max active paths | 4 | Beam width for beam search |
| Hotwords file | _(empty)_ | Path to hotwords file |
| Hotwords score | 1.5 | Boost score for hotwords |
| Rule FSTs | _(empty)_ | Paths to `.fst` text normalization files (`\|`-separated) |
| Rule FARs | _(empty)_ | Paths to `.far` text normalization files (`\|`-separated) |
| Blank penalty | 0 | Penalty for blank tokens |

### Language Model (offline only)

| Field | Default | Description |
|-------|---------|-------------|
| LM model | _(empty)_ | Path to `lm.onnx` language model |
| LM scale | 0.5 | LM weight in decoding |

### Endpoint Detection (online only)

| Field | Default | Description |
|-------|---------|-------------|
| Enable endpoint | true | Detect speech endpoints automatically |
| Rule 1 min trailing silence | 1.2 | Seconds of silence after speech to trigger endpoint |
| Rule 2 min trailing silence | 2.4 | Seconds of silence (more conservative) |
| Rule 3 min utterance length | 20.0 | Max utterance length in seconds |

### Engine Pool Size (offline only)

| Field | Default | Description |
|-------|---------|-------------|
| Pool size | 1 | Number of concurrent native recognizer instances |

### Model-Specific Fields

Each model type shows its own section with paths to `.onnx` files and type-specific parameters. These fields are filled automatically during import.

## Auto-Configure

If a model directory exists, the **Auto-configure paths** button appears at the top of the detail panel. Clicking it scans the model folder and fills all path fields automatically:

- Finds `.onnx` model files (encoder, decoder, joiner, etc.)
- Locates `tokens.txt`
- Detects `.fst` and `.far` text normalization rules
- Finds `lm.onnx` language model
- Sets model-type-specific paths based on file names

## Int8 Model Switching

When both normal and int8-quantized `.onnx` files exist in the model directory, a toggle button appears:

- **Use int8 models** (blue) — switch to quantized variants for faster inference
- **Use normal models** (grey) — switch back to full-precision models

Int8 variants are detected by `int8` in the file name (e.g. `encoder.int8.onnx`). The switcher updates all relevant path fields and preserves other settings.

## File Structure

```
Assets/StreamingAssets/SherpaOnnx/
  asr-settings.json             # Offline ASR profiles and active index
  online-asr-settings.json      # Online ASR profiles and active index
  asr-models/
    {profileName}/              # Model files for each profile
      encoder-epoch-99-avg-1.onnx
      decoder-epoch-99-avg-1.onnx
      joiner-epoch-99-avg-1.onnx
      tokens.txt
      ...
```

Both offline and online profiles share the same `asr-models/` directory.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Auto-configure paths" button missing | Import or manually place model files in the profile's model directory |
| Int8 switch button not shown | No int8 variant found; ensure both `model.onnx` and `model.int8.onnx` exist |
| Import fails with network error | Check network connection; model archives are hosted on GitHub releases |
| Engine fails to load at runtime | Verify model paths in the JSON settings file match actual file locations |
| Wrong model type detected | Change the model type manually in the profile detail panel, then click Auto-configure |
