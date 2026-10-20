#!/usr/bin/env bash
# Install Macroverse's Kefka (Steam Deck) integration for the current user.
#
#   bash ~/Apps/macroverse/ops/kefka/install-kefka.sh [--defer-deckhypr]
#
# Expects the built app in ~/Apps/macroverse (Macroverse42, frontend-build/, shaders/ ...).
# Installs three user units, the read-only input bridge, the open-or-focus launcher, a menu
# entry and its icon. Every file it replaces is backed up as <file>.bak.<stamp> first.
# Adds ^macroverse-deck$ to ~/.config/kefka/game-classes once, keeping every existing line,
# and restarts deckhypr only when that file changed. With --defer-deckhypr the restart is
# left to the first macroverse-deck-open (useful while someone is using the Deck).
# Enables and (re)starts macroverse.service; never opens the Deck window.
set -euo pipefail
defer_deckhypr=0
for arg in "$@"; do
  case "$arg" in
    --defer-deckhypr) defer_deckhypr=1 ;;
    -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
    *) echo "unknown option: $arg" >&2; exit 2 ;;
  esac
done

here=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
app="$HOME/Apps/macroverse"
units="$HOME/.config/systemd/user"
share="$HOME/.local/share/macroverse"
icon_dir="$HOME/.local/share/icons/hicolor/scalable/apps"
apps_dir="$HOME/.local/share/applications"
classes="$HOME/.config/kefka/game-classes"
pattern='^macroverse-deck$'
export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-/run/user/$(id -u)}"
stamp=$(date +%Y%m%d-%H%M%S)

[[ -x "$app/Macroverse42" ]] || { echo "missing $app/Macroverse42 (deploy the build first)" >&2; exit 1; }
[[ -d "$app/frontend-build" ]] || { echo "missing $app/frontend-build" >&2; exit 1; }

mkdir -p "$units" "$share" "$icon_dir" "$apps_dir" "$HOME/.local/bin" \
  "$HOME/.config/kefka" "$HOME/.config/macroverse"

# install_file MODE SRC DEST: back up DEST when it differs, then install.
install_file() {
  local mode=$1 src=$2 dest=$3
  if [[ -e "$dest" ]]; then
    if cmp -s "$src" "$dest"; then return 0; fi
    cp -a "$dest" "$dest.bak.$stamp"
    echo "backed up $dest -> $dest.bak.$stamp"
  fi
  install -m "$mode" "$src" "$dest"
  echo "installed $dest"
}

# Runtime files the backend expects next to its executable.
if [[ ! -f "$app/shader-preview-settings.json" ]]; then
  cp "$app/shader-preview-settings.default.json" "$app/shader-preview-settings.json"
  echo "seeded $app/shader-preview-settings.json from the default"
fi
if [[ ! -f "$app/shader-index.json" ]]; then
  printf '[]' > "$app/shader-index.json"
  echo "seeded empty $app/shader-index.json (the first start scans shaders/)"
fi
if [[ ! -f "$app/thumbnails.json" && -f "$app/thumbnails-baked.json" ]]; then
  cp "$app/thumbnails-baked.json" "$app/thumbnails.json"
  echo "seeded $app/thumbnails.json from the baked thumbnails"
fi

for name in macroverse.service macroverse-input.service macroverse-deck.service; do
  install_file 644 "$here/$name" "$units/$name"
done
install_file 644 "$here/deck-input.py" "$share/deck-input.py"
install_file 755 "$here/macroverse-deck-open" "$HOME/.local/bin/macroverse-deck-open"
install_file 644 "$here/macroverse.svg" "$icon_dir/macroverse.svg"
desktop_tmp=$(mktemp)
trap 'rm -f "$desktop_tmp"' EXIT
sed "s|@HOME@|$HOME|g" "$here/macroverse.desktop" > "$desktop_tmp"
install_file 644 "$desktop_tmp" "$apps_dir/macroverse.desktop"

if [[ ! -e "$HOME/.config/macroverse/kefka.env" ]]; then
  cat > "$HOME/.config/macroverse/kefka.env" <<'ENV'
# Overrides for macroverse.service, macroverse-input.service and macroverse-deck.service.
# Restart the affected unit after editing. All lines are optional.
#
# Reach the backend from the LAN / tailnet (default is loopback only):
#MACROVERSE_BIND_HOST=0.0.0.0
# Page the Deck window opens. Keep the origin http://127.0.0.1:8765 so the input bridge trusts it:
#MACROVERSE_DECK_URL=http://127.0.0.1:8765/
# Input bridge (exact origins, comma-separated; window class; loopback port):
#MACROVERSE_DECK_ORIGINS=http://127.0.0.1:8765,http://localhost:8765
#MACROVERSE_DECK_CLASS=macroverse-deck
#MACROVERSE_DECK_INPUT_PORT=18783
ENV
  chmod 600 "$HOME/.config/macroverse/kefka.env"
  echo "created $HOME/.config/macroverse/kefka.env (all settings commented out)"
fi

# Game-class rule: only the dedicated Deck window leaves the kernel pad to the page.
if ! grep -Fxq "$pattern" "$classes" 2>/dev/null; then
  if [[ -f "$classes" ]]; then
    cp -a "$classes" "$classes.bak.$stamp"
    echo "backed up $classes -> $classes.bak.$stamp"
  fi
  python3 - "$classes" "$pattern" <<'PY'
import os, sys, tempfile
from pathlib import Path
path, pattern = Path(sys.argv[1]), sys.argv[2]
old = path.read_bytes() if path.exists() else b""
if pattern.encode() not in old.splitlines():
    new = old + (b"" if not old or old.endswith(b"\n") else b"\n")
    new += b"\n# Macroverse Deck: native pad, trackpads and gyro via macroverse-input.\n" + pattern.encode() + b"\n"
    fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=".game-classes.")
    with os.fdopen(fd, "wb") as f:
        f.write(new)
    if path.exists():
        os.chmod(tmp, path.stat().st_mode & 0o7777)
    os.replace(tmp, path)
PY
  echo "added $pattern to $classes"
  if systemctl --user is-active --quiet deckhypr.service; then
    if (( defer_deckhypr )); then
      # Only the deckhypr that is running now still has the old list; the launcher
      # restarts it (once) if this same process is still running at first open.
      cat "$XDG_RUNTIME_DIR/deckhypr/pid" > "$XDG_RUNTIME_DIR/macroverse-deckhypr-reload" 2>/dev/null || true
      echo "deckhypr restart deferred to the first macroverse-deck-open"
    else
      systemctl --user restart deckhypr.service
      echo "restarted deckhypr.service to load $classes"
    fi
  fi
else
  echo "$classes already has $pattern (unchanged; deckhypr not restarted)"
fi

systemctl --user daemon-reload
systemctl --user enable macroverse.service >/dev/null
if systemctl --user is-active --quiet macroverse.service; then
  systemctl --user restart macroverse.service
  echo "restarted macroverse.service"
else
  systemctl --user start macroverse.service
  echo "started macroverse.service"
fi
systemctl --user try-restart macroverse-input.service
echo "done. Open the Deck window with: $HOME/.local/bin/macroverse-deck-open"
