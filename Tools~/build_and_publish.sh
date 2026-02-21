#!/usr/bin/env bash
# Build iOS managed DLL and publish it as a GitHub release.
#
# Usage:
#   ./Tools/build_and_publish.sh /path/to/sherpa-onnx

set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-sherpa-onnx-repo>"
  echo "Example: $0 ../sherpa-onnx"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "=== Step 1: Build iOS DLL ==="
"$SCRIPT_DIR/build_ios_dll.sh" "$1"

echo "=== Step 2: Publish Release ==="
"$SCRIPT_DIR/publish_release.sh" "$1"
