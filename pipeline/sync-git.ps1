# Best-effort fast-forward sync used by desktop launch shortcuts.
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

function Write-SyncStatus {
    param([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::DarkGray)
    if (-not $Quiet) { Write-Host $Message -ForegroundColor $Color }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-SyncStatus "Git is not available; skipped repo sync." Yellow
    exit 0
}

if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot ".git"))) {
    Write-SyncStatus "Not a git checkout; skipped repo sync." Yellow
    exit 0
}

try {
    $branch = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null).Trim()
    if ([string]::IsNullOrWhiteSpace($branch) -or $branch -eq "HEAD") {
        Write-SyncStatus "Detached checkout; skipped automatic repo sync." Yellow
        exit 0
    }

    $upstream = (& git -C $RepoRoot rev-parse --abbrev-ref --symbolic-full-name "@{u}" 2>$null).Trim()
    if ([string]::IsNullOrWhiteSpace($upstream)) {
        Write-SyncStatus "No upstream branch configured; skipped automatic repo sync." Yellow
        exit 0
    }

    Write-SyncStatus "Checking Macroverse repo for fast-forward updates..."
    & git -C $RepoRoot fetch --prune 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git fetch failed" }

    & git -C $RepoRoot pull --ff-only 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git pull --ff-only failed" }

    Write-SyncStatus "Macroverse repo is current." Green
} catch {
    Write-SyncStatus "Repo sync skipped: $($_.Exception.Message)" Yellow
}
