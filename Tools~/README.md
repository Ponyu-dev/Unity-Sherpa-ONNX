# Tools

Build, release, and registration scripts for the Unity-Sherpa-ONNX package.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/) (for `dotnet build` — iOS DLL only)
- [GitHub CLI](https://cli.github.com/) (`gh`) — authenticated with push access to this repository

## Scripts

### create_release.sh

Creates a GitHub release with a git tag and attaches the `.unitypackage` installer.

```bash
./Tools~/create_release.sh
```

**What it does:**
1. Checks for uncommitted changes
2. Extracts release notes from `CHANGELOG.md` for the current version
3. Builds the `.unitypackage` installer (via `build_installer.sh`)
4. Creates git tag `v{version}` and pushes it
5. Creates a GitHub Release with the `.unitypackage` attached

### build_installer.sh

Builds a small `.unitypackage` containing an editor script that adds the OpenUPM scoped registry to `manifest.json`.

```bash
./Tools~/build_installer.sh
```

**What it does:**
1. Packages `Installer/SherpaOnnxInstaller.cs` into a `.unitypackage`
2. Outputs `Tools~/output/SherpaOnnxInstaller.unitypackage`

**Source:** `Tools~/Installer/SherpaOnnxInstaller.cs` — an `[InitializeOnLoad]` editor script that:
- Adds the OpenUPM scoped registry to `Packages/manifest.json`
- Adds `com.ponyudev.sherpa-onnx` as a dependency
- Shows a confirmation dialog before making changes
- Deletes itself after successful installation

### register_openupm.sh

Registers the package on [OpenUPM](https://openupm.com/) by creating a PR to the openupm/openupm repository.

```bash
./Tools~/register_openupm.sh
```

**What it does:**
1. Checks that the repository is public
2. Forks `openupm/openupm` (if not already forked)
3. Creates a YAML package definition in `data/packages/`
4. Pushes a branch and creates a PR

### build_ios_dll.sh

Builds the patched `sherpa-onnx.dll` for iOS.

```bash
./Tools~/build_ios_dll.sh /path/to/sherpa-onnx
```

**What it does:**
1. Reads `SHERPA_ONNX_VERSION` from `sherpa-onnx/CMakeLists.txt`
2. Copies C# sources from `sherpa-onnx/scripts/dotnet/`
3. Patches `Dll.cs`: replaces `"sherpa-onnx-c-api"` → `"__Internal"`
4. Builds with `dotnet build -c Release` (target: `netstandard2.0`)
5. Outputs `Tools~/output/sherpa-onnx.dll` and `Tools~/output/sherpa-onnx.zip`

### publish_release.sh

Creates a GitHub release for the patched iOS DLL.

```bash
./Tools~/publish_release.sh /path/to/sherpa-onnx
```

**What it does:**
1. Reads version from `CMakeLists.txt`
2. Checks if release `sherpa-v{version}` already exists — skips if so
3. Creates a new tag and release, uploads `Tools~/output/sherpa-onnx.zip`

**Tag format:** `sherpa-v{version}` (e.g. `sherpa-v1.12.23`) — prefixed with `sherpa-` to avoid conflicts with UPM plugin version tags.

### build_and_publish.sh

Runs iOS DLL build and publish in sequence.

```bash
./Tools~/build_and_publish.sh /path/to/sherpa-onnx
```

## Typical Workflows

### New plugin release

```bash
# 1. Update VERSION in create_release.sh and CHANGELOG.md
# 2. Commit and push all changes
# 3. Run:
./Tools~/create_release.sh
```

### New sherpa-onnx version (iOS DLL update)

```bash
cd /path/to/sherpa-onnx
git pull

cd /path/to/Unity-Sherpa-ONNX
./Tools~/build_and_publish.sh ../sherpa-onnx
```

## Directory Structure

```
Tools~/
├── Installer/
│   └── SherpaOnnxInstaller.cs   # OpenUPM installer editor script
├── output/                       # Build artifacts (not committed)
│   ├── SherpaOnnxInstaller.unitypackage
│   ├── sherpa-onnx.dll
│   └── sherpa-onnx.zip
├── .build-ios/                   # Temporary build dir (cleaned automatically)
├── build_installer.sh            # Build .unitypackage installer
├── create_release.sh             # Create GitHub Release with tag
├── register_openupm.sh           # Register package on OpenUPM
├── build_ios_dll.sh              # Build patched iOS DLL
├── publish_release.sh            # Publish iOS DLL release
├── build_and_publish.sh          # Build + publish iOS DLL
└── README.md                     # This file
```
