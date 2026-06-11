# Asset Governance by Track

## test

- Purpose: development validation.
- Asset scope: starter-pack only.
- Forbidden: full `shaders/` bulk, `resolume/`, `resolume-example/`.

## live

- Purpose: public hosted release.
- Asset scope: starter-pack only.
- Forbidden: full private creative libraries.

## aday-private

- Purpose: personal full-asset runtime.
- Asset scope: full personal shader and Wire/Resolume assets.
- Access: mandatory nginx basic auth, optional Cloudflare Access overlay.

## CI controls

- Public jobs fail if blocked directories are detected.
- Public jobs fail if starter-pack exceeds cap (55 files).
- Aday-private jobs are isolated and do not publish public release bundles.
