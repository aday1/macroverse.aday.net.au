# Macroverse Distribution Policy

This repository publishes the Macroverse toolchain for hosted use.

## Track Policy

- `test` and `live` are public-slim tracks.
- `aday-private` is personal and may use full assets.

## Public Track Allowlist

Allowed in public artifacts:

- Macroverse application code (`api/`, `frontend/`, runtime files).
- `shaders/starter-pack/` only.
- Deployment/runtime configs (`docker-compose.yml`, `nginx/`, `ops/`, workflows).

Blocked in public artifacts:

- `resolume/`
- `resolume-example/`
- `shaders/` except `shaders/starter-pack/`

## Release Gates

Public image/release jobs must fail when:

- blocked directories are present in build context or resulting image;
- shader file count exceeds starter-pack cap;
- absolute personal paths are detected in public artifacts.

## Starter Pack Cap

- Target cap: 60 shader files maximum.
- Current baseline: 57 files in `shaders/starter-pack/` (10 legacy root + 41 curated by type + 6 macroverse-set).

## Ownership Boundary

Creative libraries remain under personal control in `aday-private`.
Public tracks ship tools, not full personal asset collections.
