#!/usr/bin/env bash
# Upsert proxied A records for Macroverse/ArtBastard Linode stack.
# Upsert CNAME for GitHub Pages showcase alias (showcase.macroverse.aday.net.au).
# Requires: CLOUDFLARE_API_TOKEN (Zone.DNS Edit on aday.net.au)
# Optional: CLOUDFLARE_ZONE_ID (else resolved from CLOUDFLARE_ZONE_NAME)
set -uo pipefail

ORIGIN_IP="${ORIGIN_IP:-172.105.171.251}"
SHOWCASE_CNAME_TARGET="${SHOWCASE_CNAME_TARGET:-aday1.github.io}"
CLOUDFLARE_ZONE_NAME="${CLOUDFLARE_ZONE_NAME:-aday.net.au}"
SYNC_MODE="${SYNC_MODE:-all}"

if [ -z "${CLOUDFLARE_API_TOKEN:-}" ]; then
  echo "Set CLOUDFLARE_API_TOKEN" >&2
  exit 1
fi

api_json() {
  local method="$1"
  local url="$2"
  local data="${3:-}"
  local bodyf codef
  bodyf="$(mktemp)"
  codef="$(mktemp)"
  if [ -n "$data" ]; then
    curl -sS -o "$bodyf" -w "%{http_code}" -X "$method" \
      -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
      -H "Content-Type: application/json" \
      -d "$data" "$url" >"$codef"
  else
    curl -sS -o "$bodyf" -w "%{http_code}" -X "$method" \
      -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
      -H "Content-Type: application/json" \
      "$url" >"$codef"
  fi
  local code
  code="$(cat "$codef")"
  rm -f "$codef"
  printf '%s\n' "$code"
  cat "$bodyf"
  rm -f "$bodyf"
}

if [ -z "${CLOUDFLARE_ZONE_ID:-}" ]; then
  echo "Resolving zone id for ${CLOUDFLARE_ZONE_NAME}..."
  resp="$(api_json GET "https://api.cloudflare.com/client/v4/zones?name=${CLOUDFLARE_ZONE_NAME}")"
  code="${resp%%$'\n'*}"
  body="${resp#*$'\n'}"
  if [ "$code" != "200" ]; then
    echo "Zone lookup failed (HTTP ${code}): ${body}" >&2
    exit 1
  fi
  CLOUDFLARE_ZONE_ID="$(echo "$body" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('result') or []; print(r[0]['id'] if r else '')")"
  if [ -z "${CLOUDFLARE_ZONE_ID}" ]; then
    echo "Could not resolve Cloudflare zone id for ${CLOUDFLARE_ZONE_NAME}" >&2
    exit 1
  fi
  echo "Zone id: ${CLOUDFLARE_ZONE_ID}"
fi

if [ "$SYNC_MODE" = "dev-only" ]; then
  REQUIRED_NAMES=(
    macroverse-dev.aday.net.au
    artbastard-dev.aday.net.au
  )
  OPTIONAL_NAMES=()
else
  REQUIRED_NAMES=(
    macroverse-dev.aday.net.au
    artbastard-dev.aday.net.au
  )
  OPTIONAL_NAMES=(
    macroverse.aday.net.au
    macroverse-test.aday.net.au
    test.macroverse.aday.net.au
    macroverse-private.aday.net.au
    macroverse-aday.aday.net.au
    artbastard.aday.net.au
    artbastard-test.aday.net.au
    test.artbastard.aday.net.au
  )
fi

failures=0

upsert_a() {
  local name="$1"
  local required="$2"
  local enc
  enc="$(python3 -c "import sys,urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=''))" "$name")"
  local list_resp list_code list_body
  list_resp="$(api_json GET "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records?type=A&name=${enc}")"
  list_code="${list_resp%%$'\n'*}"
  list_body="${list_resp#*$'\n'}"
  if [ "$list_code" != "200" ]; then
    echo "list failed for ${name} (HTTP ${list_code})" >&2
    [ "$required" = "1" ] && failures=$((failures + 1))
    return 1
  fi
  local id
  id="$(echo "$list_body" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('result') or []; print(r[0]['id'] if r else '')" 2>/dev/null || true)"
  local body
  body="$(printf '{"type":"A","name":"%s","content":"%s","ttl":1,"proxied":true}' "$name" "$ORIGIN_IP")"
  local action method url
  if [ -n "$id" ]; then
    action="updated"
    method="PUT"
    url="https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records/${id}"
  else
    action="created"
    method="POST"
    url="https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records"
  fi
  local write_resp write_code write_body
  write_resp="$(api_json "$method" "$url" "$body")"
  write_code="${write_resp%%$'\n'*}"
  write_body="${write_resp#*$'\n'}"
  if [ "$write_code" = "200" ]; then
    echo "${action} A ${name} -> ${ORIGIN_IP} (proxied)"
    return 0
  fi
  echo "FAILED ${name} (HTTP ${write_code}): $(echo "$write_body" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('errors') or d)" 2>/dev/null || echo "$write_body")" >&2
  [ "$required" = "1" ] && failures=$((failures + 1))
  return 1
}

delete_dns_by_name() {
  local name="$1"
  local type="$2"
  local enc
  enc="$(python3 -c "import sys,urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=''))" "$name")"
  local list_resp list_code list_body
  list_resp="$(api_json GET "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records?type=${type}&name=${enc}")"
  list_code="${list_resp%%$'\n'*}"
  list_body="${list_resp#*$'\n'}"
  if [ "$list_code" != "200" ]; then
    return 1
  fi
  local ids
  ids="$(echo "$list_body" | python3 -c "import sys,json; d=json.load(sys.stdin); print(' '.join(r['id'] for r in (d.get('result') or [])))" 2>/dev/null || true)"
  for id in $ids; do
    api_json DELETE "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records/${id}" >/dev/null || true
  done
}

upsert_cname() {
  local name="$1"
  local target="$2"
  local required="$3"
  delete_dns_by_name "$name" "A"
  local enc
  enc="$(python3 -c "import sys,urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=''))" "$name")"
  local list_resp list_code list_body
  list_resp="$(api_json GET "https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records?type=CNAME&name=${enc}")"
  list_code="${list_resp%%$'\n'*}"
  list_body="${list_resp#*$'\n'}"
  if [ "$list_code" != "200" ]; then
    echo "list failed for ${name} (HTTP ${list_code})" >&2
    [ "$required" = "1" ] && failures=$((failures + 1))
    return 1
  fi
  local id
  id="$(echo "$list_body" | python3 -c "import sys,json; d=json.load(sys.stdin); r=d.get('result') or []; print(r[0]['id'] if r else '')" 2>/dev/null || true)"
  local body
  body="$(printf '{"type":"CNAME","name":"%s","content":"%s","ttl":1,"proxied":true}' "$name" "$target")"
  local action method url
  if [ -n "$id" ]; then
    action="updated"
    method="PUT"
    url="https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records/${id}"
  else
    action="created"
    method="POST"
    url="https://api.cloudflare.com/client/v4/zones/${CLOUDFLARE_ZONE_ID}/dns_records"
  fi
  local write_resp write_code write_body
  write_resp="$(api_json "$method" "$url" "$body")"
  write_code="${write_resp%%$'\n'*}"
  write_body="${write_resp#*$'\n'}"
  if [ "$write_code" = "200" ]; then
    echo "${action} CNAME ${name} -> ${target} (proxied)"
    return 0
  fi
  echo "FAILED ${name} (HTTP ${write_code}): $(echo "$write_body" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('errors') or d)" 2>/dev/null || echo "$write_body")" >&2
  [ "$required" = "1" ] && failures=$((failures + 1))
  return 1
}

SHOWCASE_CNAME_NAMES=(
  showcase.macroverse.aday.net.au
)

for n in "${REQUIRED_NAMES[@]}"; do
  upsert_a "$n" 1 || true
done

for n in "${OPTIONAL_NAMES[@]}"; do
  upsert_a "$n" 0 || true
done

for n in "${SHOWCASE_CNAME_NAMES[@]}"; do
  upsert_cname "$n" "$SHOWCASE_CNAME_TARGET" 0 || true
done

if [ "$failures" -gt 0 ]; then
  echo "${failures} required DNS record(s) failed. Token needs Zone.DNS Edit on ${CLOUDFLARE_ZONE_NAME}." >&2
  exit 1
fi

echo "Done."
