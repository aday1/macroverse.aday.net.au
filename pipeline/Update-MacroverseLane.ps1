param(
    [ValidateSet("menu", "live", "dev", "aday")]
    [string]$Lane = "menu",
    [switch]$SkipBuild,
    [switch]$DeployAday
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildPs1 = Join-Path $PSScriptRoot "build.ps1"
$createShortcut = Join-Path $PSScriptRoot "create-shortcut.ps1"
$adayConfigPath = Join-Path $repoRoot "ops\aday-shaders.local.json"
$adayDeployPs1 = Join-Path $repoRoot "ops\deploy-aday-shaders.ps1"

function Write-Step {
    param([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::Cyan)
    Write-Host ""
    Write-Host "== $Message" -ForegroundColor $Color
}

function Invoke-Git {
    param(
        [string]$RepoPath,
        [string[]]$Arguments
    )
    & git -C $RepoPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed in $RepoPath with exit code $LASTEXITCODE"
    }
}

function Save-DirtyWorktree {
    param([string]$RepoPath)
    $dirty = & git -C $RepoPath status --porcelain
    if (-not $dirty) { return }

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    Write-Host "Local changes detected; stashing before lane switch." -ForegroundColor Yellow
    Invoke-Git $RepoPath @("stash", "push", "-u", "-m", "Macroverse launcher autosave $stamp")
}

function Sync-Branch {
    param(
        [string]$RepoPath,
        [string]$Branch
    )

    if (-not (Test-Path (Join-Path $RepoPath ".git"))) {
        throw "Not a git checkout: $RepoPath"
    }

    Write-Step "Fetching $Branch"
    Invoke-Git $RepoPath @("fetch", "--all", "--prune")

    $current = (& git -C $RepoPath rev-parse --abbrev-ref HEAD 2>$null)
    if ($current) { $current = [string]$current.Trim() }

    if ($current -ne $Branch) {
        Save-DirtyWorktree $RepoPath
        Write-Step "Switching to $Branch"
        $localBranch = & git -C $RepoPath branch --list $Branch
        if ($localBranch) {
            Invoke-Git $RepoPath @("switch", $Branch)
        } else {
            $remoteRef = & git -C $RepoPath rev-parse --verify --quiet "origin/$Branch"
            if ($LASTEXITCODE -eq 0 -and $remoteRef) {
                Invoke-Git $RepoPath @("switch", "-c", $Branch, "origin/$Branch")
            } else {
                throw "Remote branch origin/$Branch does not exist."
            }
        }
    } else {
        $dirty = & git -C $RepoPath status --porcelain
        if ($dirty) {
            Write-Host "Local changes are present; staying on $Branch and leaving them in place." -ForegroundColor Yellow
        }
    }

    Write-Step "Pulling $Branch"
    Invoke-Git $RepoPath @("pull", "--ff-only", "origin", $Branch)
    $short = (& git -C $RepoPath rev-parse --short HEAD).Trim()
    Write-Host "Macroverse repo is now $Branch @$short" -ForegroundColor Green
}

function Get-AdayConfig {
    if (-not (Test-Path $adayConfigPath)) {
        Write-Host "No ops\aday-shaders.local.json found; private shader sync is skipped." -ForegroundColor Yellow
        return $null
    }
    return Get-Content -LiteralPath $adayConfigPath -Raw | ConvertFrom-Json
}

function Sync-AdayPrivateSource {
    $cfg = Get-AdayConfig
    if (-not $cfg) { return }

    $privateRoot = [string]$cfg.fortyTwoRoot
    $shaderRoot = [string]$cfg.shadersSource

    Write-Step "Checking Aday private shader source"
    if ($shaderRoot -and (Test-Path $shaderRoot)) {
        Write-Host "Private shader source is present and remains local-only." -ForegroundColor Green
    } else {
        Write-Host "Private shader source path is missing; check ops\aday-shaders.local.json." -ForegroundColor Yellow
    }

    if (-not $privateRoot -or -not (Test-Path $privateRoot)) {
        Write-Host "Private FortyTwo root is not present; skipped private git update." -ForegroundColor Yellow
        return
    }

    if (-not (Test-Path (Join-Path $privateRoot ".git"))) {
        Write-Host "Private FortyTwo root is present but is not a git checkout; skipped private git pull." -ForegroundColor Yellow
        return
    }

    Write-Step "Updating private FortyTwo checkout"
    Invoke-Git $privateRoot @("fetch", "--all", "--prune")
    Invoke-Git $privateRoot @("pull", "--ff-only")
    $short = (& git -C $privateRoot rev-parse --short HEAD).Trim()
    Write-Host "Private source updated @$short" -ForegroundColor Green
}

function Invoke-BuildAndShortcut {
    if ($SkipBuild) {
        Write-Host "Skipping build by request." -ForegroundColor Yellow
    } else {
        Write-Step "Building Macroverse"
        & $buildPs1
        if (-not $?) { throw "build.ps1 failed." }
    }

    if (Test-Path $createShortcut) {
        Write-Step "Refreshing Desktop shortcuts"
        & $createShortcut
    }
}

function Invoke-AdayDeploy {
    if (-not $DeployAday) { return }
    if (-not (Test-Path $adayDeployPs1)) {
        throw "Missing $adayDeployPs1"
    }

    Write-Host ""
    Write-Host "This will upload the private shader library to the passworded Aday lane." -ForegroundColor Yellow
    $confirm = Read-Host "Type DEPLOY to continue"
    if ($confirm -ne "DEPLOY") {
        Write-Host "Aday deploy cancelled." -ForegroundColor Yellow
        return
    }

    Write-Step "Deploying Aday private shaders"
    & $adayDeployPs1
    if (-not $?) { throw "deploy-aday-shaders.ps1 failed." }
}

function Select-Lane {
    if ($Lane -ne "menu") { return $Lane }

    Write-Host ""
    Write-Host "Macroverse lane update" -ForegroundColor Cyan
    Write-Host "  1. Live/public      main -> macroverse.aday.net.au"
    Write-Host "  2. Dev/test         dev  -> macroverse-test/dev"
    Write-Host "  3. Aday private     main + local private shader source"
    Write-Host ""

    do {
        $choice = Read-Host "Choose 1, 2, or 3"
    } until ($choice -in @("1", "2", "3", "live", "dev", "aday"))

    if ($choice -in @("2", "dev")) { return "dev" }
    if ($choice -in @("3", "aday")) { return "aday" }
    return "live"
}

try {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw "git is not available on PATH."
    }

    $selected = Select-Lane
    switch ($selected) {
        "live" {
            Sync-Branch $repoRoot "main"
            Invoke-BuildAndShortcut
        }
        "dev" {
            Sync-Branch $repoRoot "dev"
            Invoke-BuildAndShortcut
        }
        "aday" {
            Sync-Branch $repoRoot "main"
            Sync-AdayPrivateSource
            Invoke-BuildAndShortcut
            Invoke-AdayDeploy
        }
    }

    Write-Step "Done" Green
    Write-Host "Lane '$selected' is ready." -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "Lane update stopped: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
