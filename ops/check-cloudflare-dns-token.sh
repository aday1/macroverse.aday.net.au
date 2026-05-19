#!/usr/bin/env bash
# Quick probe: can this token list DNS records on aday.net.au?
set -euo pipefail
ZONE="${CLOUDFLARE_ZONE_NAME:-aday.net.au}"
if [ -z "${CLOUDFLARE_API_TOKEN:-}" ]; then
  echo "Set CLOUDFLARE_API_TOKEN" >&2
  exit 1
fi
enc="$(python3 -c "import urllib.parse; print(urllib.parse.quote('$ZONE', safe=''))")"
code="$(curl -sS -o /tmp/cf-dns-probe.json -w "%{http_code}" \
  -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
  "https://api.cloudflare.com/client/v4/zones?name=${enc}")"
echo "zones?name= HTTP ${code}"
if [ "$code" != "200" ]; then
  cat /tmp/cf-dns-probe.json >&2
  exit 1
fi
zid="$(python3 -c "import json; d=json.load(open('/tmp/cf-dns-probe.json')); r=d.get('result') or []; print(r[0]['id'] if r else '')")"
if [ -z "$zid" ]; then
  echo "zone not found" >&2
  exit 1
fi
code2="$(curl -sS -o /tmp/cf-dns-list.json -w "%{http_code}" \
  -H "Authorization: Bearer ${CLOUDFLARE_API_TOKEN}" \
  "https://api.cloudflare.com/client/v4/zones/${zid}/dns_records?per_page=1")"
echo "dns_records list HTTP ${code2}"
if [ "$code2" != "200" ]; then
  echo "Token lacks Zone.DNS Edit (Pages deploy tokens return 403)." >&2
  cat /tmp/cf-dns-list.json >&2
  exit 1
fi
echo "OK: token can manage DNS on ${ZONE}"
