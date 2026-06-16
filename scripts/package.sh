#!/usr/bin/env bash
set -euo pipefail

RID="${1:-}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ -z "$RID" ]]; then
  echo "Usage: scripts/package.sh <win-x64|osx-x64|osx-arm64|linux-x64>"
  exit 2
fi

case "$RID" in
  osx-x64|osx-arm64)
    "$ROOT/scripts/package/macos.sh" "$RID"
    ;;
  linux-x64)
    "$ROOT/scripts/package/linux.sh" "$RID"
    ;;
  win-x64)
    if command -v pwsh >/dev/null 2>&1; then
      pwsh "$ROOT/scripts/package/windows.ps1" -Rid "$RID"
    elif command -v powershell >/dev/null 2>&1; then
      powershell -ExecutionPolicy Bypass -File "$ROOT/scripts/package/windows.ps1" -Rid "$RID"
    else
      echo "PowerShell is required to package $RID."
      exit 1
    fi
    ;;
  *)
    echo "Unsupported RID: $RID"
    exit 2
    ;;
esac
