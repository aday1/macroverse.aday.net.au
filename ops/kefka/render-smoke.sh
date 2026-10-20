#!/usr/bin/env bash
# Render Macroverse in a throwaway headless Chrome on Kefka and fetch screenshots + a JSON report.
#
#   ops/kefka/render-smoke.sh OUT_DIR [HOST]        (HOST defaults to kefka; needs Node >= 22 here)
#
# Kefka side: flatpak Chrome --headless=new, niced, with its own profile in ~/.cache/macroverse-render-smoke
# (exposed to the sandbox only for this run), DevTools on Kefka's 127.0.0.1:9339 reached through an SSH
# tunnel. No window is shown. The browser instance, the tunnel and the profile are removed on exit.
set -euo pipefail
here=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
out=${1:?usage: render-smoke.sh OUT_DIR [HOST]}
host=${2:-kefka}
remote_port=9339
local_port=${MACROVERSE_SMOKE_LOCAL_PORT:-19339}
mkdir -p "$out"
tunnel_pid=""
cleanup() {
  if [[ -n "$tunnel_pid" ]]; then kill "$tunnel_pid" 2>/dev/null || true; fi
  ssh "$host" bash -s <<'REMOTE' || true
d=$HOME/.cache/macroverse-render-smoke
if [ -s "$d/instance" ]; then
  i=$(cat "$d/instance")
  case "$i" in ""|*[!0-9]*) ;; *) flatpak kill "$i" 2>/dev/null || true ;; esac
fi
systemctl --user stop macroverse-render-smoke.service 2>/dev/null || true
sleep 1
rm -rf "$d"
echo "kefka: headless Chrome stopped, profile removed"
REMOTE
}
trap cleanup EXIT
ssh "$host" REMOTE_PORT="$remote_port" bash -s <<'REMOTE'
set -eu
d=$HOME/.cache/macroverse-render-smoke
rm -rf "$d"; mkdir -p "$d"
systemd-run --user --quiet --collect --unit=macroverse-render-smoke --nice=10 /bin/sh -c "exec /usr/bin/flatpak run \
  --die-with-parent --instance-id-fd=3 --filesystem=$d com.google.Chrome --headless=new --user-data-dir=$d/profile \
  --remote-debugging-address=127.0.0.1 --remote-debugging-port=$REMOTE_PORT --window-size=1280,800 --no-first-run \
  --no-default-browser-check --disable-features=TranslateUI --enable-gpu --ignore-gpu-blocklist \
  --enable-unsafe-swiftshader about:blank 3>$d/instance"
for _ in $(seq 1 80); do
  curl -fsS -o /dev/null "http://127.0.0.1:$REMOTE_PORT/json/version" && exit 0
  sleep 0.25
done
echo "headless Chrome did not start" >&2
journalctl --user -u macroverse-render-smoke -n 20 --no-pager >&2
exit 1
REMOTE
ssh -N -o ExitOnForwardFailure=yes -L "127.0.0.1:$local_port:127.0.0.1:$remote_port" "$host" &
tunnel_pid=$!
for _ in $(seq 1 40); do
  curl -fsS -o /dev/null "http://127.0.0.1:$local_port/json/version" 2>/dev/null && break
  sleep 0.25
done
curl -fsS "http://127.0.0.1:$local_port/json/version" | head -c 300; echo
node "$here/render-smoke.mjs" --cdp "http://127.0.0.1:$local_port" \
  --url "${MACROVERSE_SMOKE_URL:-http://127.0.0.1:8765/?quickstart=0}" --out "$out"
