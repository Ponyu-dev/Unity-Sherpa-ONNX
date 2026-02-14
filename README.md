# Unity-Sherpa-ONNX

Unity integration plugin for [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx) — an open-source speech toolkit powered by ONNX Runtime.

## Feature Roadmap

| Feature | Status |
|---------|--------|
| Native library installer (Editor UI) | ✅ Done |
| Cross-platform native libraries | ✅ Done |
| Automatic PluginImporter configuration | ✅ Done |
| Version management & Update All | ✅ Done |
| Archive caching (Android, iOS) | ✅ Done |
| iOS managed DLL with `__Internal` binding | ✅ Done |
| `SHERPA_ONNX` scripting define symbol | ✅ Done |
| TTS model installer (Editor UI) | 🚧 In Progress |
| Text-to-Speech (TTS) | 📋 Planned |
| Speech Recognition (ASR) | 📋 Planned |

## Supported Platforms

| Platform | Architectures |
|----------|--------------|
| Windows | x64, x86, arm64 |
| macOS | x64, arm64 |
| Linux | x64, arm64 |
| Android | arm64-v8a, armeabi-v7a, x86, x86_64 |
| iOS | arm64, x86_64-simulator |

## Installation

### Via Unity Package Manager (UPM)

Add the package by git URL:

```
https://github.com/Ponyu-dev/Unity-Sherpa-ONNX.git?path=SherpaONNX/Packages/com.ponyudev.sherpa-onnx
```

Or clone the repository and reference it as a local package.

### Installing Native Libraries

1. Open **Edit → Project Settings → Sherpa ONNX**
2. Set the desired sherpa-onnx version (e.g. `1.12.24`)
3. Click **Install** for each platform you need
4. Use **Update All** when you change the version to update all installed libraries at once

Libraries are downloaded from:
- **Desktop** (Windows, macOS, Linux): [NuGet](https://www.nuget.org/packages?q=org.k2fsa.sherpa.onnx.runtime)
- **Android / iOS native**: [sherpa-onnx GitHub releases](https://github.com/k2-fsa/sherpa-onnx/releases)
- **iOS managed DLL**: this repository's [GitHub releases](https://github.com/Ponyu-dev/Unity-Sherpa-ONNX/releases) (see below)

## Why the iOS Managed DLL Is Hosted Here

On desktop and Android, Unity loads native code via dynamic libraries (`.dll`, `.so`, `.dylib`).
The managed C# binding (`sherpa-onnx.dll`) uses `DllImport("sherpa-onnx-c-api")` to find them at runtime.

iOS does **not** support dynamic loading. All native code must be statically linked into the app binary.
This means the managed DLL must use `DllImport("__Internal")` instead of `"sherpa-onnx-c-api"`.

The upstream sherpa-onnx NuGet package ships with the standard `"sherpa-onnx-c-api"` binding, which does not work on iOS.
To solve this, the `Tools/` scripts in this repository:

1. Take the official C# sources from `sherpa-onnx/scripts/dotnet/`
2. Patch `Dll.cs` to replace `"sherpa-onnx-c-api"` with `"__Internal"`
3. Build a custom `sherpa-onnx.dll` targeting `netstandard2.0`
4. Publish it as a GitHub release with tag `sherpa-v{version}`

The plugin's iOS install pipeline downloads this patched DLL automatically.

## Scripting Define Symbol

After installing any library, the plugin automatically adds **`SHERPA_ONNX`** to Scripting Define Symbols for all build targets. This allows you to guard runtime code that depends on sherpa-onnx:

```csharp
#if SHERPA_ONNX
    var recognizer = new OnlineRecognizer(config);
#endif
```

The define is removed automatically when all libraries are uninstalled.

## Requirements

- Unity 2021.3 or later
- `com.unity.sharp-zip-lib` 1.4.1+ (added automatically as a dependency)

## License

[Apache 2.0](LICENSE)
