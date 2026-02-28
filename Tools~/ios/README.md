# iOS Build Scripts

Build and publish iOS artifacts (native xcframeworks + managed DLL) for Unity-Sherpa-ONNX.

## Prerequisites

- macOS with Xcode Command Line Tools
- [.NET SDK](https://dotnet.microsoft.com/) (for `dotnet build`)
- [GitHub CLI](https://cli.github.com/) (`gh`) — for publishing releases
- A local clone of [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx)

## Scripts

### build_ios.sh

Full iOS build pipeline. Runs `build_ios_lib.sh` + `build_ios_dll.sh`, then packages everything into a single zip.

```bash
./Tools~/ios/build_ios.sh /path/to/sherpa-onnx
```

**Output:** `Tools~/output/sherpa-onnx-ios.zip` containing:
- `sherpa-onnx.dll` — managed C# with `__Internal` P/Invoke
- `sherpa-onnx.xcframework/` — native static library (device + simulator)
- `onnxruntime.xcframework/` — ONNX Runtime (from sherpa-onnx build)

### build_ios_lib.sh

Builds native `libsherpa-onnx.a` static library for iOS (device + simulator) with the **kiss_fft symbol-prefix fix** (required for Unity). The sherpa-onnx repo is used read-only — the fix is applied via `CMAKE_C_FLAGS`.

```bash
./Tools~/ios/build_ios_lib.sh /path/to/sherpa-onnx
```

**What it does:**
1. Checks onnxruntime in the repo's `build-ios/` (runs `build-ios.sh` automatically if missing)
2. Builds sherpa-onnx for arm64 device, arm64 simulator, x86_64 simulator
3. Applies kiss_fft symbol rename via `CMAKE_C_FLAGS` (repo is NOT modified)
4. Creates universal simulator binary via `lipo`
5. Merges all static libraries into a single `libsherpa-onnx.a` via `libtool`
6. Verifies kiss_fft symbols are prefixed (`sherpa_kiss_fft`) and no unprefixed symbols remain
7. Creates `Tools~/output/sherpa-onnx.xcframework/` and `Tools~/output/onnxruntime.xcframework/`

**Why the kiss_fft fix is needed:** Unity's `libiPhone-lib.a` contains its own `kiss_fft` with a different `kiss_fft_state` struct layout. Without prefixing, the linker resolves all `kiss_fft` calls to Unity's version, causing `EXC_BAD_ACCESS` in sherpa-onnx's fbank feature extraction.

### build_ios_dll.sh

Builds the patched `sherpa-onnx.dll` for iOS.

```bash
./Tools~/ios/build_ios_dll.sh /path/to/sherpa-onnx
```

**What it does:**
1. Reads `SHERPA_ONNX_VERSION` from `sherpa-onnx/CMakeLists.txt`
2. Copies C# sources from `sherpa-onnx/scripts/dotnet/`
3. Patches `Dll.cs`: replaces `"sherpa-onnx-c-api"` with `"__Internal"`
4. Builds with `dotnet build -c Release` (target: `netstandard2.0`)
5. Outputs `Tools~/output/sherpa-onnx.dll`

### publish_release.sh

Creates a GitHub release for the iOS artifacts zip.

```bash
./Tools~/ios/publish_release.sh /path/to/sherpa-onnx
```

**What it does:**
1. Reads version from `CMakeLists.txt`
2. Checks if release `sherpa-v{version}` already exists — skips if so
3. Creates a new tag and release, uploads `Tools~/output/sherpa-onnx-ios.zip`

**Tag format:** `sherpa-v{version}` (e.g. `sherpa-v1.12.25`) — prefixed with `sherpa-` to avoid conflicts with UPM plugin version tags.

### build_and_publish.sh

Runs full iOS build and publish in sequence.

```bash
./Tools~/ios/build_and_publish.sh /path/to/sherpa-onnx
```
