# Unity Package Scripts

Scripts for building the `.unitypackage` installer, creating UPM releases, and registering on OpenUPM.

## Prerequisites

- [GitHub CLI](https://cli.github.com/) (`gh`) — authenticated with push access

## Scripts

### build_installer.sh

Builds a small `.unitypackage` containing an editor script that adds the OpenUPM scoped registry to `manifest.json`.

```bash
./Tools~/unity/build_installer.sh
```

**Output:** `Tools~/output/SherpaOnnxInstaller.unitypackage`

**Source:** `Installer/SherpaOnnxInstaller.cs` — an `[InitializeOnLoad]` editor script that:
- Adds the OpenUPM scoped registry to `Packages/manifest.json`
- Adds `com.ponyudev.sherpa-onnx` as a dependency
- Shows a confirmation dialog before making changes
- Deletes itself after successful installation

### create_release.sh

Creates a GitHub release with a git tag and attaches the `.unitypackage` installer.

```bash
./Tools~/unity/create_release.sh
```

**What it does:**
1. Checks for uncommitted changes
2. Extracts release notes from `CHANGELOG.md` for the current version
3. Builds the `.unitypackage` installer (via `build_installer.sh`)
4. Creates git tag `v{version}` and pushes it
5. Creates a GitHub Release with the `.unitypackage` attached

### register_openupm.sh

Registers the package on [OpenUPM](https://openupm.com/) by creating a PR to the openupm/openupm repository.

```bash
./Tools~/unity/register_openupm.sh
```

**What it does:**
1. Checks that the repository is public
2. Forks `openupm/openupm` (if not already forked)
3. Creates a YAML package definition in `data/packages/`
4. Pushes a branch and creates a PR
