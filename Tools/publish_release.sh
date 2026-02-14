#!/usr/bin/env bash
# Create a GitHub release and upload sherpa-onnx.zip if it doesn't exist yet.
#
# Reads version from the sherpa-onnx repo CMakeLists.txt.
# Requires: gh (GitHub CLI) authenticated.
#
# Usage:
#   ./Tools/publish_release.sh /path/to/sherpa-onnx
#
# The script will:
#   1. Read version from CMakeLists.txt
#   2. Check if release sherpa-v{version} already exists
#   3. If not — create tag + release, upload Tools/output/sherpa-onnx.zip
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
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ZIP_PATH="$SCRIPT_DIR/output/sherpa-onnx.zip"

if [ ! -f "$ZIP_PATH" ]; then
  echo "ERROR: $ZIP_PATH not found. Run build_ios_dll.sh first."
  exit 1
fi

# Work from the repo root so gh knows which repo to use
cd "$REPO_ROOT"

# Check if release already exists
if gh release view "$TAG" &>/dev/null; then
  echo "Release $TAG already exists. Skipping."
  exit 0
fi

echo "Creating release $TAG..."
gh release create "$TAG" \
  --title "$TAG" \
  --notes "iOS managed DLL (sherpa-onnx.dll with __Internal binding) for sherpa-onnx $SHERPA_VERSION" \
  "$ZIP_PATH"

echo ""
echo "=== Release $TAG published ==="
echo "Asset: sherpa-onnx.zip"
echo ""
