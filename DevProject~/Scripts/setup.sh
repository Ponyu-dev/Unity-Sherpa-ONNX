#!/usr/bin/env bash
# Sets up DevProject on a fresh macOS / Linux machine:
#   - Marks Unity auto-modified files as skip-worktree so local edits
#     don't pollute git status or risk being committed.
#   - Symlinks (Samples) work natively on macOS/Linux, so no junction needed.
#
# Idempotent. Safe to run multiple times.

set -e

cd "$(dirname "$0")/.."

echo
echo "=== DevProject local setup ==="
echo

cd ..

skip() {
    if git update-index --skip-worktree "$1" 2>/dev/null; then
        echo "  [ OK ] $1"
    else
        echo "  [skip] $1 (not tracked, nothing to do)"
    fi
}

echo "[WORK] Marking Unity-managed files as skip-worktree..."

skip "DevProject~/Packages/manifest.json"
skip "DevProject~/Packages/packages-lock.json"
skip "DevProject~/ProjectSettings/ProjectVersion.txt"
skip "DevProject~/ProjectSettings/ProjectSettings.asset"
skip "DevProject~/ProjectSettings/AsrSettings.asset"
skip "DevProject~/ProjectSettings/TtsSettings.asset"
skip "DevProject~/ProjectSettings/VadSettings.asset"
skip "DevProject~/ProjectSettings/SherpaOnnxSettings.asset"

echo
echo "=== Setup complete ==="
echo "You can now open DevProject~ in Unity."
echo "To undo: run Scripts/unsetup.sh"
