#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
GODOT_BIN="${GODOT_BIN:-/home/gabriel/Documents/Godot/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64}"
if [[ ! -x "$GODOT_BIN" ]]; then
  echo 'Godot .NET was not found. Set GODOT_BIN to the executable path.'
  exit 1
fi
dotnet build Abyss.csproj --nologo
"$GODOT_BIN" --headless --editor --path . --import >/dev/null 2>&1
exec "$GODOT_BIN" --path . "$@"
