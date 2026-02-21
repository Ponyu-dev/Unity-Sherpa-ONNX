#!/usr/bin/env bash
set -euo pipefail

# Register com.ponyudev.sherpa-onnx on OpenUPM
# Prerequisites: gh CLI authenticated, repo must be PUBLIC

PACKAGE_NAME="com.ponyudev.sherpa-onnx"
REPO_URL="https://github.com/Ponyu-dev/Unity-Sherpa-ONNX"
OPENUPM_REPO="openupm/openupm"
BRANCH_NAME="add-${PACKAGE_NAME}"

echo "=== OpenUPM Registration: ${PACKAGE_NAME} ==="

# Check if repo is public
VISIBILITY=$(gh repo view Ponyu-dev/Unity-Sherpa-ONNX --json isPrivate --jq '.isPrivate')
if [ "$VISIBILITY" = "true" ]; then
    echo "ERROR: Repository is still private. Make it public first (SHONNX-33)."
    exit 1
fi

# Fork openupm repo (skip if already forked)
echo "Forking ${OPENUPM_REPO}..."
gh repo fork "${OPENUPM_REPO}" --clone=false 2>/dev/null || true

# Clone the fork
WORK_DIR=$(mktemp -d)
echo "Cloning fork to ${WORK_DIR}..."
gh repo clone "Ponyu-dev/openupm" "${WORK_DIR}" -- --depth 1

cd "${WORK_DIR}"

# Create branch
git checkout -b "${BRANCH_NAME}"

# Create package YAML
cat > "data/packages/${PACKAGE_NAME}.yml" << 'EOF'
name: com.ponyudev.sherpa-onnx
displayName: PonyuDev Sherpa-ONNX
description: Unity integration for sherpa-onnx — offline TTS, ASR, and VAD with one-click native library install for all platforms.
repoUrl: 'https://github.com/Ponyu-dev/Unity-Sherpa-ONNX'
licenseSpdxId: Apache-2.0
licenseName: Apache License 2.0
topics:
  - ai
  - voice
  - audio
  - tts
  - stt
  - asr
  - vad
  - speech
  - onnx
  - unity
hunter: Ponyu-dev
gitTagPrefix: 'v'
gitTagIgnore: 'sherpa-v'
createdAt: 1740100800000
EOF

echo "Created data/packages/${PACKAGE_NAME}.yml"

# Commit and push
git add "data/packages/${PACKAGE_NAME}.yml"
git commit -m "Add ${PACKAGE_NAME}"
git push -u origin "${BRANCH_NAME}"

# Create PR
PR_URL=$(gh pr create \
    --repo "${OPENUPM_REPO}" \
    --title "Add ${PACKAGE_NAME}" \
    --body "$(cat <<'BODY'
## Package Info

- Package name: `com.ponyudev.sherpa-onnx`
- Repository: https://github.com/Ponyu-dev/Unity-Sherpa-ONNX
- License: Apache-2.0
- Description: Unity integration for sherpa-onnx — offline TTS, ASR, and VAD with one-click native library install for all platforms.

## Checklist

- [x] Package name matches `package.json`
- [x] Repository is public
- [x] License is specified
- [x] `package.json` is at repository root
BODY
)")

echo ""
echo "=== Done ==="
echo "PR created: ${PR_URL}"

# Cleanup
rm -rf "${WORK_DIR}"
