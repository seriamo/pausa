#!/usr/bin/env bash
# Builds the Mac App Store package: a sandboxed, universal Pausa.app without the
# self-update check, wrapped in a signed Pausa.pkg ready for App Store Connect.
#
#   Scripts/bundle-appstore.sh            build, sign and validate Pausa.pkg
#   Scripts/bundle-appstore.sh --upload   ...then upload it to App Store Connect
#   Scripts/bundle-appstore.sh --local    ad-hoc signed, sandboxed Pausa.app to test on this Mac
#                                         (bundle ID com.seriamo.pausa.localtest)
#
# Needs Xcode, the "3rd Party Mac Developer Application" and "3rd Party Mac Developer
# Installer" certificates in the keychain, and the Mac App Store provisioning profile at
# $PROFILE. Validation and upload use the App Store Connect API key in ~/.appstoreconnect/.
# The build number defaults to the git commit count; set BUILD to override it.
set -euo pipefail

cd "$(dirname "$0")/.."

export DEVELOPER_DIR="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"

VERSION=$(sed -n 's/^VERSION="\(.*\)"$/\1/p' Scripts/bundle.sh)
BUILD="${BUILD:-$(git rev-list --count HEAD)}"
if ! grep -q "currentVersion = \"$VERSION\"" Sources/Pausa/main.swift; then
    echo "Version mismatch: Scripts/bundle.sh says $VERSION but main.swift's currentVersion differs." >&2
    exit 1
fi

TEAM="FVAPC8DNSW"
APP_SIGN_ID="3rd Party Mac Developer Application: Masieri Ventures LLC ($TEAM)"
PKG_SIGN_ID="3rd Party Mac Developer Installer: Masieri Ventures LLC ($TEAM)"
ENTITLEMENTS="Scripts/entitlements-appstore.plist"
PROFILE="${PROFILE:-$HOME/.appstoreconnect/profiles/Pausa_Mac_App_Store.provisionprofile}"
ASC_CONFIG="$HOME/.appstoreconnect/seriamo.json"

OUT="build/appstore"
APP="$OUT/Pausa.app"
PKG="$OUT/Pausa.pkg"
CONTENTS="$APP/Contents"

MODE="pkg"
UPLOAD=0
for arg in "$@"; do
    case "$arg" in
        --local) MODE="local" ;;
        --upload) UPLOAD=1 ;;
        *) echo "Unknown option: $arg" >&2; exit 1 ;;
    esac
done

if [[ "$MODE" == "pkg" ]]; then
    security find-identity -v -p codesigning | grep -q "$APP_SIGN_ID" \
        || { echo "Missing signing identity: $APP_SIGN_ID" >&2; exit 1; }
    security find-identity -v | grep -q "$PKG_SIGN_ID" \
        || { echo "Missing installer identity: $PKG_SIGN_ID" >&2; exit 1; }
    [[ -f "$PROFILE" ]] || { echo "Missing provisioning profile: $PROFILE" >&2; exit 1; }
fi

echo "Building Pausa $VERSION ($BUILD) for the Mac App Store (universal, release)..."
swift build -c release --arch arm64 --arch x86_64 -Xswiftc -DAPP_STORE
BUILT="$(swift build -c release --arch arm64 --arch x86_64 --show-bin-path)/Pausa"

rm -rf "$OUT"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"
cp "$BUILT" "$CONTENTS/MacOS/Pausa"
cp "Scripts/AppIcon.icns" "$CONTENTS/Resources/AppIcon.icns"

SDK_VERSION=$(xcrun --sdk macosx --show-sdk-version)
SDK_BUILD=$(xcrun --sdk macosx --show-sdk-build-version)

# SwiftPM's link step stamps the deployment target as the SDK version (sdk 14.0), and App
# Store Connect rejects builds from old SDKs, so record the SDK it was actually built with.
MIN_OS=$(otool -arch arm64 -l "$CONTENTS/MacOS/Pausa" | awk '/LC_BUILD_VERSION/ {f=1} f && $1 == "minos" {print $2; exit}')
LD_VERSION=$(otool -arch arm64 -l "$CONTENTS/MacOS/Pausa" | awk '/LC_BUILD_VERSION/ {f=1} f && $1 == "version" {print $2; exit}')
xcrun vtool -set-build-version macos "$MIN_OS" "$SDK_VERSION" -tool ld "$LD_VERSION" -replace \
    -output "$CONTENTS/MacOS/Pausa.stamped" "$CONTENTS/MacOS/Pausa"
mv "$CONTENTS/MacOS/Pausa.stamped" "$CONTENTS/MacOS/Pausa"
XCODE_BUILD=$(xcodebuild -version | awk '/Build version/ {print $3}')
XCODE_VERSION=$(xcodebuild -version | awk '/^Xcode/ {print $2}')
# DTXcode is the Xcode version as four digits, e.g. 27.2 -> 2720
XCODE_MAJOR=${XCODE_VERSION%%.*}
XCODE_REST=${XCODE_VERSION#*.}
[[ "$XCODE_REST" == "$XCODE_VERSION" ]] && XCODE_REST=0
XCODE_MINOR=${XCODE_REST%%.*}
DT_XCODE=$(printf '%d%d0' "$XCODE_MAJOR" "$XCODE_MINOR")
OS_BUILD=$(sw_vers -buildVersion)

cat > "$CONTENTS/Info.plist" << PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>Pausa</string>
    <key>CFBundleIdentifier</key>
    <string>com.seriamo.pausa</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>Pausa</string>
    <key>CFBundleDisplayName</key>
    <string>Pausa</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$BUILD</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleSupportedPlatforms</key>
    <array>
        <string>MacOSX</string>
    </array>
    <key>LSUIElement</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>14.0</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.healthcare-fitness</string>
    <key>ITSAppUsesNonExemptEncryption</key>
    <false/>
    <key>NSHumanReadableCopyright</key>
    <string>© 2026 Seriamo</string>
    <key>BuildMachineOSBuild</key>
    <string>$OS_BUILD</string>
    <key>DTCompiler</key>
    <string>com.apple.compilers.llvm.clang.1_0</string>
    <key>DTPlatformBuild</key>
    <string>$SDK_BUILD</string>
    <key>DTPlatformName</key>
    <string>macosx</string>
    <key>DTPlatformVersion</key>
    <string>$SDK_VERSION</string>
    <key>DTSDKBuild</key>
    <string>$SDK_BUILD</string>
    <key>DTSDKName</key>
    <string>macosx$SDK_VERSION</string>
    <key>DTXcode</key>
    <string>$DT_XCODE</string>
    <key>DTXcodeBuild</key>
    <string>$XCODE_BUILD</string>
</dict>
</plist>
PLIST
plutil -lint "$CONTENTS/Info.plist" >/dev/null

if [[ "$MODE" == "local" ]]; then
    # A sandboxed app's first launch moves ~/Library/Preferences/<bundle id>.plist into its
    # container. A separate bundle ID keeps this test build away from the installed Pausa's settings.
    /usr/libexec/PlistBuddy -c "Set :CFBundleIdentifier com.seriamo.pausa.localtest" "$CONTENTS/Info.plist"
    # Sandbox only: the application-identifier entitlements need the store profile.
    LOCAL_ENT="$OUT/entitlements-local.plist"
    /usr/libexec/PlistBuddy -c "Add :com.apple.security.app-sandbox bool true" \
        -c "Add :com.apple.security.network.client bool true" "$LOCAL_ENT" >/dev/null
    codesign --force --sign - --entitlements "$LOCAL_ENT" "$APP"
    codesign --verify --verbose=2 "$APP"
    echo ""
    echo "Done! $APP is ad-hoc signed and sandboxed for local testing (not for upload)."
    exit 0
fi

cp "$PROFILE" "$CONTENTS/embedded.provisionprofile"

echo "Codesigning for the Mac App Store..."
codesign --force --options runtime --timestamp --sign "$APP_SIGN_ID" \
    --entitlements "$ENTITLEMENTS" "$APP"
codesign --verify --strict --verbose=2 "$APP"

echo "Building installer package..."
productbuild --component "$APP" /Applications --sign "$PKG_SIGN_ID" "$PKG"
pkgutil --check-signature "$PKG"

KEY_ID=$(python3 -c "import json; print(json.load(open('$ASC_CONFIG'))['keyId'])")
ISSUER_ID=$(python3 -c "import json; print(json.load(open('$ASC_CONFIG'))['issuerId'])")

echo "Validating with App Store Connect..."
xcrun altool --validate-app -f "$PKG" -t macos --apiKey "$KEY_ID" --apiIssuer "$ISSUER_ID"

if [[ "$UPLOAD" -eq 1 ]]; then
    echo "Uploading Pausa $VERSION ($BUILD) to App Store Connect..."
    xcrun altool --upload-app -f "$PKG" -t macos --apiKey "$KEY_ID" --apiIssuer "$ISSUER_ID"
    echo "Uploaded. Attach build $BUILD once it finishes processing."
else
    echo ""
    echo "Done! Created $PKG (Pausa $VERSION, build $BUILD). Re-run with --upload to send it."
fi
