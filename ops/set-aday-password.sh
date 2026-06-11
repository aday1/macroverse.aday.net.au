#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "usage: $0 <username>"
  exit 1
fi

USERNAME="$1"
mkdir -p nginx/auth
htpasswd -c nginx/auth/aday.htpasswd "$USERNAME"
echo "Created nginx/auth/aday.htpasswd for $USERNAME"
