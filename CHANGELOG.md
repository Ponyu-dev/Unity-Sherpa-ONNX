# Changelog

All notable changes to `com.ponyudev.sherpa-onnx` will be documented in this file.

## [0.1.0] - 2026-02-21

First public release with TTS, ASR, and VAD support.

### Added

#### Text-to-Speech (TTS)
- 6 model architectures: Vits (Piper), Matcha, Kokoro, Kitten, ZipVoice, Pocket (voice cloning)
- Editor UI for model import with auto-detection and auto-configuration
- Int8 quantization toggle
- Flexible deployment: Local (StreamingAssets), Remote (runtime download), LocalZip
- Matcha vocoder selector with independent download
- Cache pooling for audio buffers, AudioClips, and AudioSources

#### Speech Recognition (ASR)
- 15 offline architectures: Zipformer, Paraformer, Whisper, SenseVoice, Moonshine, and more
- 5 online (streaming) architectures with real-time microphone capture
- Partial and final result callbacks
- Engine pool for concurrent offline recognizer instances
- Endpoint detection with configurable silence rules

#### Voice Activity Detection (VAD)
- 2 model architectures: Silero VAD, TEN-VAD
- Configurable parameters: threshold, min silence/speech duration, window size
- VAD + ASR pipeline for segment-based recognition

#### Library Installer
- One-click native library install for Windows, macOS, Linux, Android, and iOS
- Update All to re-install every platform at once when changing version
- Desktop libraries from NuGet, mobile from sherpa-onnx GitHub releases
- Patched iOS managed DLL with `DllImport("__Internal")` from this repo's releases

#### Model Importer
- One-click import from URL (tar.gz, tar.bz2, zip)
- Auto-detection of model type and architecture
- Auto-configuration of model paths and profile settings

#### Platform Fixes
- Android: native `AudioRecord` fallback when Unity Microphone returns silence
- Android: StreamingAssets extraction to `persistentDataPath` with version tracking
- Android: locale guard (`LC_NUMERIC = "C"`) to prevent float parsing crashes
- iOS: xcframework architecture filtering (device vs simulator)
- Unity: silent AudioSource workaround to force microphone recording
- Unity: `Microphone.GetPosition()` polling with configurable timeout
- All: built-in sample rate resampler (any rate → 16 kHz)
- Android/iOS: async microphone permission request via UniTask

## [0.0.1] - Initial

- Initial package skeleton.
