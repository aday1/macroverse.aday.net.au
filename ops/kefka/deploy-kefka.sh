#!/usr/bin/env bash
# Build the current working tree on a workstation and deploy it to Kefka's ~/Apps/macroverse.
#
#   ops/kefka/deploy-kefka.sh [BUILD_DIR] [--host kefka] [--install] [--defer-deckhypr]
#
# BUILD_DIR (default: a fresh mktemp dir) receives frontend/ (with node_modules linked, never
# copied), the vite output and the linux/amd64 binary, then app/ is rsynced to the host.
# Node modules: MACROVERSE_NODE_MODULES, else frontend/node_modules, else `npm ci` in BUILD_DIR.
# Kefka-side state is preserved: macroverse.db, shader-preview-settings.json, shader-index.json,
# thumbnails.json and anything added to shaders/ are never deleted. Stale frontend assets are.
# --install runs ops/kefka/install-kefka.sh on the host afterwards (it never opens the window).
set -euo pipefail
repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
host=kefka
build=""
install=0
install_args=()
while (( $# )); do
  case "$1" in
    --host) host=$2; shift 2 ;;
    --install) install=1; shift ;;
    --defer-deckhypr) install_args+=(--defer-deckhypr); shift ;;
    -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
    -*) echo "unknown option: $1" >&2; exit 2 ;;
    *) build=$1; shift ;;
  esac
done
[[ -n "$build" ]] || build=$(mktemp -d)
mkdir -p "$build"
build=$(cd -- "$build" && pwd)
app="$build/app"

echo "[1/4] frontend (vite build) in $build/frontend"
rm -rf "$build/frontend" "$app"
rsync -a --exclude node_modules --exclude dist "$repo/frontend/" "$build/frontend/"
modules=${MACROVERSE_NODE_MODULES:-}
if [[ -z "$modules" && -d "$repo/frontend/node_modules" ]]; then modules="$repo/frontend/node_modules"; fi
if [[ -n "$modules" ]]; then
  ln -s "$(cd -- "$modules" && pwd -P)" "$build/frontend/node_modules"
else
  (cd "$build/frontend" && npm ci --silent)
fi
(cd "$build/frontend" && npx vite build --logLevel warn)

echo "[2/4] backend (CGO_ENABLED=0 linux/amd64)"
mkdir -p "$app"
(cd "$repo/api" && CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build \
  -ldflags="-s -w -X main.buildDate=$(date +%Y-%m-%d_%H:%M)" -o "$app/Macroverse42" .)

echo "[3/4] assemble $app"
cp -a "$build/frontend/dist" "$app/frontend-build"
# Build metadata for the splash / VJ token cache (CI writes the same file for the cloud lanes).
sha=$(git -C "$repo" rev-parse HEAD)
dirty=$([[ -n "$(git -C "$repo" status --porcelain --untracked-files=no)" ]] && echo "+dirty" || true)
built=$(date -u +%Y-%m-%dT%H:%M:%SZ)
cat > "$app/frontend-build/deploy-meta.json" <<META
{
  "site": "kefka (local)",
  "branch": "$(git -C "$repo" rev-parse --abbrev-ref HEAD)",
  "track": "kefka",
  "version": "${sha:0:12}${dirty}",
  "build_date": "$built",
  "last_git_sha_short": "${sha:0:12}${dirty}",
  "last_git_sha": "$sha",
  "last_build_at": "$built",
  "last_deployed_at": "$built",
  "deploy_note": "Built on $(hostname) with ops/kefka/deploy-kefka.sh; served by macroverse.service on Kefka."
}
META
rsync -a "$repo/shaders/" "$app/shaders/"
cp "$repo/shader-preview-settings.default.json" "$repo/ascii.art" "$repo/thumbnails-baked.json" "$app/"
mkdir -p "$app/ops/kefka" "$app/docs"
rsync -a --exclude __pycache__ "$repo/ops/kefka/" "$app/ops/kefka/"
cp "$repo/docs/KEFKA.md" "$app/docs/KEFKA.md"
du -sh "$app"

echo "[4/4] deploy to $host:~/Apps/macroverse"
ssh "$host" 'mkdir -p ~/Apps/macroverse'
# The binary is replaced by rename (rsync's default temp file), so a running backend keeps
# its old inode until install-kefka.sh restarts it.
rsync -a --delete "$app/frontend-build/" "$host:Apps/macroverse/frontend-build/"
rsync -a --exclude frontend-build "$app/" "$host:Apps/macroverse/"
if (( install )); then
  ssh "$host" "bash ~/Apps/macroverse/ops/kefka/install-kefka.sh ${install_args[*]:-}"
fi
echo "deployed. build dir: $build"
