#!/usr/bin/env bash
# Build all iOS artifacts and publish as a GitHub release.
#
# Usage:
#   ./Tools~/ios/build_and_publish.sh /path/to/sherpa-onnx

set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-sherpa-onnx-repo>"
  echo "Example: $0 ../sherpa-onnx"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "=== Step 1: Build iOS (DLL + native libs) ==="
"$SCRIPT_DIR/build_ios.sh" "$1"

echo "=== Step 2: Publish Release ==="
"$SCRIPT_DIR/publish_release.sh" "$1"
