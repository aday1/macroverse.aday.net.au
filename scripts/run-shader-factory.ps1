# Orchestrate VJ shader generation, QA, thumbnail bake.
# Usage: .\scripts\run-shader-factory.ps1 [-VariantsPerTemplate 12] [-MaxRounds 8]
param(
  [int]$VariantsPerTemplate = 12,
  [int]$MaxRounds = 8,
  [double]$MinPassRate = 0.4,
  [int]$FailStreakLimit = 3
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$failStreak = 0
$round = 0
$start = Get-Date

while ($round -lt $MaxRounds) {
  $round++
  $elapsed = (Get-Date) - $start
  if ($elapsed.TotalHours -ge 8) {
    Write-Host "Stop: 8h budget" -ForegroundColor Yellow
    break
  }

  Write-Host "`n=== Factory round $round ===" -ForegroundColor Cyan
  node scripts/generate-vj-shaders.mjs --batch=$VariantsPerTemplate
  if ($round -gt 1) {
    node scripts/generate-vj-shaders.mjs --batch=4 --mutate
  }

  $reportPath = Join-Path $root "scripts/reports/shader-qa-round-$round.json"
  node scripts/validate-shader-batch.mjs shaders/VJ-Generated --move-failures --json=$reportPath
  $qaExit = $LASTEXITCODE

  if (Test-Path $reportPath) {
    $rep = Get-Content $reportPath -Raw | ConvertFrom-Json
    $rate = if ($rep.total -gt 0) { $rep.passed.Count / $rep.total } else { 0 }
    Write-Host "Pass rate: $([math]::Round($rate * 100, 1))%" -ForegroundColor $(if ($rate -ge $MinPassRate) { 'Green' } else { 'Yellow' })
    if ($rate -lt $MinPassRate) {
      $failStreak++
      if ($failStreak -ge $FailStreakLimit) {
        Write-Host "Stop: pass rate below $MinPassRate for $FailStreakLimit rounds" -ForegroundColor Yellow
        break
      }
    } else {
      $failStreak = 0
    }
  }

  if ($qaExit -ne 0 -and $round -eq 1) {
    Write-Host "Some failures quarantined; continuing." -ForegroundColor DarkYellow
  }
}

Write-Host "`nBaking thumbnails for VJ-Generated..." -ForegroundColor Cyan
node scripts/bake-thumbnails-offline.mjs --dir=shaders/VJ-Generated --merge --concurrency=3

$count = (Get-ChildItem -Recurse -File (Join-Path $root "shaders/VJ-Generated") -Filter *.fs -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Host "VJ-Generated shader files on disk: $count" -ForegroundColor Green