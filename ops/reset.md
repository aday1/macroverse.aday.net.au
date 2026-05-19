# Reset and Redeploy Runbook

All commands run on the Linode host from the repository root.

## 1) Soft reset (no data loss)

`docker compose pull && docker compose up -d`

## 2) Roll back to known image tags

1. Edit `docker-compose.yml` image tags to known good versions.
2. Run `docker compose pull && docker compose up -d`.

## 3) Service nuke and recreate

`docker compose down`

Then remove specific images/containers and bring stack back:

`docker image prune -f && docker compose up -d`

## 4) Full nuke with restore

1. Backup named volumes (`macroverse_data`, `macroverse_shaders`, `artbastard_data`).
2. Recreate Linode.
3. Re-bootstrap Docker and clone this repo.
4. Restore volumes.
5. `docker compose up -d`.

## 5) Full nuke without restore

1. Recreate Linode.
2. Re-bootstrap Docker and clone this repo.
3. `docker compose up -d`.
