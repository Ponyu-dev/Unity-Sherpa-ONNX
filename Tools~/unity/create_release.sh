#!/usr/bin/env bash
set -euo pipefail

# Create GitHub Release with git tag
# Prerequisites: gh CLI authenticated, all changes committed and pushed

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
VERSION="0.1.0"
TAG="v${VERSION}"

echo "=== GitHub Release: ${TAG} ==="

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo "ERROR: Uncommitted changes detected. Commit and push first."
    exit 1
fi

# Check if tag already exists
if git rev-parse "${TAG}" >/dev/null 2>&1; then
    echo "ERROR: Tag ${TAG} already exists."
    exit 1
fi

# Extract release notes from CHANGELOG.md (section between ## [version] and next ## or EOF)
NOTES=$(awk "/^## \\[${VERSION}\\]/{found=1; next} /^## \\[/{if(found) exit} found{print}" CHANGELOG.md)

if [ -z "$NOTES" ]; then
    echo "ERROR: Could not extract release notes from CHANGELOG.md"
    exit 1
fi

echo "Release notes:"
echo "${NOTES}"
echo ""

# Build .unitypackage installer
echo "Building .unitypackage installer..."
"${SCRIPT_DIR}/build_installer.sh"
INSTALLER_PKG="${SCRIPT_DIR}/../output/SherpaOnnxInstaller.unitypackage"

if [ ! -f "${INSTALLER_PKG}" ]; then
    echo "ERROR: Failed to build .unitypackage"
    exit 1
fi

# Create tag and release
echo "Creating tag ${TAG}..."
git tag "${TAG}"
git push origin "${TAG}"

echo "Creating GitHub Release..."
gh release create "${TAG}" \
    "${INSTALLER_PKG}" \
    --title "v${VERSION} — First Public Release" \
    --notes "${NOTES}" \
    --latest

echo ""
echo "=== Done ==="
echo "Release: https://github.com/Ponyu-dev/Unity-Sherpa-ONNX/releases/tag/${TAG}"
echo ""
echo "Install options:"
echo "  UPM git URL:  https://github.com/Ponyu-dev/Unity-Sherpa-ONNX.git#${TAG}"
echo "  Installer:    Download SherpaOnnxInstaller.unitypackage from the release"
