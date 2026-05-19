param(
  [ValidateSet("soft","rollback","nuke-service")]
  [string]$Mode = "soft"
)

if ($Mode -eq "soft") {
  docker compose pull
  docker compose up -d
  exit $LASTEXITCODE
}

if ($Mode -eq "rollback") {
  Write-Host "Set image tags in docker-compose.yml before rollback."
  docker compose pull
  docker compose up -d
  exit $LASTEXITCODE
}

if ($Mode -eq "nuke-service") {
  docker compose down
  docker image prune -f
  docker compose up -d
  exit $LASTEXITCODE
}
