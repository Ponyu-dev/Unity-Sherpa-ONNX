#!/usr/bin/env bash
# Create a GitHub release and upload sherpa-onnx-ios.zip if it doesn't exist yet.
#
# Reads version from the sherpa-onnx repo CMakeLists.txt.
# Requires: gh (GitHub CLI) authenticated.
#
# Usage:
#   ./Tools~/ios/publish_release.sh /path/to/sherpa-onnx
#
# The script will:
#   1. Read version from CMakeLists.txt
#   2. Check if release sherpa-v{version} already exists
#   3. If not — create tag + release, upload Tools/output/sherpa-onnx-ios.zip
#
# Tag format: sherpa-v{version} (to avoid conflicts with UPM plugin releases)

set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-sherpa-onnx-repo>"
  echo "Example: $0 ../sherpa-onnx"
  exit 1
fi

SHERPA_ONNX_REPO="$(cd "$1" && pwd)"

# Read version
SHERPA_VERSION=$(grep "SHERPA_ONNX_VERSION" "$SHERPA_ONNX_REPO/CMakeLists.txt" \
  | head -1 | cut -d '"' -f 2)

if [ -z "$SHERPA_VERSION" ]; then
  echo "ERROR: Could not read SHERPA_ONNX_VERSION from CMakeLists.txt"
  exit 1
fi

TAG="sherpa-v$SHERPA_VERSION"
echo "=== Version: $SHERPA_VERSION  Tag: $TAG ==="

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
ZIP_PATH="$SCRIPT_DIR/../output/sherpa-onnx-ios.zip"

if [ ! -f "$ZIP_PATH" ]; then
  echo "ERROR: $ZIP_PATH not found. Run build_ios.sh first."
  exit 1
fi

# Work from the repo root so gh knows which repo to use
cd "$REPO_ROOT"

# Check if release already exists
if gh release view "$TAG" &>/dev/null; then
  echo "Release $TAG already exists. Skipping."
  exit 0
fi

NOTES_FILE=$(mktemp)
trap 'rm -f "$NOTES_FILE"' EXIT

cat > "$NOTES_FILE" <<'NOTES_TEMPLATE'
Prebuilt iOS artifacts for sherpa-onnx VERSION_PLACEHOLDER (Unity).

## Contents

| Artifact | Description |
|---|---|
| `sherpa-onnx.dll` | Managed C# binding with `__Internal` P/Invoke (iOS requires static linking — no .dylib/.so) |
| `sherpa-onnx.xcframework` | Native static library (arm64 device + arm64/x86_64 simulator) with **kiss_fft symbol prefix fix** |
| `onnxruntime.xcframework` | ONNX Runtime static library (from upstream sherpa-onnx build) |

## Why custom builds are needed

1. **Static linking** — iOS does not allow dynamic libraries in apps. The upstream NuGet `sherpa-onnx` package ships a .dylib which cannot be used. The DLL is rebuilt with `DllImport("__Internal")` so Unity's IL2CPP linker resolves symbols from the static library.

2. **kiss_fft symbol collision** — Unity's `libiPhone-lib.a` contains its own `kiss_fft` with a different `kiss_fft_state` struct layout. Without prefixing, the linker resolves sherpa-onnx's `kiss_fft` calls to Unity's version, causing `EXC_BAD_ACCESS` crashes in fbank feature extraction. All kiss_fft symbols are prefixed with `sherpa_` via `CMAKE_C_FLAGS`.

## Usage

Extract into your Unity project's `Assets/Plugins/SherpaOnnx/iOS/`:

```
Assets/Plugins/SherpaOnnx/iOS/
├── sherpa-onnx.dll
├── sherpa-onnx.xcframework/
└── onnxruntime.xcframework/
```
NOTES_TEMPLATE

sed -i '' "s/VERSION_PLACEHOLDER/$SHERPA_VERSION/" "$NOTES_FILE"

# Create and push tag if it doesn't exist
if ! git rev-parse "$TAG" &>/dev/null; then
  git tag "$TAG"
fi
git push origin "$TAG" 2>/dev/null || true

echo "Creating release $TAG..."
gh release create "$TAG" \
  --title "$TAG" \
  --notes-file "$NOTES_FILE" \
  "$ZIP_PATH"

echo ""
echo "=== Release $TAG published ==="
echo "Asset: sherpa-onnx-ios.zip"
echo ""
