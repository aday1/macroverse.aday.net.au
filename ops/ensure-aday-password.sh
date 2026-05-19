#!/usr/bin/env bash
# Ensure nginx/auth/aday.htpasswd exists for macroverse-aday.aday.net.au (user aday).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
HTPASSWD="${ROOT}/nginx/auth/aday.htpasswd"
USER_NAME="${ADAY_AUTH_USER:-aday}"
PASS="${ADAY_AUTH_PASSWORD:-macroverse}"

mkdir -p "${ROOT}/nginx/auth"
if command -v htpasswd >/dev/null 2>&1; then
  htpasswd -cb "${HTPASSWD}" "${USER_NAME}" "${PASS}"
else
  HASH="$(openssl passwd -apr1 "${PASS}")"
  printf '%s:%s\n' "${USER_NAME}" "${HASH}" > "${HTPASSWD}"
fi
echo "Wrote ${HTPASSWD} for user ${USER_NAME}"
