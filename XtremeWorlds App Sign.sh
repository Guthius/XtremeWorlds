#!/bin/bash
set -euo pipefail

if [ "${EUID:-$(id -u)}" -eq 0 ]; then
  echo "❌ Do not run this script with sudo (root cannot access your signing identity)." >&2
  echo "   Run it as your user: $USER" >&2
  exit 1
fi

PREPARE_ONLY="${PREPARE_ONLY:-0}"
NOTARIZE="${NOTARIZE:-1}"
if [ "${1:-}" != "" ]; then
  NOTARIZE="$1"
fi

# ---- Paths (adjust if needed) ----

resolve_repo_root() {
  if [ -f "/Users/helladen/XtremeWorlds/XtremeWorlds.sln" ]; then
    echo "/Users/helladen/XtremeWorlds"
    return 0
  fi
  if [ -f "/Users/Helladen/XtremeWorlds/XtremeWorlds.sln" ]; then
    echo "/Users/Helladen/XtremeWorlds"
    return 0
  fi
  if [ -f "$PWD/XtremeWorlds.sln" ]; then
    echo "$PWD"
    return 0
  fi
  echo "❌ Could not locate repo root (XtremeWorlds.sln)." >&2
  echo "   Set REPO_ROOT manually in the script." >&2
  exit 1
}

REPO_ROOT="$(resolve_repo_root)"

pick_icon_path() {
  # Icon source (preferred): Desktop/Icon.png
  # Override at runtime if needed:
  #   ICON_PATH="/path/to/Icon.png" PREPARE_ONLY=1 bash "./XtremeWorlds App Sign.sh"
  if [ -n "${ICON_PATH:-}" ] && [ -f "$ICON_PATH" ]; then
    echo "$ICON_PATH"
    return 0
  fi

  local candidates=(
    "$HOME/Desktop/Icon.png"
    "$REPO_ROOT/src/Client/Icon.png"
    "$REPO_ROOT/src/Client/icon.png"
    "$REPO_ROOT/src/Client/Icon.ico"
    "$REPO_ROOT/src/Client/icon.ico"
  )

  local c
  for c in "${candidates[@]}"; do
    if [ -f "$c" ]; then
      echo "$c"
      return 0
    fi
  done

  echo ""  # caller handles error
  return 1
}

ICON_PATH="$(pick_icon_path || true)"
if [ -z "$ICON_PATH" ]; then
  echo "❌ Could not find an icon source file." >&2
  echo "   Provide ICON_PATH=/path/to/Icon.png (or .ico)" >&2
  echo "   Checked: $HOME/Desktop/Icon.png and $REPO_ROOT/src/Client/{Icon.png,icon.png,Icon.ico,icon.ico}" >&2
  exit 1
fi

# Set variables
# You can override this at runtime:
#   DEV_ID_CERT="Developer ID Application: Your Name (TEAMID)" bash "./XtremeWorlds App Sign.sh"
# or:
#   DEV_ID_CERT="<SHA-1>" bash "./XtremeWorlds App Sign.sh"
DEV_ID_CERT="${DEV_ID_CERT:-1D7EAFD7B33ACC5AADCF8533573F2D55499281EF}"
HOME_DIR="${HOME}"
ENTITLEMENTS="$HOME_DIR/entitlements.plist"
# Optional: restrict codesign to a specific keychain.
# WARNING: If the identity is not in this keychain, codesign will fail with "no identity found".
KEYCHAIN_PATH="${KEYCHAIN_PATH:-$HOME_DIR/Library/Keychains/login.keychain-db}"

APPLE_ID="${APPLE_ID:-hellator@gmail.com}"
TEAM_ID="${TEAM_ID:-JADW4PJ5V4}"
NOTARY_PASSWORD="${NOTARY_PASSWORD:-}"
NOTARY_PROFILE="${NOTARY_PROFILE:-}"

# Output app bundles (home folder)
APPS=(
  "$HOME_DIR/XtremeWorlds Client.app"
  "$HOME_DIR/XtremeWorlds Server.app"
)

CLIENT_APP_OUT="${APPS[0]}"
SERVER_APP_OUT="${APPS[1]}"

BUILD_CLIENT_APP="$REPO_ROOT/build/Client/XtremeWorlds.app"
BUILD_SERVER_UNIVERSAL="$REPO_ROOT/build/Server/XtremeWorlds.Server"
BUILD_SERVER_RES_ARM64="$REPO_ROOT/build/Server/osx-arm64"
BUILD_SERVER_RES_X64="$REPO_ROOT/build/Server/osx-x64"

copy_client_overlay_from_build_output() {
  # Overlay the freshly-built managed DLL into the copied .app bundle so we can
  # test code changes without needing to rebuild the entire .app creator step.
  local runtime_arch="${CLIENT_RUNTIME_ARCH:-auto}"
  if [ "$runtime_arch" = "auto" ]; then
    runtime_arch="$(uname -m)"
  fi

  local runtime_dir=""
  case "$runtime_arch" in
    arm64) runtime_dir="$REPO_ROOT/Build/Client/osx-arm64" ;;
    x86_64) runtime_dir="$REPO_ROOT/Build/Client/osx-x64" ;;
    *)
      echo "❌ Unsupported CLIENT_RUNTIME_ARCH: $runtime_arch (expected arm64 or x86_64)" >&2
      exit 1
      ;;
  esac

  local resources_dir="$CLIENT_APP_OUT/Contents/Resources"
  local macos_dir="$CLIENT_APP_OUT/Contents/MacOS"

  # Keep the managed payload in Resources.
  if [ -f "$runtime_dir/XtremeWorlds.Client.dll" ]; then
    cp "$runtime_dir/XtremeWorlds.Client.dll" "$resources_dir/XtremeWorlds.Client.dll"
  fi
  if [ -f "$runtime_dir/XtremeWorlds.Client.pdb" ]; then
    cp "$runtime_dir/XtremeWorlds.Client.pdb" "$resources_dir/XtremeWorlds.Client.pdb"
  fi

  # Ensure the native host and runtime bits match the rebuilt managed DLL.
  # The prebuilt universal apphost can embed stale managed code, which makes
  # runtime script compilation fixes appear to have no effect.
  if [ -f "$runtime_dir/XtremeWorlds.Client" ]; then
    cp "$runtime_dir/XtremeWorlds.Client" "$macos_dir/XtremeWorlds.Client"
    chmod +x "$macos_dir/XtremeWorlds.Client" 2>/dev/null || true
  fi
  if [ -f "$runtime_dir/createdump" ]; then
    cp "$runtime_dir/createdump" "$macos_dir/createdump"
    chmod +x "$macos_dir/createdump" 2>/dev/null || true
  fi

  # Copy self-contained .NET runtime dylibs next to the executable.
  # (This is where the apphost expects to find libhostfxr/libhostpolicy/etc.)
  if ls "$runtime_dir"/*.dylib >/dev/null 2>&1; then
    cp "$runtime_dir"/*.dylib "$macos_dir/"
  fi

  # Keep deps/runtimeconfig aligned with the rebuilt output.
  if [ -f "$runtime_dir/XtremeWorlds.Client.deps.json" ]; then
    cp "$runtime_dir/XtremeWorlds.Client.deps.json" "$resources_dir/XtremeWorlds.Client.deps.json"
  fi
  if [ -f "$runtime_dir/XtremeWorlds.Client.runtimeconfig.json" ]; then
    cp "$runtime_dir/XtremeWorlds.Client.runtimeconfig.json" "$resources_dir/XtremeWorlds.Client.runtimeconfig.json"
  fi
}

copy_client_app() {
  if [ ! -d "$BUILD_CLIENT_APP" ]; then
    echo "❌ Missing client app bundle: $BUILD_CLIENT_APP" >&2
    exit 1
  fi

  echo "Preparing Client app bundle: $CLIENT_APP_OUT"
  rm -rf "$CLIENT_APP_OUT"
  cp -R "$BUILD_CLIENT_APP" "$CLIENT_APP_OUT"

  # Remove incomplete self-contained hostfxr bits from the bundle.
  # If libhostfxr/hostpolicy are present without the full shared framework,
  # the apphost will refuse to use the globally installed .NET runtime.
  local resources_dir="$CLIENT_APP_OUT/Contents/Resources"
  rm -f "$resources_dir/libhostfxr.dylib" "$resources_dir/libhostpolicy.dylib" 2>/dev/null || true

  # createdump belongs next to the executable (Contents/MacOS). If the prebuilt
  # bundle includes a Resources copy, remove it to avoid signing/launch confusion.
  rm -f "$resources_dir/createdump" 2>/dev/null || true

  copy_client_overlay_from_build_output

  # Some parts of the client and the .NET apphost expect key files alongside the
  # executable directory (Contents/MacOS). Keep canonical copies in Resources,
  # but provide symlinks in MacOS.
  local macos_dir="$CLIENT_APP_OUT/Contents/MacOS"

  local link_names=(
    "XtremeWorlds.Client.dll"
    "XtremeWorlds.Client.deps.json"
    "XtremeWorlds.Client.runtimeconfig.json"
  )
  for name in "${link_names[@]}"; do
    if [ -e "$resources_dir/$name" ] && [ ! -e "$macos_dir/$name" ]; then
      (cd "$macos_dir" && ln -s "../Resources/$name" "$name")
    fi
  done

  # Some parts of the client may resolve asset paths relative to the executable
  # directory (Contents/MacOS) instead of the working directory.
  # Provide a symlink to the canonical Resources/Content folder.
  if [ -d "$resources_dir/Content" ] && [ ! -e "$macos_dir/Content" ]; then
    (cd "$macos_dir" && ln -s "../Resources/Content" "Content")
  fi

  # Keep non-code configuration out of Contents/MacOS to avoid codesign treating
  # it as a nested code component (can break signing with errors like
  # "code object is not signed at all" referencing Settings.json).
  if [ -d "$macos_dir/Config" ] && [ ! -e "$resources_dir/Config" ]; then
    mv "$macos_dir/Config" "$resources_dir/Config"
  fi
  if [ -d "$resources_dir/Config" ] && [ ! -e "$macos_dir/Config" ]; then
    (cd "$macos_dir" && ln -s "../Resources/Config" "Config")
  fi
}

create_server_app() {
  # Decide which runtime payload to bundle.
  # Auto: match current machine arch, so the bundle actually launches locally.
  local runtime_arch="${SERVER_RUNTIME_ARCH:-auto}"
  if [ "$runtime_arch" = "auto" ]; then
    runtime_arch="$(uname -m)"
  fi

  local runtime_dir=""
  case "$runtime_arch" in
    arm64)
      runtime_dir="$BUILD_SERVER_RES_ARM64"
      ;;
    x86_64)
      runtime_dir="$BUILD_SERVER_RES_X64"
      ;;
    *)
      echo "❌ Unsupported SERVER_RUNTIME_ARCH: $runtime_arch (expected arm64 or x86_64)" >&2
      exit 1
      ;;
  esac

  if [ ! -d "$runtime_dir" ]; then
    echo "❌ Missing server runtime folder: $runtime_dir" >&2
    exit 1
  fi

  # Prefer the freshly built framework-dependent server apphost if available,
  # so code changes from `dotnet build` are reflected in the packaged app.
  local server_bin_src="$REPO_ROOT/Build/Server/XtremeWorlds.Server"
  if [ ! -f "$server_bin_src" ]; then
    # Otherwise use the matching-arch published server binary from the runtime folder.
    # (A universal binary + single-arch runtime can cause launch failures.)
    server_bin_src="$runtime_dir/XtremeWorlds.Server"
    if [ ! -f "$server_bin_src" ]; then
      # Fallback to universal if present.
      if [ -f "$BUILD_SERVER_UNIVERSAL" ]; then
        server_bin_src="$BUILD_SERVER_UNIVERSAL"
      else
        echo "❌ Missing server binary (expected $REPO_ROOT/Build/Server/XtremeWorlds.Server, $runtime_dir/XtremeWorlds.Server, or $BUILD_SERVER_UNIVERSAL)" >&2
        exit 1
      fi
    fi
  fi

  echo "Preparing Server app bundle: $SERVER_APP_OUT"
  rm -rf "$SERVER_APP_OUT"
  mkdir -p "$SERVER_APP_OUT/Contents/MacOS" "$SERVER_APP_OUT/Contents/Resources"

  # Wrapper script:
  # - If launched from Finder (no TTY), open Terminal and run the server.
  # - If launched from an existing terminal, run directly.
  cat > "$SERVER_APP_OUT/Contents/MacOS/XtremeWorlds Server" <<'EOF'
#!/bin/bash
set -euo pipefail
APP_DIR="$(cd "$(dirname "$0")" && pwd)"
CONTENTS_DIR="$(cd "$APP_DIR/.." && pwd)"
RES_DIR="$CONTENTS_DIR/Resources"

run_server() {
  cd "$RES_DIR"
  exec "$APP_DIR/XtremeWorlds.Server" "$@"
}

# If we have a terminal, run in-place.
if [ -t 0 ] && [ -t 1 ]; then
  run_server "$@"
fi

# Finder launch: open Terminal on the .command runner (no AppleScript permissions).
# Prefer opening the .command directly (same behavior as double-clicking it in Finder).
# This is more reliable than forcing Terminal as the handler.
open "$RES_DIR/RunServer.command" || open -a Terminal "$RES_DIR/RunServer.command"

exit 0
EOF
  chmod +x "$SERVER_APP_OUT/Contents/MacOS/XtremeWorlds Server"

  # Main server binary (matching the bundled runtime payload)
  cp "$server_bin_src" "$SERVER_APP_OUT/Contents/MacOS/XtremeWorlds.Server"
  chmod +x "$SERVER_APP_OUT/Contents/MacOS/XtremeWorlds.Server"

  # Resources (match the chosen architecture)
  rsync -a --delete "$runtime_dir/" "$SERVER_APP_OUT/Contents/Resources/"

  # Terminal runner script (double-clicking/opening will run in Terminal)
  # NOTE: Must be created after the rsync above, because rsync uses --delete.
  cat > "$SERVER_APP_OUT/Contents/Resources/RunServer.command" <<'EOF'
#!/bin/bash
set -euo pipefail

RES_DIR="$(cd "$(dirname "$0")" && pwd)"
MACOS_DIR="$(cd "$RES_DIR/../MacOS" && pwd)"

cd "$RES_DIR"
"$MACOS_DIR/XtremeWorlds.Server" "$@"
status=$?
echo ""
echo "Server exited with status $status"
if [ "${XTW_NO_PAUSE:-0}" != "1" ]; then
  read -n 1 -s -r -p "Press any key to close..."
  echo ""
fi
exit $status
EOF
  chmod +x "$SERVER_APP_OUT/Contents/Resources/RunServer.command"

  # Avoid having a second server binary in Resources; the launcher uses the one in MacOS.
  rm -f "$SERVER_APP_OUT/Contents/Resources/XtremeWorlds.Server" 2>/dev/null || true

    # Remove incomplete self-contained hostfxr bits from the bundle so the framework-dependent
    # server apphost uses the globally installed .NET runtime.
    rm -f "$SERVER_APP_OUT/Contents/Resources/libhostfxr.dylib" \
      "$SERVER_APP_OUT/Contents/Resources/libhostpolicy.dylib" 2>/dev/null || true

  # Overlay freshly built managed assemblies/config into the bundle so changes
  # from `dotnet build` are reflected without needing to rebuild the publish output.
  local server_build_dir="$REPO_ROOT/Build/Server"
  if [ -d "$server_build_dir" ]; then
    for f in \
      "XtremeWorlds.Server.dll" \
      "XtremeWorlds.Server.deps.json" \
      "XtremeWorlds.Server.runtimeconfig.json" \
      "XtremeWorlds.Server.pdb"; do
      if [ -f "$server_build_dir/$f" ]; then
        cp "$server_build_dir/$f" "$SERVER_APP_OUT/Contents/Resources/$f"
      fi
    done
  fi

  # Some parts of the server may resolve file paths relative to the executable directory
  # (Contents/MacOS) instead of the current working directory (Contents/Resources).
  # Keep the canonical copy in Resources, but provide symlinks in MacOS.
  local macos_dir="$SERVER_APP_OUT/Contents/MacOS"
  local resources_dir="$SERVER_APP_OUT/Contents/Resources"

  # Core runtime/config files commonly expected alongside the executable
  local link_names=(
    "XtremeWorlds.Server.deps.json"
    "XtremeWorlds.Server.runtimeconfig.json"
    "XtremeWorlds.Server.dll"
    "appsettings.json"
    "Switches.json"
    "Variables.json"
    "Database"
    "cs" "de" "es" "fr" "it" "ja" "ko" "pl" "pt-BR" "ru" "tr" "zh-Hans" "zh-Hant"
  )

  for name in "${link_names[@]}"; do
    if [ -e "$resources_dir/$name" ] && [ ! -e "$macos_dir/$name" ]; then
      (cd "$macos_dir" && ln -s "../Resources/$name" "$name")
    fi
  done

  # Keep non-code configuration out of Contents/MacOS (avoid codesign treating it as nested code)
  if [ -d "$macos_dir/Config" ] && [ ! -e "$resources_dir/Config" ]; then
    mv "$macos_dir/Config" "$resources_dir/Config"
  fi
  if [ -d "$resources_dir/Config" ] && [ ! -e "$macos_dir/Config" ]; then
    (cd "$macos_dir" && ln -s "../Resources/Config" "Config")
  fi

  # Minimal Info.plist
  cat > "$SERVER_APP_OUT/Contents/Info.plist" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>XtremeWorlds Server</string>
  <key>CFBundleDisplayName</key><string>XtremeWorlds Server</string>
  <key>CFBundleIdentifier</key><string>com.treeflyx.xtremeworlds.server</string>
  <key>CFBundleVersion</key><string>1.0.0</string>
  <key>CFBundleShortVersionString</key><string>1.0.0</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleExecutable</key><string>XtremeWorlds Server</string>
</dict>
</plist>
EOF
}

generate_icns_from_source() {
  local src="$1"
  local out_icns="$2"

  if [ ! -f "$src" ]; then
    echo "❌ Missing icon file: $src" >&2
    exit 1
  fi

  local tmp_dir
  tmp_dir="$(mktemp -d)"
  local src_png="$tmp_dir/source.png"
  local iconset="$tmp_dir/XtremeWorlds.iconset"
  mkdir -p "$iconset"

  # Convert source -> PNG (sips supports .png; .ico support can vary)
  if ! sips -s format png "$src" --out "$src_png" >/dev/null; then
    echo "❌ Failed to convert icon to PNG via sips: $src" >&2
    echo "   Tip: Use a PNG source (e.g. $HOME/Desktop/Icon.png)" >&2
    exit 1
  fi

  # Create required icon sizes
  local sizes=(16 32 128 256 512)
  for s in "${sizes[@]}"; do
    sips -z "$s" "$s" "$src_png" --out "$iconset/icon_${s}x${s}.png" >/dev/null
    local s2=$((s * 2))
    sips -z "$s2" "$s2" "$src_png" --out "$iconset/icon_${s}x${s}@2x.png" >/dev/null
  done

  iconutil -c icns "$iconset" -o "$out_icns"
  rm -rf "$tmp_dir"
}

set_app_icon() {
  local app="$1"
  local icns_src="$2"

  local resources_dir="$app/Contents/Resources"
  local plist="$app/Contents/Info.plist"
  if [ ! -d "$resources_dir" ] || [ ! -f "$plist" ]; then
    echo "⚠️  Skipping icon for non-standard app bundle: $app"
    return 0
  fi

  echo "Setting app icon for: $app"
  mkdir -p "$resources_dir"
  cp "$icns_src" "$resources_dir/XtremeWorlds.icns"

  # Ensure Info.plist points at the icon (value is typically without extension)
  /usr/libexec/PlistBuddy -c "Set :CFBundleIconFile XtremeWorlds" "$plist" 2>/dev/null \
    || /usr/libexec/PlistBuddy -c "Add :CFBundleIconFile string XtremeWorlds" "$plist"

  # Newer macOS versions also respect CFBundleIconName for Finder display.
  /usr/libexec/PlistBuddy -c "Set :CFBundleIconName XtremeWorlds" "$plist" 2>/dev/null \
    || /usr/libexec/PlistBuddy -c "Add :CFBundleIconName string XtremeWorlds" "$plist"

  # Nudge Finder to refresh the icon for the bundle.
  touch "$app" 2>/dev/null || true
}

codesign_keychain_args() {
  # Only pass --keychain if the requested identity exists in that keychain.
  # Otherwise, let codesign search the default keychain list (this is the most reliable).
  if [ -z "${KEYCHAIN_PATH:-}" ] || [ ! -f "$KEYCHAIN_PATH" ]; then
    return 0
  fi

  if security find-identity -v -p codesigning "$KEYCHAIN_PATH" 2>/dev/null | grep -q "$DEV_ID_CERT"; then
    echo "--keychain" "$KEYCHAIN_PATH"
    return 0
  fi

  echo "ℹ️  Identity not found in KEYCHAIN_PATH; not restricting codesign keychain." >&2
  echo "    KEYCHAIN_PATH=$KEYCHAIN_PATH" >&2
  return 0
}

# Function to sign a binary
sign_binary() {
  local binary="$1"
  echo "Signing binary: $binary"
  local output
  if ! output=$(codesign --force --verbose --timestamp --options runtime \
    --sign "$DEV_ID_CERT" \
    $(codesign_keychain_args) \
    --entitlements "$ENTITLEMENTS" \
    "$binary" 2>&1); then
    echo "$output" >&2
    echo "❌ Failed to sign $binary" >&2
    exit 1
  fi
  echo "$output"
}

sign_app_bundle() {
  local app="$1"
  echo "Signing app bundle: $app"
  local output
  if ! output=$(codesign --force --verbose --timestamp --options runtime \
    --sign "$DEV_ID_CERT" \
    $(codesign_keychain_args) \
    "$app" 2>&1); then
    echo "$output" >&2
    echo "❌ Failed to sign app bundle $app" >&2
    exit 1
  fi
  echo "$output"
}

is_macho_file() {
  local p="$1"
  # Only sign actual Mach-O binaries/libraries; skip scripts and data files.
  file -b "$p" 2>/dev/null | grep -q "Mach-O"
}

sign_macho_dir() {
  local dir="$1"
  if [ ! -d "$dir" ]; then
    return 0
  fi

  echo "Signing Mach-O binaries in $dir..."

  # 1) Sign native libraries first (common requirement for signing executables that load them).
  find "$dir" -type f \( -name "*.dylib" -o -name "*.so" \) | while read -r f; do
    if is_macho_file "$f"; then
      sign_binary "$f"
    fi
  done

  # 2) Then sign remaining Mach-O files (executables, apphosts, helpers).
  find "$dir" -type f ! -name "*.dylib" ! -name "*.so" | while read -r f; do
    if is_macho_file "$f"; then
      sign_binary "$f"
    fi
  done
}

sanitize_app_bundle() {
  local app="$1"
  # Remove extended attributes (quarantine/resource forks) that can break signing.
  if command -v xattr >/dev/null 2>&1; then
    xattr -cr "$app" 2>/dev/null || true
  fi
}

clean_runtime_artifacts() {
  local app="$1"
  # Remove runtime-generated logs or artifacts that should never be shipped.
  rm -rf \
    "$app/Contents/MacOS/Logs" \
    "$app/Contents/MacOS/Logs"* \
    "$app/Contents/Resources/logs" \
    "$app/Contents/Resources/logs"* \
    2>/dev/null || true
}

notary_profile_exists() {
  local profile="$1"
  xcrun notarytool info --keychain-profile "$profile" >/dev/null 2>&1
}

ensure_notary_profile() {
  local profile="$1"
  if notary_profile_exists "$profile"; then
    return 0
  fi

  # If the user provided a NOTARY_PASSWORD, we can create the profile non-interactively.
  # This avoids requiring AUTO_STORE_CREDENTIALS for CI or scripted runs.
  if [ -n "${NOTARY_PASSWORD:-}" ]; then
    echo "ℹ️  Storing notarytool credentials into Keychain profile: $profile"
    xcrun notarytool store-credentials "$profile" --apple-id "$APPLE_ID" --team-id "$TEAM_ID" --password "$NOTARY_PASSWORD"
    notary_profile_exists "$profile" || { echo "❌ Failed to create profile: $profile" >&2; exit 1; }
    return 0
  fi

  echo "❌ No Keychain password item found for profile: $profile" >&2
  echo "   Create it with:" >&2
  echo "     xcrun notarytool store-credentials \"$profile\" --apple-id \"$APPLE_ID\" --team-id \"$TEAM_ID\"" >&2
  echo "   (You'll be prompted for your app-specific password.)" >&2
  echo "" >&2
  echo "   Optional non-interactive:" >&2
  echo "     NOTARY_PASSWORD=\"<app-specific-password>\" xcrun notarytool store-credentials \"$profile\" --apple-id \"$APPLE_ID\" --team-id \"$TEAM_ID\" --password \"$NOTARY_PASSWORD\"" >&2
  echo "" >&2
  echo "   Or set AUTO_STORE_CREDENTIALS=1 to have this script prompt you." >&2

  if [ "${AUTO_STORE_CREDENTIALS:-0}" = "1" ]; then
    echo "ℹ️  Storing notarytool credentials into Keychain profile: $profile"
    if [ -n "${NOTARY_PASSWORD:-}" ]; then
      xcrun notarytool store-credentials "$profile" --apple-id "$APPLE_ID" --team-id "$TEAM_ID" --password "$NOTARY_PASSWORD"
    else
      xcrun notarytool store-credentials "$profile" --apple-id "$APPLE_ID" --team-id "$TEAM_ID"
    fi
    notary_profile_exists "$profile" || { echo "❌ Failed to create profile: $profile" >&2; exit 1; }
    return 0
  fi

  exit 1
}

ensure_signing_identity() {
  # Prefer Developer ID Application for distribution
  if security find-identity -v -p codesigning | grep -q "Developer ID Application"; then
    if security find-identity -v -p codesigning | grep -q "$DEV_ID_CERT"; then
      return 0
    fi
    # If the requested value isn't found, auto-pick a Developer ID Application identity.
    # Prefer an identity that matches TEAM_ID (if provided).
    local picked
    if [ -n "${TEAM_ID:-}" ]; then
      picked="$(security find-identity -v -p codesigning | grep "Developer ID Application" | grep "(${TEAM_ID})" | head -n 1 | sed -E 's/^\s*[0-9]+\) ([0-9A-F]{40}) .*/\1/')"
    fi
    if [ -z "${picked:-}" ]; then
      picked="$(security find-identity -v -p codesigning | grep "Developer ID Application" | head -n 1 | sed -E 's/^\s*[0-9]+\) ([0-9A-F]{40}) .*/\1/')"
    fi
    if [ -n "$picked" ]; then
      echo "ℹ️  DEV_ID_CERT not found; using first Developer ID Application identity: $picked"
      DEV_ID_CERT="$picked"
      return 0
    fi
  fi

  # Fall back: allow any valid identity if explicitly provided by user.
  if security find-identity -v -p codesigning | grep -q "$DEV_ID_CERT"; then
    return 0
  fi

  echo "❌ No valid code-signing identity found for: $DEV_ID_CERT" >&2
  echo "   Available identities:" >&2
  security find-identity -v -p codesigning >&2 || true
  echo "" >&2
  echo "   Fix:" >&2
  echo "   - Install/import a 'Developer ID Application' certificate into your login keychain" >&2
  echo "   - Or run with DEV_ID_CERT set to an identity shown above" >&2
  exit 1
}

# Build/prepare app bundles and apply icon before signing
copy_client_app
create_server_app

for app in "${APPS[@]}"; do
  sanitize_app_bundle "$app"
  clean_runtime_artifacts "$app"
done

ICNS_TMP_DIR="$(mktemp -d -t xtremeworlds_icon)"
ICNS_TMP="$ICNS_TMP_DIR/XtremeWorlds.icns"
generate_icns_from_source "$ICON_PATH" "$ICNS_TMP"
for app in "${APPS[@]}"; do
  set_app_icon "$app" "$ICNS_TMP"
done
rm -rf "$ICNS_TMP_DIR"

if [ "$PREPARE_ONLY" = "1" ]; then
  echo "✅ Prepared Client + Server app bundles (PREPARE_ONLY=1)."
  echo "   Client: $CLIENT_APP_OUT"
  echo "   Server: $SERVER_APP_OUT"
  exit 0
fi

ensure_signing_identity

# Re-sign createdump files (if they exist)
for app in "${APPS[@]}"; do
  for CREATEDUMP in "$app/Contents/MacOS/createdump" "$app/Contents/Resources/createdump"; do
    if [ -f "$CREATEDUMP" ]; then
      sign_binary "$CREATEDUMP"
    fi
  done
done

# Sign native libraries first, then executables (Mach-O only)
for app in "${APPS[@]}"; do
  sign_macho_dir "$app/Contents/MacOS"
  sign_macho_dir "$app/Contents/Resources"
done

# Finally sign the .app bundles themselves (important after modifying Resources/Info.plist)
for app in "${APPS[@]}"; do
  sign_app_bundle "$app"
done

echo "✅ All components signed successfully."

if [ "$NOTARIZE" != "1" ]; then
  echo "ℹ️  Skipping notarization (NOTARIZE=0)."
  exit 0
fi

# Zip the apps for notarization
FINAL_ZIP="${FINAL_ZIP:-$HOME_DIR/XtremeWorlds.zip}"
echo "Creating combined ZIP archive: $FINAL_ZIP"
rm -f "$FINAL_ZIP" 2>/dev/null || true

# Use ditto to preserve symlinks, extended attributes, and avoid __MACOSX.
ZIP_STAGING_DIR="$(mktemp -d -t xtremeworlds_zip)"
ZIP_STAGING_PAYLOAD="$ZIP_STAGING_DIR/payload"
mkdir -p "$ZIP_STAGING_PAYLOAD"
for app in "${APPS[@]}"; do
  ditto "$app" "$ZIP_STAGING_PAYLOAD/$(basename "$app")"
done
ditto -c -k --sequesterRsrc "$ZIP_STAGING_PAYLOAD" "$FINAL_ZIP"
rm -rf "$ZIP_STAGING_DIR"
echo "✅ Created $FINAL_ZIP"

# Submit the ZIP to Apple for notarization
echo "Submitting $FINAL_ZIP to Apple for notarization..."

if [ -z "$NOTARY_PROFILE" ] && [ -z "$NOTARY_PASSWORD" ]; then
  # Default behavior: use a standard keychain profile name.
  # Create it once via:
  #   xcrun notarytool store-credentials --apple-id "$APPLE_ID" --team-id "$TEAM_ID" --password "<app-specific-password>" --keychain-profile notarytool
  NOTARY_PROFILE="notarytool"
  echo "ℹ️  Using default notarytool keychain profile: $NOTARY_PROFILE"
fi

if [ -n "$NOTARY_PROFILE" ]; then
  ensure_notary_profile "$NOTARY_PROFILE"
  xcrun notarytool submit "$FINAL_ZIP" \
    --keychain-profile "$NOTARY_PROFILE" \
    --wait || { echo "❌ Notarization failed"; exit 1; }
else
  if [ -z "$NOTARY_PASSWORD" ]; then
    echo "❌ NOTARY_PASSWORD is not set." >&2
    echo "   Either set NOTARY_PROFILE (recommended) or set NOTARY_PASSWORD for notarytool." >&2
    echo "   Tip: create a keychain profile once with:" >&2
    echo "     xcrun notarytool store-credentials --apple-id \"$APPLE_ID\" --team-id \"$TEAM_ID\" --password \"<app-specific-password>\" --keychain-profile notarytool" >&2
    exit 1
  fi
  xcrun notarytool submit "$FINAL_ZIP" \
    --apple-id "$APPLE_ID" \
    --team-id "$TEAM_ID" \
    --password "$NOTARY_PASSWORD" \
    --wait || { echo "❌ Notarization failed"; exit 1; }
fi

echo "Stapling notarization tickets to apps..."
for app in "${APPS[@]}"; do
  xcrun stapler staple "$app" || { echo "❌ Stapling failed for $app"; exit 1; }
  xcrun stapler validate "$app" || { echo "❌ Staple validation failed for $app"; exit 1; }
done

echo "✅ Notarization completed successfully."
