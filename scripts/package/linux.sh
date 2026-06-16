#!/usr/bin/env bash
set -euo pipefail

RID="${1:-linux-x64}"
VERSION="${VERSION:-$(git describe --tags --always --dirty 2>/dev/null || echo local)}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PUBLISH_DIR="$ROOT/artifacts/publish/$RID"
PACKAGE_ROOT="$ROOT/artifacts/package"
APP_DIR="$PACKAGE_ROOT/iLearn-$VERSION-$RID"
TAR_PATH="$PACKAGE_ROOT/iLearn-$VERSION-$RID.tar.gz"
DEB_ROOT="$PACKAGE_ROOT/deb-root"
DEB_PATH="$PACKAGE_ROOT/iLearn-$VERSION-$RID.deb"

rm -rf "$PUBLISH_DIR" "$APP_DIR" "$TAR_PATH" "$DEB_ROOT" "$DEB_PATH"
mkdir -p "$APP_DIR" "$PACKAGE_ROOT"

dotnet publish "$ROOT/iLearn/iLearn.csproj" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -o "$PUBLISH_DIR"

cp -R "$PUBLISH_DIR"/. "$APP_DIR/"
chmod +x "$APP_DIR/iLearn"

(cd "$PACKAGE_ROOT" && tar -czf "$TAR_PATH" "iLearn-$VERSION-$RID")

if command -v dpkg-deb >/dev/null 2>&1; then
  mkdir -p "$DEB_ROOT/DEBIAN" "$DEB_ROOT/opt/iLearn" "$DEB_ROOT/usr/bin"
  cp -R "$PUBLISH_DIR"/. "$DEB_ROOT/opt/iLearn/"
  chmod +x "$DEB_ROOT/opt/iLearn/iLearn"
  ln -s /opt/iLearn/iLearn "$DEB_ROOT/usr/bin/ilearn"
  cat > "$DEB_ROOT/DEBIAN/control" <<CONTROL
Package: ilearn
Version: $VERSION
Section: education
Priority: optional
Architecture: amd64
Maintainer: iLearn contributors
Description: Cross-platform iLearn desktop client
CONTROL
  dpkg-deb --build "$DEB_ROOT" "$DEB_PATH"
fi

echo "Created:"
echo "  $TAR_PATH"
if [[ -f "$DEB_PATH" ]]; then
  echo "  $DEB_PATH"
fi
