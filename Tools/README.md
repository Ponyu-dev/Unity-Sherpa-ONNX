# Tools

Build and release scripts for the iOS managed DLL.

These scripts exist because iOS requires a patched version of `sherpa-onnx.dll` where `DllImport("sherpa-onnx-c-api")` is replaced with `DllImport("__Internal")` for static linking. The upstream NuGet package does not provide this, so we build and host it ourselves.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/) (for `dotnet build`)
- [GitHub CLI](https://cli.github.com/) (`gh`) — authenticated with push access to this repository
- A local clone of [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx)

## Scripts

### build_ios_dll.sh

Builds the patched `sherpa-onnx.dll` for iOS.

```bash
./Tools/build_ios_dll.sh /path/to/sherpa-onnx
```

**What it does:**
1. Reads `SHERPA_ONNX_VERSION` from `sherpa-onnx/CMakeLists.txt`
2. Copies C# sources from `sherpa-onnx/scripts/dotnet/`
3. Patches `Dll.cs`: replaces `"sherpa-onnx-c-api"` → `"__Internal"`
4. Builds with `dotnet build -c Release` (target: `netstandard2.0`)
5. Outputs `Tools/output/sherpa-onnx.dll` and `Tools/output/sherpa-onnx.zip`

### publish_release.sh

Creates a GitHub release and uploads the built zip.

```bash
./Tools/publish_release.sh /path/to/sherpa-onnx
```

**What it does:**
1. Reads version from `CMakeLists.txt`
2. Checks if release `sherpa-v{version}` already exists — skips if so
3. Creates a new tag and release, uploads `Tools/output/sherpa-onnx.zip`

**Tag format:** `sherpa-v{version}` (e.g. `sherpa-v1.12.24`) — prefixed with `sherpa-` to avoid conflicts with UPM plugin version tags.

**Download URL used by the plugin:**
```
https://github.com/Ponyu-dev/Unity-Sherpa-ONNX/releases/download/sherpa-v{version}/sherpa-onnx.zip
```

### build_and_publish.sh

Runs both scripts in sequence (build → publish).

```bash
./Tools/build_and_publish.sh /path/to/sherpa-onnx
```

## Typical Workflow

When a new sherpa-onnx version is released:

```bash
cd /path/to/sherpa-onnx
git pull

cd /path/to/Unity-Sherpa-ONNX
./Tools/build_and_publish.sh ../sherpa-onnx
```

This builds the patched DLL and publishes it. After that, update the default version in `SherpaOnnxProjectSettings.cs` and the plugin will download the new DLL automatically during iOS install.

## Output

```
Tools/
├── output/
│   ├── sherpa-onnx.dll    # Patched managed DLL (not committed)
│   └── sherpa-onnx.zip    # Archive uploaded to GitHub releases
└── .build-ios/            # Temporary build directory (cleaned automatically)
```
