# Asset Governance by Track

## test

- Purpose: development validation.
- Asset scope: `shaders/starter-pack/` + `shaders/VJ-Sorted-Production/ISF/`.
- Forbidden: `resolume/`, `resolume-example/`, `private-data/`.

## live

- Purpose: public hosted release.
- Asset scope: same as test — full public ISF library in the image.
- Forbidden: Resolume Wire trees and aday-only private volume payloads.

## aday-private

- Purpose: personal full-asset runtime.
- Asset scope: full personal shader and Wire/Resolume assets.
- Access: mandatory nginx basic auth, optional Cloudflare Access overlay.

## CI controls

- Public jobs fail if blocked directories are detected.
- Public jobs fail if starter-pack exceeds cap (60 files).
- Aday-private jobs are isolated and do not publish public release bundles.
