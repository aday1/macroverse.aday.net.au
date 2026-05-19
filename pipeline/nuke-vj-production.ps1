# NUKE: Hard reset a VJ-Sorted-Production (or given) path with backup.
# Reads shader-preview-settings.json sourcePaths to find a path containing "VJ-Sorted-Production",
# or uses -Path if provided. Creates a timestamped backup branch then git reset --hard.
param([string]$Path = "")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$settingsPath = Join-Path $root "shader-preview-settings.json"

$targetPath = $Path
if (-not $targetPath -and (Test-Path $settingsPath)) {
    $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $srcPaths = $settings.sourcePaths
    if ($srcPaths) {
        foreach ($p in $srcPaths) {
            $full = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $root $p }
            if ($full -match "VJ-Sorted-Production") {
                $targetPath = $full
                break
            }
        }
    }
}
if (-not $targetPath) {
    $candidates = @(
        (Join-Path $root "shaders\VJ-Sorted-Production"),
        (Join-Path $root "VJ-Sorted-Production")
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) {
            $targetPath = $c
            break
        }
    }
}
if (-not $targetPath -or -not (Test-Path $targetPath)) {
    Write-Host "NUKE: No VJ-Sorted-Production path found. Set -Path or add to sourcePaths in settings." -ForegroundColor Red
    exit 1
}

Push-Location $targetPath
try {
    if (-not (Test-Path ".git")) {
        Write-Host "NUKE: $targetPath is not a git repo. Skipping." -ForegroundColor Yellow
        Write-Host "      To use NUKE here: run 'git init' in that folder, or clone your shaders from a git remote." -ForegroundColor Gray
        exit 0
    }
    $rev = git rev-parse --short HEAD 2>$null
    $backupBranch = "nuke-backup-" + (Get-Date -Format "yyyyMMdd-HHmmss")
    Write-Host "NUKE: Backup branch $backupBranch at $rev ..." -ForegroundColor Cyan
    git branch $backupBranch 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "NUKE: Failed to create backup branch." -ForegroundColor Red
        exit 1
    }
    Write-Host "NUKE: git fetch then reset --hard ..." -ForegroundColor Cyan
    git fetch 2>$null
    $reset = $false
    foreach ($remote in @("origin/main", "origin/master")) {
        $cnt = git rev-list "HEAD..$remote" --count 2>$null
        if ($cnt -match '^\d+$') {
            git reset --hard $remote
            Write-Host "NUKE: Reset to $remote" -ForegroundColor Green
            $reset = $true
            break
        }
    }
    if (-not $reset) {
        git reset --hard HEAD
        Write-Host "NUKE: Reset to local HEAD (no remote found)" -ForegroundColor Yellow
    }
    Write-Host "NUKE: Done. To restore: git checkout $backupBranch" -ForegroundColor Green
} finally {
    Pop-Location
}
