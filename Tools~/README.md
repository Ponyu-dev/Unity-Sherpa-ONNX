# Tools

Build, release, and registration scripts for the Unity-Sherpa-ONNX package.

## Directory Structure

```
Tools~/
├── ios/                    # iOS build & publish scripts
│   ├── build_ios.sh        # Full iOS build (native + managed → zip)
│   ├── build_ios_lib.sh    # Native static libraries → xcframeworks
│   ├── build_ios_dll.sh    # Managed C# DLL (→ __Internal P/Invoke)
│   ├── publish_release.sh  # Upload zip as GitHub release
│   └── build_and_publish.sh# Build + publish in one step
├── unity/                  # Unity package scripts
│   ├── Installer/          # OpenUPM installer editor script
│   ├── build_installer.sh  # Build .unitypackage installer
│   ├── create_release.sh   # Create UPM GitHub release with tag
│   └── register_openupm.sh # Register package on OpenUPM
├── output/                 # Build artifacts (not committed)
└── README.md               # This file
```

See `ios/README.md` and `unity/README.md` for detailed usage.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/) (for `dotnet build` — iOS DLL only)
- [GitHub CLI](https://cli.github.com/) (`gh`) — authenticated with push access to this repository
- Xcode Command Line Tools (for iOS native builds)

## Typical Workflows

### New sherpa-onnx version (iOS update)

```bash
cd /path/to/sherpa-onnx && git pull
cd /path/to/Unity-Sherpa-ONNX
./Tools~/ios/build_and_publish.sh ../sherpa-onnx
```

### New plugin release

```bash
# 1. Update VERSION in create_release.sh and CHANGELOG.md
# 2. Commit and push all changes
# 3. Run:
./Tools~/unity/create_release.sh
```
