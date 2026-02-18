#!/usr/bin/env bash
# Build sherpa-onnx.dll for iOS (Unity).
#
# iOS uses static linking, so Dll.Filename must be "__Internal"
# instead of "sherpa-onnx-c-api".
#
# Usage:
#   ./Tools/build_ios_dll.sh /path/to/sherpa-onnx
#
# Output:
#   Tools/output/sherpa-onnx.dll
#   Tools/output/sherpa-onnx.zip

set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-sherpa-onnx-repo>"
  echo "Example: $0 ../sherpa-onnx"
  exit 1
fi

SHERPA_ONNX_REPO="$(cd "$1" && pwd)"
DOTNET_SRC="$SHERPA_ONNX_REPO/scripts/dotnet"

if [ ! -d "$DOTNET_SRC" ]; then
  echo "ERROR: scripts/dotnet/ not found in $SHERPA_ONNX_REPO"
  exit 1
fi

# Read version from CMakeLists.txt
SHERPA_VERSION=$(grep "SHERPA_ONNX_VERSION" "$SHERPA_ONNX_REPO/CMakeLists.txt" \
  | head -1 | cut -d '"' -f 2)

if [ -z "$SHERPA_VERSION" ]; then
  echo "ERROR: Could not read SHERPA_ONNX_VERSION from CMakeLists.txt"
  exit 1
fi

echo "=== sherpa-onnx version: $SHERPA_VERSION ==="

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/.build-ios"
OUTPUT_DIR="$SCRIPT_DIR/output"

# Clean previous build
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"
mkdir -p "$OUTPUT_DIR"

# Copy .cs sources
cp "$DOTNET_SRC"/*.cs "$BUILD_DIR"/

# Patch Dll.cs: replace "sherpa-onnx-c-api" with "__Internal"
sed -i.bak 's|"sherpa-onnx-c-api"|"__Internal"|g' "$BUILD_DIR/Dll.cs"
rm -f "$BUILD_DIR/Dll.cs.bak"

echo "Patched Dll.cs:"
grep Filename "$BUILD_DIR/Dll.cs"

# Create .csproj
cat > "$BUILD_DIR/sherpa-onnx-ios.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <LangVersion>10.0</LangVersion>
    <TargetFramework>netstandard2.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AssemblyName>sherpa-onnx</AssemblyName>
    <Version>${SHERPA_VERSION}</Version>
    <SignAssembly>false</SignAssembly>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>
</Project>
EOF

# Build
cd "$BUILD_DIR"
dotnet build -c Release

# Find output DLL
DLL_PATH=$(find "$BUILD_DIR" -name "sherpa-onnx.dll" -path "*/Release/*" | head -1)

if [ -z "$DLL_PATH" ]; then
  echo "ERROR: sherpa-onnx.dll not found after build!"
  exit 1
fi

# Copy DLL to output
cp "$DLL_PATH" "$OUTPUT_DIR/sherpa-onnx.dll"

# Create zip archive
cd "$OUTPUT_DIR"
rm -f sherpa-onnx.zip
zip sherpa-onnx.zip sherpa-onnx.dll

# Clean build dir
rm -rf "$BUILD_DIR"

echo ""
echo "=== Build successful ==="
echo "Version:  $SHERPA_VERSION"
echo "DLL:      $OUTPUT_DIR/sherpa-onnx.dll"
echo "ZIP:      $OUTPUT_DIR/sherpa-onnx.zip"
echo "Dll.Filename = \"__Internal\" (for iOS static linking)"
echo ""
