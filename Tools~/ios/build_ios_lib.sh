#!/usr/bin/env bash
# Build libsherpa-onnx.a for iOS (device + simulator) with kiss_fft Unity fix.
#
# Builds for:
#   - arm64 device (real iPhone/iPad)
#   - arm64 + x86_64 simulator (also covers "Designed for iPad" on Mac)
#
# The kiss_fft symbol-prefix fix is applied via CMAKE_C_FLAGS/CMAKE_CXX_FLAGS
# so the sherpa-onnx repo is NOT modified. Unity's libiPhone-lib.a contains its
# own kiss_fft with a different struct layout — without prefixing, the linker
# resolves all kiss_fft calls to Unity's version, causing EXC_BAD_ACCESS.
#
# Usage:
#   ./Tools~/ios/build_ios_lib.sh /path/to/sherpa-onnx
#
# Output:
#   Tools~/output/sherpa-onnx.xcframework/
#
# Prerequisites:
#   - Xcode with iOS SDK
#   - cmake
#   - sherpa-onnx repo with onnxruntime already fetched (build-ios/ directory)

set -euo pipefail

# ── Args ──

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-sherpa-onnx-repo>"
  echo "Example: $0 ../sherpa-onnx"
  exit 1
fi

SHERPA_ONNX_REPO="$(cd "$1" && pwd)"

if [ ! -f "$SHERPA_ONNX_REPO/CMakeLists.txt" ]; then
  echo "ERROR: CMakeLists.txt not found in $SHERPA_ONNX_REPO"
  exit 1
fi

if [ ! -f "$SHERPA_ONNX_REPO/toolchains/ios.toolchain.cmake" ]; then
  echo "ERROR: toolchains/ios.toolchain.cmake not found in $SHERPA_ONNX_REPO"
  exit 1
fi

SHERPA_VERSION=$(grep "SHERPA_ONNX_VERSION" "$SHERPA_ONNX_REPO/CMakeLists.txt" \
  | head -1 | cut -d '"' -f 2)

ONNXRUNTIME_VERSION=$(grep "onnxruntime_version=" "$SHERPA_ONNX_REPO/build-ios.sh" \
  | head -1 | cut -d= -f2)

# onnxruntime — check the exact version required by this tag
ONNXRUNTIME_VERSIONED="$SHERPA_ONNX_REPO/build-ios/ios-onnxruntime/$ONNXRUNTIME_VERSION/onnxruntime.xcframework"
ONNXRUNTIME_XCF="$SHERPA_ONNX_REPO/build-ios/ios-onnxruntime/onnxruntime.xcframework"

if [ ! -f "$ONNXRUNTIME_VERSIONED/ios-arm64/onnxruntime.a" ]; then
  echo "onnxruntime $ONNXRUNTIME_VERSION not found — running build-ios.sh..."
  pushd "$SHERPA_ONNX_REPO" > /dev/null
  bash build-ios.sh
  popd > /dev/null

  if [ ! -f "$ONNXRUNTIME_VERSIONED/ios-arm64/onnxruntime.a" ]; then
    echo "ERROR: onnxruntime $ONNXRUNTIME_VERSION still not found after build-ios.sh"
    exit 1
  fi
fi

# Use the symlink (build-ios.sh keeps it pointing to the right version)
if [ ! -f "$ONNXRUNTIME_XCF/ios-arm64/onnxruntime.a" ]; then
  # Symlink might be stale — fix it
  ln -sf "$ONNXRUNTIME_VERSION/onnxruntime.xcframework" \
    "$SHERPA_ONNX_REPO/build-ios/ios-onnxruntime/onnxruntime.xcframework"
fi

if [ -z "$SHERPA_VERSION" ]; then
  echo "ERROR: Could not read SHERPA_ONNX_VERSION from CMakeLists.txt"
  exit 1
fi

echo "=== sherpa-onnx $SHERPA_VERSION | onnxruntime $ONNXRUNTIME_VERSION ==="

# ── Paths ──

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/../.build-ios-lib"
OUTPUT_DIR="$SCRIPT_DIR/../output"
TOOLCHAIN="$SHERPA_ONNX_REPO/toolchains/ios.toolchain.cmake"
NCPU="$(sysctl -n hw.ncpu)"

mkdir -p "$BUILD_DIR"
mkdir -p "$OUTPUT_DIR"

# ── kiss_fft symbol rename (Unity collision fix) ──

KISS_FFT_DEFINES=""
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fft=sherpa_kiss_fft"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fft_alloc=sherpa_kiss_fft_alloc"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fft_stride=sherpa_kiss_fft_stride"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fft_cleanup=sherpa_kiss_fft_cleanup"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fft_next_fast_size=sherpa_kiss_fft_next_fast_size"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftr=sherpa_kiss_fftr"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftr_alloc=sherpa_kiss_fftr_alloc"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftri=sherpa_kiss_fftri"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftnd=sherpa_kiss_fftnd"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftnd_alloc=sherpa_kiss_fftnd_alloc"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftndr=sherpa_kiss_fftndr"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftndr_alloc=sherpa_kiss_fftndr_alloc"
KISS_FFT_DEFINES="$KISS_FFT_DEFINES -Dkiss_fftndri=sherpa_kiss_fftndri"

# ── Shared cmake args ──

CMAKE_COMMON=(
  -DBUILD_PIPER_PHONMIZE_EXE=OFF
  -DBUILD_PIPER_PHONMIZE_TESTS=OFF
  -DBUILD_ESPEAK_NG_EXE=OFF
  -DBUILD_ESPEAK_NG_TESTS=OFF
  -S "$SHERPA_ONNX_REPO"
  -DCMAKE_TOOLCHAIN_FILE="$TOOLCHAIN"
  -DENABLE_BITCODE=0
  -DENABLE_ARC=1
  -DENABLE_VISIBILITY=0
  -DCMAKE_BUILD_TYPE=Release
  -DBUILD_SHARED_LIBS=OFF
  -DSHERPA_ONNX_ENABLE_PYTHON=OFF
  -DSHERPA_ONNX_ENABLE_TESTS=OFF
  -DSHERPA_ONNX_ENABLE_CHECK=OFF
  -DSHERPA_ONNX_ENABLE_PORTAUDIO=OFF
  -DSHERPA_ONNX_ENABLE_JNI=OFF
  -DSHERPA_ONNX_ENABLE_C_API=ON
  -DSHERPA_ONNX_ENABLE_WEBSOCKET=OFF
  -DSHERPA_ONNX_ENABLE_BINARY=OFF
  -DDEPLOYMENT_TARGET=13.0
  "-DCMAKE_C_FLAGS=$KISS_FFT_DEFINES"
  "-DCMAKE_CXX_FLAGS=$KISS_FFT_DEFINES"
)

# Static libs to merge
MERGE_LIBS=(
  libkaldi-native-fbank-core.a
  libkissfft-float.a
  libsherpa-onnx-c-api.a
  libsherpa-onnx-core.a
  libsherpa-onnx-fstfar.a
  libsherpa-onnx-fst.a
  libsherpa-onnx-kaldifst-core.a
  libkaldi-decoder-core.a
  libucd.a
  libpiper_phonemize.a
  libespeak-ng.a
  libssentencepiece_core.a
)

merge_libs() {
  local build_path="$1"
  local output="$2"
  local tmpdir
  tmpdir="$(mktemp -d)"

  # cmake generates GNU ar format archives with '/' and '//' entries.
  # Apple libtool can't read those directly, so we extract .o files first.
  for lib in "${MERGE_LIBS[@]}"; do
    local p="$build_path/lib/$lib"
    if [ -f "$p" ]; then
      local libdir="$tmpdir/${lib%.a}"
      mkdir -p "$libdir"
      pushd "$libdir" > /dev/null
      ar x "$p"
      popd > /dev/null
    else
      echo "  WARNING: $lib not found, skipping"
    fi
  done

  # Collect all .o files and merge with Apple libtool
  local ofiles
  ofiles=$(find "$tmpdir" -name '*.o' -o -name '*.c.o' -o -name '*.cc.o' -o -name '*.cpp.o')
  libtool -static -o "$output" $ofiles

  rm -rf "$tmpdir"
}

# ── 1. Build arm64 device ──

echo ""
echo "=== Building arm64 (device) ==="

export SHERPA_ONNXRUNTIME_LIB_DIR="$ONNXRUNTIME_XCF/ios-arm64"
export SHERPA_ONNXRUNTIME_INCLUDE_DIR="$ONNXRUNTIME_XCF/Headers"

cmake "${CMAKE_COMMON[@]}" \
  -DPLATFORM=OS64 \
  -DCMAKE_INSTALL_PREFIX="$BUILD_DIR/install" \
  -B "$BUILD_DIR/build/os64"

cmake --build "$BUILD_DIR/build/os64" -j "$NCPU"

# Copy headers manually (install target tries to build everything)
mkdir -p "$BUILD_DIR/install/include/sherpa-onnx/c-api"
cp "$SHERPA_ONNX_REPO/sherpa-onnx/c-api/c-api.h" "$BUILD_DIR/install/include/sherpa-onnx/c-api/"
cp "$SHERPA_ONNX_REPO/sherpa-onnx/c-api/cxx-api.h" "$BUILD_DIR/install/include/sherpa-onnx/c-api/"

merge_libs "$BUILD_DIR/build/os64" "$BUILD_DIR/build/os64/libsherpa-onnx.a"
echo "device arm64: $(du -h "$BUILD_DIR/build/os64/libsherpa-onnx.a" | cut -f1)"

# ── 2. Build arm64 simulator ──

echo ""
echo "=== Building arm64 (simulator) ==="

export SHERPA_ONNXRUNTIME_LIB_DIR="$ONNXRUNTIME_XCF/ios-arm64_x86_64-simulator"

cmake "${CMAKE_COMMON[@]}" \
  -DPLATFORM=SIMULATORARM64 \
  -B "$BUILD_DIR/build/sim_arm64"

cmake --build "$BUILD_DIR/build/sim_arm64" -j "$NCPU"

merge_libs "$BUILD_DIR/build/sim_arm64" "$BUILD_DIR/build/sim_arm64/libsherpa-onnx.a"
echo "simulator arm64: $(du -h "$BUILD_DIR/build/sim_arm64/libsherpa-onnx.a" | cut -f1)"

# ── 3. Build x86_64 simulator ──

echo ""
echo "=== Building x86_64 (simulator) ==="

cmake "${CMAKE_COMMON[@]}" \
  -DPLATFORM=SIMULATOR64 \
  -B "$BUILD_DIR/build/sim_x86_64"

cmake --build "$BUILD_DIR/build/sim_x86_64" -j "$NCPU"

merge_libs "$BUILD_DIR/build/sim_x86_64" "$BUILD_DIR/build/sim_x86_64/libsherpa-onnx.a"
echo "simulator x86_64: $(du -h "$BUILD_DIR/build/sim_x86_64/libsherpa-onnx.a" | cut -f1)"

# ── 4. Create universal simulator binary ──

echo ""
echo "=== Creating universal simulator binary ==="

lipo -create \
  "$BUILD_DIR/build/sim_arm64/libsherpa-onnx.a" \
  "$BUILD_DIR/build/sim_x86_64/libsherpa-onnx.a" \
  -output "$BUILD_DIR/build/simulator-libsherpa-onnx.a"

echo "simulator universal: $(du -h "$BUILD_DIR/build/simulator-libsherpa-onnx.a" | cut -f1)"

# ── 5. Verify kiss_fft symbols ──

echo ""
echo "=== Verifying kiss_fft symbols ==="

if nm "$BUILD_DIR/build/os64/libsherpa-onnx.a" 2>/dev/null | grep -q "_sherpa_kiss_fft"; then
  echo "OK: sherpa_kiss_fft found"
else
  echo "WARNING: sherpa_kiss_fft not found"
fi

if nm "$BUILD_DIR/build/os64/libsherpa-onnx.a" 2>/dev/null | grep " T _kiss_fft$\| T _kiss_fft_alloc$" > /dev/null; then
  echo "ERROR: unprefixed _kiss_fft found — Unity collision NOT fixed!"
  exit 1
else
  echo "OK: no unprefixed _kiss_fft"
fi

# ── 6. Create xcframework ──

echo ""
echo "=== Creating xcframework ==="

XCFW_OUT="$OUTPUT_DIR/sherpa-onnx.xcframework"
rm -rf "$XCFW_OUT"

xcodebuild -create-xcframework \
  -library "$BUILD_DIR/build/os64/libsherpa-onnx.a" \
  -headers "$BUILD_DIR/install/include" \
  -library "$BUILD_DIR/build/simulator-libsherpa-onnx.a" \
  -headers "$BUILD_DIR/install/include" \
  -output "$XCFW_OUT"

# Copy onnxruntime.xcframework to output (resolve symlinks with -RL)
ONNX_OUT="$OUTPUT_DIR/onnxruntime.xcframework"
rm -rf "$ONNX_OUT"
cp -RL "$ONNXRUNTIME_XCF" "$ONNX_OUT"

# ── Summary ──

echo ""
echo "========================================="
echo "  BUILD SUCCESSFUL"
echo "========================================="
echo "Version:       $SHERPA_VERSION"
echo "Platforms:     arm64 (device), arm64+x86_64 (simulator)"
echo "Output:        $OUTPUT_DIR/"
echo "  sherpa-onnx.xcframework  ($(du -h "$XCFW_OUT/ios-arm64/libsherpa-onnx.a" | cut -f1) device, $(du -h "$XCFW_OUT/ios-arm64_x86_64-simulator/"*sherpa*.a | cut -f1) simulator)"
echo "  onnxruntime.xcframework"
echo "kiss_fft:      prefixed with sherpa_ (Unity collision fix)"
echo ""
echo "Install into Unity project:"
echo "  cp -R \"$OUTPUT_DIR/sherpa-onnx.xcframework\" <unity>/Assets/Plugins/SherpaOnnx/iOS/"
echo "  cp -R \"$OUTPUT_DIR/onnxruntime.xcframework\" <unity>/Assets/Plugins/SherpaOnnx/iOS/"
echo "========================================="
