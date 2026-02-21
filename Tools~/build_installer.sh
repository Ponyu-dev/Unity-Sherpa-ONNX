#!/usr/bin/env bash
set -euo pipefail

# Build .unitypackage installer for PonyuDev Sherpa-ONNX
# Creates a small package containing an editor script that adds
# the OpenUPM scoped registry and package dependency to manifest.json

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
INSTALLER_CS="${SCRIPT_DIR}/Installer/SherpaOnnxInstaller.cs"
OUTPUT_DIR="${SCRIPT_DIR}/output"
OUTPUT_FILE="${OUTPUT_DIR}/SherpaOnnxInstaller.unitypackage"

# Asset GUID (stable, so re-runs produce the same package)
ASSET_GUID="a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6"
ASSET_PATH="Assets/PonyuDev/SherpaOnnxInstaller/Editor/SherpaOnnxInstaller.cs"
FOLDER_GUID="f6e5d4c3b2a1f0e9d8c7b6a5f4e3d2c1"
FOLDER_PATH="Assets/PonyuDev/SherpaOnnxInstaller"
EDITOR_GUID="d2c1b0a9f8e7d6c5b4a3f2e1d0c9b8a7"
EDITOR_PATH="Assets/PonyuDev/SherpaOnnxInstaller/Editor"

if [ ! -f "${INSTALLER_CS}" ]; then
    echo "ERROR: ${INSTALLER_CS} not found"
    exit 1
fi

mkdir -p "${OUTPUT_DIR}"

# Create temp directory for .unitypackage structure
WORK_DIR=$(mktemp -d)
trap 'rm -rf "${WORK_DIR}"' EXIT

# --- Folder asset: Assets/PonyuDev/SherpaOnnxInstaller ---
mkdir -p "${WORK_DIR}/${FOLDER_GUID}"
echo "${FOLDER_PATH}" > "${WORK_DIR}/${FOLDER_GUID}/pathname"
cat > "${WORK_DIR}/${FOLDER_GUID}/asset.meta" << META
fileFormatVersion: 2
guid: ${FOLDER_GUID}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
META

# --- Folder asset: Assets/PonyuDev/SherpaOnnxInstaller/Editor ---
mkdir -p "${WORK_DIR}/${EDITOR_GUID}"
echo "${EDITOR_PATH}" > "${WORK_DIR}/${EDITOR_GUID}/pathname"
cat > "${WORK_DIR}/${EDITOR_GUID}/asset.meta" << META
fileFormatVersion: 2
guid: ${EDITOR_GUID}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
META

# --- Script asset ---
mkdir -p "${WORK_DIR}/${ASSET_GUID}"
echo "${ASSET_PATH}" > "${WORK_DIR}/${ASSET_GUID}/pathname"
cp "${INSTALLER_CS}" "${WORK_DIR}/${ASSET_GUID}/asset"
cat > "${WORK_DIR}/${ASSET_GUID}/asset.meta" << META
fileFormatVersion: 2
guid: ${ASSET_GUID}
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
META

# Build .unitypackage (gzipped tar)
cd "${WORK_DIR}"
tar czf "${OUTPUT_FILE}" -- */

echo "Built: ${OUTPUT_FILE}"
echo "Size: $(du -h "${OUTPUT_FILE}" | cut -f1)"
