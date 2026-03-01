# KWS Models Setup Guide

This guide covers how to configure Keyword Spotting models using the Editor UI provided by **Unity-Sherpa-ONNX**.

For runtime usage examples, see [KWS Runtime Usage](kws-runtime-usage.md).

## Opening the Settings

**Project Settings > Sherpa-ONNX > KWS**

The KWS settings window has three areas:

- **Import section** — download a KWS model archive by URL
- **Profile list** — manage multiple KWS profiles
- **Profile detail** — configure model paths, keywords, thresholds, and runtime options

## Supported Model Types

| Type | Architecture | Description |
|------|-------------|-------------|
| Transducer | Zipformer | Encoder + Decoder + Joiner — all pre-trained KWS models use this architecture |

Model type is auto-detected from the archive name during import.

### Auto-Detection Keywords

| Archive name keyword | Detected type |
|----------------------|---------------|
| `kws` | Transducer |
| `zipformer` | Transducer |

## Importing a Model

KWS models are distributed as archives (`.tar.bz2`) containing multiple files.

1. Click **Import from URL** to expand the import section
2. Paste the model archive URL
3. Click **Import**

The importer downloads and extracts the archive to `Assets/StreamingAssets/SherpaOnnx/kws-models/{name}/`, creates a profile, and auto-configures all model paths.

### Download URLs

Pre-trained models are available from the sherpa-onnx releases:

**sherpa-onnx-kws-zipformer-zh-en (Chinese & English):**
```
https://github.com/k2-fsa/sherpa-onnx/releases/download/kws-models/sherpa-onnx-kws-zipformer-zh-en-3M-2025-12-20.tar.bz2
```

**sherpa-onnx-kws-zipformer-wenetspeech (Chinese):**
```
https://github.com/k2-fsa/sherpa-onnx/releases/download/kws-models/sherpa-onnx-kws-zipformer-wenetspeech-3.3M-2024-01-01.tar.bz2
```

**sherpa-onnx-kws-zipformer-gigaspeech (English):**
```
https://github.com/k2-fsa/sherpa-onnx/releases/download/kws-models/sherpa-onnx-kws-zipformer-gigaspeech-3.3M-2024-01-01.tar.bz2
```

Each model uses a different tokenization — see [Per-Model Tokenization](#per-model-tokenization) for details on writing keywords.

For more information, see the
[sherpa-onnx KWS documentation](https://k2-fsa.github.io/sherpa/onnx/kws/index.html#pretrained-models).

## Include KWS in Build

The **Include KWS in Build** toggle at the top of the settings controls whether
KWS model files and settings are included in the build output:

- **Enabled** (default) — KWS settings JSON and `kws-models/` directory are included
- **Disabled** — KWS files are excluded from the build, reducing app size

## Profile Management

### Creating a Profile

Click the **+** button below the profile list. A new profile is added and selected.

### Removing a Profile

Select a profile and click the **-** button. The profile and its model directory are deleted.

### Active Profile

Use the **Active profile** dropdown above the list to select which profile the runtime KWS system will use. This value is serialized to `kws-settings.json` at build time.

## Profile Detail Fields

### Identity

| Field | Description |
|-------|-------------|
| Profile name | Display name; also used as the model folder name |
| Model type | Transducer (only supported type) |

### Transducer Model Files

| Field | Description |
|-------|-------------|
| Tokens | Path to `tokens.txt` (required) |
| Encoder | Path to encoder ONNX model (required) |
| Decoder | Path to decoder ONNX model (required) |
| Joiner | Path to joiner ONNX model (required) |

### Keywords

Keywords define what the model listens for. At least one source is required:

| Field | Description |
|-------|-------------|
| Keywords file | Path to a keywords file inside the model directory |
| Custom keywords | Multiline text field for additional keywords (passed as `keywords_buf` at runtime) |

Either the keywords file or the custom keywords text must be provided — or both.

#### Keywords Format

Each line defines one keyword in token format:

```
token1 token2 token3 @TAG :boost #threshold
```

- **Tokens** (required): space-separated tokens from the model's `tokens.txt` vocabulary
- **@TAG** (optional): identifier string returned on detection
- **:boost** (optional): keyword-specific score multiplier (e.g. `:1.5`)
- **#threshold** (optional): keyword-specific detection threshold (e.g. `#0.3`)

**Important:** Tokens must exist in the model's `tokens.txt`. Each model uses a different tokenization scheme — using tokens from the wrong model will prevent loading.

#### Per-Model Tokenization

| Model | Language | Token type | Example keyword |
|-------|----------|------------|-----------------|
| gigaspeech | English | BPE subwords | `▁HE LL O ▁WORLD @HELLO` |
| wenetspeech | Chinese | ppinyin | `n ǐ h ǎo @你好` |
| zh-en | Chinese + English | phone + ppinyin | `HH AH0 L OW1 @HELLO` (EN) / `n ǐ h ǎo @你好` (ZH) |

**BPE models (gigaspeech):** Tokens are BPE subword units. Word boundaries use `▁` (U+2581). Generate with:
```
sherpa-onnx-cli text2token --tokens-type bpe --bpe-model bpe.model
```

**ppinyin models (wenetspeech):** Tokens are pinyin syllables with tone diacriticals. `@TAG` with original text is required. Generate with:
```
sherpa-onnx-cli text2token --tokens-type ppinyin
```

**phone+ppinyin models (zh-en):** English uses ARPAbet phonemes, Chinese uses ppinyin. `@TAG` is required. Generate with:
```
sherpa-onnx-cli text2token --tokens-type phone+ppinyin --lexicon en.phone
```

#### Token Validation

The Editor validates custom keywords against the model vocabulary in real-time:
- Format errors (missing tokens, invalid boost/threshold) are shown as warnings
- Tokens not found in `tokens.txt` are highlighted with the specific invalid token

At runtime, keywords are validated before the native engine loads. If invalid tokens are found, loading is blocked with a detailed error log to prevent crashes.

### Detection Parameters

| Field | Default | Description |
|-------|---------|-------------|
| Max active paths | 4 | Maximum number of active decoding paths |
| Num trailing blanks | 1 | Number of trailing blank frames |
| Keywords score | 1.0 | Global score for keyword detection |
| Keywords threshold | 0.25 | Global detection threshold (lower = more sensitive) |

### Runtime

| Field | Default | Description |
|-------|---------|-------------|
| Sample rate | 16000 | Expected input audio sample rate in Hz |
| Feature dim | 80 | Feature dimension for the model |
| Threads | 1 | Number of inference threads |
| Provider | cpu | ONNX Runtime execution provider |
| Allow INT8 | false | Allow loading INT8 quantized models (see INT8 section) |

## INT8 Model Switching

If INT8 quantized variants of the model files exist in the model directory (files with `int8` in their names), a **Use INT8 models** button appears in the profile detail.

Clicking it switches encoder, decoder, and joiner paths to the INT8 variants. The original (float32) paths can be restored by clicking the button again.

**Warning:** INT8 models require platform support for INT8 ONNX operators. Enable `allowInt8` only if your target device supports INT8 inference, otherwise the engine will fail to load.

## Auto-Configure

If a model directory exists, the **Auto-configure paths** button appears at the top of the detail panel. Clicking it scans the model folder and fills all model paths (tokens, encoder, decoder, joiner, keywords file) automatically.

## Model Source & Remote

| Field | Description |
|-------|-------------|
| Model source | `Local` (bundled in StreamingAssets), `Remote` (downloaded at runtime), or `LocalZip` (zipped at build time, extracted on first launch) |
| Source URL | Download URL for re-importing or remote download |
| Remote base URL | Base URL for remote model file downloads at runtime |

## Version Requirements

KWS Transducer models are supported since **sherpa-onnx v1.9.25**. If the installed sherpa-onnx version is older, a warning is shown in the profile detail.

## File Structure

```
Assets/StreamingAssets/SherpaOnnx/
  kws-settings.json              # Serialized profiles and active index
  kws-models/
    {profileName}/               # Model files for each profile
      tokens.txt
      encoder-epoch-XX-avg-X.onnx
      decoder-epoch-XX-avg-X.onnx
      joiner-epoch-XX-avg-X.onnx
      keywords.txt
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Auto-configure paths" button missing | Import or manually place model files in the profile's model directory |
| Import fails | Ensure the URL points to a `.tar.bz2` archive (KWS models are archives, not single files) |
| "(incomplete)" marker on profile | Fill in all required fields: tokens, encoder, decoder, joiner, and at least one keywords source |
| "(missing files)" marker on profile | Model files were deleted from disk; re-import the model |
| Version warning shown | Update sherpa-onnx plugin to v1.9.25 or later |
| INT8 button not shown | No INT8 model variants found in the model directory |
| Keywords validation warnings | Check token format: each line needs space-separated tokens; @TAG, :boost, #threshold are optional suffixes |
| "token not found in model vocabulary" | Keyword tokens must match the model's `tokens.txt`. Each model uses different tokenization — see [Per-Model Tokenization](#per-model-tokenization). Use `sherpa-onnx text2token` to convert text to correct format |
| Unity crashes on Play | Keywords contain tokens not in the model vocabulary. The Editor now validates tokens and blocks loading. Re-check custom keywords against the model's token scheme |
