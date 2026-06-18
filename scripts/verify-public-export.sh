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

if [ ! -d shaders/VJ-Sorted-Production/ISF ]; then
  echo "Missing shaders/VJ-Sorted-Production/ISF (public cloud library)."
  exit 1
fi

count=$(find shaders -type f \( -name '*.fs' -o -name '*.frag' -o -name '*.glsl' -o -name '*.isf' \) | wc -l | tr -d ' ')
min_public=500
if [ "$count" -lt "$min_public" ]; then
  echo "Public shader library has $count files; expected at least $min_public."
  exit 1
fi

echo "Public export gate passed ($count shader files)."