# DevProject

Local Unity test project for developing and testing `com.ponyudev.sherpa-onnx`.

The `~` suffix tells Unity Package Manager to skip this folder, so it never ships to consumers. The package itself is referenced via `file:../../` in `Packages/manifest.json`.

## First-Time Setup on a New Machine

After `git clone` on a new machine, run **once**:

**Windows:**
```cmd
DevProject~\Scripts\setup.cmd
```

**macOS / Linux:**
```bash
./DevProject~/Scripts/setup.sh
```

This does two things:

1. **Recreates the `Assets/Samples` link** to the package's `Samples~/` folder so test scenes are visible in the editor. On Windows, Git stores symlinks as text stubs unless `core.symlinks` is enabled — the script fixes this by creating a directory junction (`mklink /J`), which doesn't require admin privileges. On macOS/Linux the symlink works natively, so this step is skipped.

2. **Marks Unity-managed files as `skip-worktree`** so local edits don't pollute `git status` or get accidentally committed. These files are modified by Unity on every project open (package version bumps, editor version stamp, active profile indices, etc.) and should not propagate across machines:

   - `Packages/manifest.json`
   - `Packages/packages-lock.json`
   - `ProjectSettings/ProjectVersion.txt`
   - `ProjectSettings/ProjectSettings.asset`
   - `ProjectSettings/AsrSettings.asset`
   - `ProjectSettings/TtsSettings.asset`
   - `ProjectSettings/VadSettings.asset`
   - `ProjectSettings/SherpaOnnxSettings.asset`

The `skip-worktree` flag is **per-clone, not per-repo** — so every new clone needs to run setup once.

## Undo

If you need to commit a change to one of the skipped files (e.g. bumping `ProjectVersion.txt` for a real Unity-version bump, or updating settings the package depends on):

**Windows:**
```cmd
DevProject~\Scripts\unsetup.cmd
```

**macOS / Linux:**
```bash
./DevProject~/Scripts/unsetup.sh
```

This re-enables tracking on all the files. After committing your change, re-run `setup` to silence them again.

## Why a junction (`mklink /J`) instead of a symlink (`mklink /D`) on Windows?

Junctions don't require admin privileges or Developer Mode and work identically for Unity. The setup script always uses `/J`.

## Optional: enable Git symlink support globally

If you frequently clone repos with symlinks (this one has `Plugins/iOS` symlinks too), enable once globally:

```bash
git config --global core.symlinks true
```

This only affects **future** clones. Existing clones still need the `setup` script.
