# Macroverse Distribution Policy

This repository publishes the Macroverse toolchain for hosted use.

## Track Policy

- `test` and `live` are public-slim tracks.
- `aday-private` is personal and may use full assets.

## Public Track Allowlist

Allowed in public artifacts:

- Macroverse application code (`api/`, `frontend/`, runtime files).
- `shaders/starter-pack/` — curated demos and MacroVerse Origin set.
- `shaders/VJ-Sorted-Production/ISF/` — full public ISF library (~2400 shaders).
- Deployment/runtime configs (`docker-compose.yml`, `nginx/`, `ops/`, workflows).

Blocked in public artifacts:

- `resolume/`
- `resolume-example/`
- `private-data/` (aday lane volume sync only)

## Release Gates

Public image/release jobs must fail when:

- blocked directories are present in build context or resulting image;
- public shader library is below minimum (see `scripts/verify-public-export.sh`);
- absolute personal paths are detected in public artifacts.

## Public Library Baseline

- Minimum: 500 indexed shader files under `shaders/` (CI gate).
- Current baseline: starter-pack (~53) + VJ-Sorted-Production ISF tree (~2400), including `macroverse/` chapter shaders.
- Sync source: `Macroversed-FortyTwoEdition` via `scripts/sync-public-shaders.ps1`.

## Ownership Boundary

Creative libraries remain under personal control in `aday-private`.
Public tracks ship tools, not full personal asset collections.
