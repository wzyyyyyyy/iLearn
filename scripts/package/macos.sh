#!/usr/bin/env bash
set -euo pipefail

RID="${1:-osx-arm64}"
VERSION="${VERSION:-$(git describe --tags --always --dirty 2>/dev/null || echo local)}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PUBLISH_DIR="$ROOT/artifacts/publish/$RID"
PACKAGE_DIR="$ROOT/artifacts/package/$RID"
APP_DIR="$PACKAGE_DIR/iLearn.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
DMG_PATH="$ROOT/artifacts/package/iLearn-$VERSION-$RID-unsigned.dmg"
ZIP_PATH="$ROOT/artifacts/package/iLearn-$VERSION-$RID.zip"

rm -rf "$PUBLISH_DIR" "$PACKAGE_DIR" "$DMG_PATH" "$ZIP_PATH"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"

dotnet publish "$ROOT/iLearn/iLearn.csproj" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -o "$PUBLISH_DIR"

cp -R "$PUBLISH_DIR"/. "$MACOS_DIR/"
chmod +x "$MACOS_DIR/iLearn"

cat > "$CONTENTS_DIR/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>iLearn</string>
  <key>CFBundleIdentifier</key>
  <string>com.wzyyyyyyy.ilearn</string>
  <key>CFBundleName</key>
  <string>iLearn</string>
  <key>CFBundleDisplayName</key>
  <string>iLearn</string>
  <key>CFBundleVersion</key>
  <string>$VERSION</string>
  <key>CFBundleShortVersionString</key>
  <string>$VERSION</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

if command -v hdiutil >/dev/null 2>&1; then
  hdiutil create -volname "iLearn" -srcfolder "$APP_DIR" -ov -format UDZO "$DMG_PATH"
fi

(cd "$PACKAGE_DIR" && zip -qry "$ZIP_PATH" "iLearn.app")

echo "Created:"
if [[ -f "$DMG_PATH" ]]; then
  echo "  $DMG_PATH"
fi
echo "  $ZIP_PATH"
