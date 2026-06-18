# Sync VJ-Sorted-Production ISF library from Macroversed-FortyTwoEdition into this repo.
# Run from repo root: .\scripts\sync-public-shaders.ps1
param(
  [string]$SourceRoot = "C:\aday.repo\Macroversed-FortyTwoEdition\shaders\VJ-Sorted-Production",
  [string]$DestRoot = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $DestRoot) {
  $DestRoot = Join-Path $repoRoot "shaders\VJ-Sorted-Production"
}

if (-not (Test-Path $SourceRoot)) {
  Write-Error "Source not found: $SourceRoot"
}

New-Item -ItemType Directory -Force -Path $DestRoot | Out-Null
& robocopy $SourceRoot $DestRoot /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit $LASTEXITCODE" }

$count = (Get-ChildItem -Recurse -File $DestRoot -Include *.fs,*.frag,*.glsl,*.isf | Measure-Object).Count

$fortyTwoRoot = "C:\aday.repo\Macroversed-FortyTwoEdition"
$genSrc = Join-Path $repoRoot "shaders\VJ-Generated"
if (Test-Path $genSrc) {
  $genDest = Join-Path $fortyTwoRoot "shaders\VJ-Generated"
  New-Item -ItemType Directory -Force -Path $genDest | Out-Null
  & robocopy $genSrc $genDest /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "robocopy VJ-Generated failed with exit $LASTEXITCODE" }
  Write-Host "Synced VJ-Generated -> $genDest" -ForegroundColor Green
}

Write-Host "Synced $count shader files -> $DestRoot" -ForegroundColor Green
Write-Host "Next: bash scripts/verify-public-export.sh" -ForegroundColor Cyan