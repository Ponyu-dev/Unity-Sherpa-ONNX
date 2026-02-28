#!/usr/bin/env bash
# Build all iOS artifacts for Unity and package into a single zip.
#
# Runs two stages:
#   1. build_ios_lib.sh  — native C/C++ xcframeworks (sherpa-onnx + onnxruntime)
#   2. build_ios_dll.sh  — managed C# DLL with __Internal P/Invoke
#
# Usage:
#   ./Tools~/ios/build_ios.sh /path/to/sherpa-onnx
#
# Output:
#   Tools~/output/sherpa-onnx-ios.zip
#     ├── sherpa-onnx.dll
#     ├── sherpa-onnx.xcframework/
#     └── onnxruntime.xcframework/

set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-sherpa-onnx-repo>"
  echo "Example: $0 ../sherpa-onnx"
  exit 1
fi

SHERPA_ONNX_REPO="$(cd "$1" && pwd)"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/../output"

# Early check: reject versions that use System.Text.Json (incompatible with Unity)
if grep -rq "System\.Text\.Json" "$SHERPA_ONNX_REPO/scripts/dotnet/"*.cs 2>/dev/null; then
  echo "ERROR: C# sources use System.Text.Json — incompatible with Unity. Skipping this version."
  exit 1
fi

# Clean previous output
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# ── 1. Native libs ──

echo ""
echo "##############################"
echo "# Stage 1: Native xcframeworks"
echo "##############################"
echo ""

bash "$SCRIPT_DIR/build_ios_lib.sh" "$SHERPA_ONNX_REPO"

# ── 2. Managed DLL ──

echo ""
echo "##############################"
echo "# Stage 2: Managed DLL"
echo "##############################"
echo ""

bash "$SCRIPT_DIR/build_ios_dll.sh" "$SHERPA_ONNX_REPO"

# ── 3. Package ──

echo ""
echo "##############################"
echo "# Stage 3: Packaging"
echo "##############################"
echo ""

# Verify all artifacts exist
for artifact in "sherpa-onnx.dll" "sherpa-onnx.xcframework" "onnxruntime.xcframework"; do
  if [ ! -e "$OUTPUT_DIR/$artifact" ]; then
    echo "ERROR: $artifact not found in $OUTPUT_DIR"
    exit 1
  fi
done

cd "$OUTPUT_DIR"
rm -f sherpa-onnx-ios.zip

# -r for directories, -y to store symlinks as-is
zip -ry sherpa-onnx-ios.zip \
  sherpa-onnx.dll \
  sherpa-onnx.xcframework \
  onnxruntime.xcframework

# Read version for summary
SHERPA_VERSION=$(grep "SHERPA_ONNX_VERSION" "$SHERPA_ONNX_REPO/CMakeLists.txt" \
  | head -1 | cut -d '"' -f 2)

echo ""
echo "========================================="
echo "  ALL DONE"
echo "========================================="
echo "Version:  $SHERPA_VERSION"
echo "Archive:  $OUTPUT_DIR/sherpa-onnx-ios.zip"
echo ""
echo "Contents:"
echo "  sherpa-onnx.dll              (C# managed, __Internal)"
echo "  sherpa-onnx.xcframework/     (native, kiss_fft prefixed)"
echo "  onnxruntime.xcframework/     (native)"
echo "========================================="
