#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

VERSION="1.0"

SIGN_ID="Developer ID Application: Masieri Ventures LLC (FVAPC8DNSW)"
ENTITLEMENTS="Scripts/entitlements.plist"

NOTARY_KEY="docs/certs/AuthKey_FYLUNAP39B.p8"
NOTARY_KEY_ID="FYLUNAP39B"
NOTARY_ISSUER="dfed3295-1bca-43ec-947a-fde7c2e8363b"

APP="Pausa.app"
DMG="Pausa.dmg"
CONTENTS="$APP/Contents"

# Pass --no-notarize to skip notarization (faster local builds)
NOTARIZE=1
for arg in "$@"; do
    case "$arg" in
        --no-notarize) NOTARIZE=0 ;;
    esac
done

echo "Building Pausa $VERSION (release)..."
swift build -c release

BUILT=$(swift build -c release --show-bin-path)/Pausa

rm -rf "$APP" "$DMG"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"

cp "$BUILT" "$CONTENTS/MacOS/Pausa"
cp "Scripts/AppIcon.icns" "$CONTENTS/Resources/AppIcon.icns"

cat > "$CONTENTS/Info.plist" << PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>Pausa</string>
    <key>CFBundleIdentifier</key>
    <string>com.seriamo.pausa</string>
    <key>CFBundleName</key>
    <string>Pausa</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>LSUIElement</key>
    <true/>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
</dict>
</plist>
PLIST

echo "Codesigning with Developer ID + hardened runtime..."
codesign --force --options runtime --sign "$SIGN_ID" \
    --entitlements "$ENTITLEMENTS" "$APP"

codesign --verify --verbose=2 "$APP"

echo "Building DMG..."
create-dmg \
  --volname "Pausa" \
  --volicon "Scripts/AppIcon.icns" \
  --background "Scripts/dmg-background-light.png" \
  --window-pos 200 120 \
  --window-size 600 450 \
  --icon-size 100 \
  --icon "Pausa.app" 150 185 \
  --app-drop-link 455 185 \
  --hide-extension "Pausa.app" \
  "$DMG" \
  "$APP"

if [[ "$NOTARIZE" -eq 0 ]]; then
    echo "Skipping notarization (--no-notarize)"
    echo "Done! Created $DMG (unnotarized)"
    exit 0
fi

echo "Submitting to Apple notary service (this can take a few minutes)..."
xcrun notarytool submit "$DMG" \
    --key "$NOTARY_KEY" \
    --key-id "$NOTARY_KEY_ID" \
    --issuer "$NOTARY_ISSUER" \
    --wait

echo "Stapling notarization ticket..."
xcrun stapler staple "$DMG"
xcrun stapler validate "$DMG"

echo "Verifying Gatekeeper acceptance..."
spctl --assess --type execute -v "$APP"

echo ""
echo "Done! Created notarized $DMG (Pausa $VERSION)"
