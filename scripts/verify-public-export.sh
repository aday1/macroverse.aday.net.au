#!/usr/bin/env bash
set -euo pipefail

if [ -d resolume ] || [ -d resolume-example ]; then
  echo "Forbidden directory present for public export."
  exit 1
fi

if [ ! -d shaders/starter-pack ]; then
  echo "Missing shaders/starter-pack."
  exit 1
fi

count=$(find shaders/starter-pack -type f | wc -l | tr -d ' ')
if [ "$count" -gt 25 ]; then
  echo "starter-pack contains $count files, max is 25."
  exit 1
fi

echo "Public export gate passed."
