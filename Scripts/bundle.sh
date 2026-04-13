#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

echo "Building Pausa (release)..."
swift build -c release

BUILT=$(swift build -c release --show-bin-path)/Pausa
APP="Pausa.app"
CONTENTS="$APP/Contents"

rm -rf "$APP"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"

cp "$BUILT" "$CONTENTS/MacOS/Pausa"
cp "Scripts/AppIcon.icns" "$CONTENTS/Resources/AppIcon.icns"

cat > "$CONTENTS/Info.plist" << 'PLIST'
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
    <string>1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>LSUIElement</key>
    <true/>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
</dict>
</plist>
PLIST

echo "Codesigning..."
codesign --force --sign - "$APP"

echo "Done! Created $APP"
