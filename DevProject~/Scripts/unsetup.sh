#!/usr/bin/env bash
# Reverses setup.sh — re-enables git tracking of Unity-managed files.
# Use this if you want to commit changes to ProjectSettings or manifest.json.

set -e

cd "$(dirname "$0")/.."

echo
echo "=== DevProject undo setup ==="
echo

cd ..

unskip() {
    if git update-index --no-skip-worktree "$1" 2>/dev/null; then
        echo "  [ OK ] $1"
    else
        echo "  [skip] $1 (not tracked)"
    fi
}

unskip "DevProject~/Packages/manifest.json"
unskip "DevProject~/Packages/packages-lock.json"
unskip "DevProject~/ProjectSettings/ProjectVersion.txt"
unskip "DevProject~/ProjectSettings/ProjectSettings.asset"
unskip "DevProject~/ProjectSettings/AsrSettings.asset"
unskip "DevProject~/ProjectSettings/TtsSettings.asset"
unskip "DevProject~/ProjectSettings/VadSettings.asset"
unskip "DevProject~/ProjectSettings/SherpaOnnxSettings.asset"

echo
echo "=== Undo complete ==="
echo "Run Scripts/setup.sh to re-enable local development mode."
