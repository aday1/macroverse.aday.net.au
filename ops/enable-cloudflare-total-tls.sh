#!/usr/bin/env bash
# Optional: issue edge certs for nested hostnames (dev.macroverse.aday.net.au).
# Token needs SSL and Certificates Write on zone aday.net.au (Wrangler OAuth is not enough).
set -euo pipefail
ZONE_NAME="${ZONE_NAME:-aday.net.au}"
TOKEN="${CLOUDFLARE_DNS_API_TOKEN:-${CLOUDFLARE_API_TOKEN:-}}"
if [[ -z "$TOKEN" ]]; then
  echo "Set CLOUDFLARE_DNS_API_TOKEN with SSL and Certificates Write on $ZONE_NAME"
  exit 1
fi
zone_id="$(curl -fsS -H "Authorization: Bearer $TOKEN" \
  "https://api.cloudflare.com/client/v4/zones?name=${ZONE_NAME}" | jq -r '.result[0].id')"
curl -fsS -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  "https://api.cloudflare.com/client/v4/zones/${zone_id}/acm/total_tls" \
  -d '{"enabled":true,"certificate_authority":"lets_encrypt"}' | jq .
echo "Wait 15-60m, then test: curl -sS -o /dev/null -w '%{http_code}\n' https://dev.macroverse.aday.net.au/"
