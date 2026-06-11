# Sync Macroversed-FortyTwoEdition shaders to macroverse-private (password lane) on Linode.
# Library stays private: copy ops/aday-shaders.local.example.json -> ops/aday-shaders.local.json
param(
  [string]$ConfigPath = "",
  [switch]$DryRun,
  [switch]$SkipReindex
)

$ErrorActionPreference = "Stop"
if (-not $ConfigPath) {
  $ConfigPath = Join-Path $PSScriptRoot "aday-shaders.local.json"
}

if (-not (Test-Path $ConfigPath)) {
  Write-Host "Missing $ConfigPath"
  Write-Host "Copy ops/aday-shaders.local.example.json and set shadersSource to your FortyTwoEdition/shaders path."
  exit 1
}

$cfg = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$shadersSource = [string]$cfg.shadersSource
$fortyTwoRoot = [string]$cfg.fortyTwoRoot
$includeResolume = [bool]$cfg.includeResolume
$host_ = [string]$cfg.linodeHost
$user = [string]$cfg.linodeUser
$key = [string]$cfg.sshKeyPath
$remoteDir = [string]$cfg.remoteComposeDir
$triggerReindex = -not $SkipReindex -and ([bool]$cfg.triggerReindex)

if (-not (Test-Path $shadersSource)) {
  throw "shadersSource not found: $shadersSource"
}

$sshArgs = @("-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=accept-new")
if ($key -and (Test-Path $key)) {
  $sshArgs = @("-i", $key) + $sshArgs
}

function Invoke-Ssh([string]$Command) {
  & ssh @sshArgs "${user}@${host_}" $Command
  if ($LASTEXITCODE -ne 0) { throw "ssh failed: $Command" }
}

function Invoke-Scp([string[]]$ScpArgs) {
  & scp @sshArgs @ScpArgs
  if ($LASTEXITCODE -ne 0) { throw "scp failed" }
}

$fileCount = (Get-ChildItem $shadersSource -Recurse -File | Measure-Object).Count
Write-Host "Source: $shadersSource ($fileCount files)"
Write-Host "Target: ${user}@${host_}:$remoteDir/private-data/aday-shaders/"

$staging = Join-Path $env:TEMP ("macroverse-aday-shaders-" + [Guid]::NewGuid().ToString("n"))
$payload = Join-Path $staging "payload"
New-Item -ItemType Directory -Force -Path $payload | Out-Null

Write-Host "Staging shaders..."
Copy-Item -Path (Join-Path $shadersSource "*") -Destination $payload -Recurse -Force

if ($includeResolume -and $fortyTwoRoot -and (Test-Path (Join-Path $fortyTwoRoot "resolume"))) {
  Write-Host "Including resolume/ (Wire library)..."
  Copy-Item -Path (Join-Path $fortyTwoRoot "resolume") -Destination (Join-Path $payload "resolume") -Recurse -Force
}

$archive = Join-Path $staging "aday-shaders.tgz"
Write-Host "Creating archive..."
& tar -czf $archive -C $payload .
if ($LASTEXITCODE -ne 0) { throw "tar create failed" }

$remoteArchive = "/tmp/aday-shaders.tgz"
if ($DryRun) {
  Write-Host "Dry run: would upload $archive and extract on server."
  Remove-Item -Recurse -Force $staging
  exit 0
}

Write-Host "Uploading to Linode..."
Invoke-Scp @($archive, "${user}@${host_}:${remoteArchive}")

$remoteCmd = "cd '$remoteDir' && git pull --ff-only origin main && mkdir -p private-data/aday-shaders && find private-data/aday-shaders -mindepth 1 -maxdepth 1 -exec rm -rf {} + && tar -xzf /tmp/aday-shaders.tgz -C private-data/aday-shaders && rm -f /tmp/aday-shaders.tgz && docker compose up -d macroverse_aday && docker compose exec -T nginx nginx -t && docker restart edge_nginx"

Write-Host "Extracting on server and restarting macroverse_aday..."
Invoke-Ssh $remoteCmd

if ($triggerReindex) {
  Write-Host "Triggering shader reindex on private lane (may take several minutes)..."
  $pass = $env:ADAY_AUTH_PASSWORD
  if (-not $pass) {
    Write-Host "Skip reindex: set ADAY_AUTH_PASSWORD env var (not stored in git)."
  } else {
    try {
      $cred = "aday:$pass"
      $pair = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($cred))
      $headers = @{ Authorization = "Basic $pair" }
      Invoke-RestMethod -Uri "https://macroverse-private.aday.net.au/api/native-scan" -Method Post -Headers $headers -TimeoutSec 600 | Out-Null
      Write-Host "Reindex request accepted."
    } catch {
      Write-Host "Reindex call failed (library is still on disk): $($_.Exception.Message)"
      Write-Host "Open https://macroverse-private.aday.net.au and use Settings -> scan, or POST /api/native-scan"
    }
  }
}

Remove-Item -Recurse -Force $staging
Write-Host "Done. Open https://macroverse-private.aday.net.au (basic auth — credentials in password manager)"
