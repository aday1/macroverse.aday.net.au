# Run Macroverse 42 - The Wired Atelier from the correct working directory.
# Lane-aware runner: live/dev use the repo library; aday uses a private local source on loopback only.
param(
    [ValidateSet("menu", "live", "dev", "aday")]
    [string]$Lane = "menu",
    [switch]$NoPrompt
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
$createShortcut = Join-Path $PSScriptRoot "create-shortcut.ps1"
$adayConfigPath = Join-Path $root "ops\aday-shaders.local.json"
$localStateDir = Join-Path $root ".local-state"

if (-not (Test-Path $exe)) {
    Write-Host "Macroverse42.exe not found. Run pipeline\build.ps1 first." -ForegroundColor Red
    exit 1
}

chcp 65001 | Out-Null

if ($host.Name -eq "ConsoleHost") {
    try {
        $null = Add-Type -Name Win -Namespace Console -MemberDefinition '[DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);' -ErrorAction SilentlyContinue
        $hwnd = [Console.Win]::GetConsoleWindow()
        if ($hwnd -ne [IntPtr]::Zero) { [Console.Win]::ShowWindow($hwnd, 3) | Out-Null }
    } catch { }
}

if (Test-Path $createShortcut) { & $createShortcut 2>$null }

function Select-RunLane {
    if ($Lane -ne "menu") { return $Lane }
    if ($NoPrompt) { return "aday" }

    Write-Host ""
    Write-Host "Macroverse boot lane" -ForegroundColor Cyan
    Write-Host "  1. Live/local repo library"
    Write-Host "  2. Dev/local repo library"
    Write-Host "  3. Aday private local library (loopback only)"
    Write-Host ""
    $choice = Read-Host "Choose 1, 2, or 3 [3]"
    if ([string]::IsNullOrWhiteSpace($choice)) { $choice = "3" }
    switch ($choice.ToLowerInvariant()) {
        "1" { return "live" }
        "live" { return "live" }
        "2" { return "dev" }
        "dev" { return "dev" }
        default { return "aday" }
    }
}

function Clear-PrivateEnv {
    foreach ($name in @(
        "MACROVERSE_PRIVATE_LIBRARY",
        "MACROVERSE_FORCE_ENV_SOURCES",
        "MACROVERSE_ADAY_AUTHORIZED",
        "MACROVERSE_ADAY_OBSIDIAN_MARKER",
        "MACROVERSE_SOURCE_PATHS",
        "VFX_GLSL_ROOT",
        "SHADER_INDEX_DB"
    )) {
        Remove-Item -LiteralPath "Env:\$name" -ErrorAction SilentlyContinue
    }
}

function Test-AdayIdentity {
    $userOk = $env:USERNAME -ieq "aday"
    $vault = Join-Path $env:USERPROFILE "Desktop\Obsidian\YomikosPapers"
    $vaultOk = Test-Path -LiteralPath $vault -PathType Container
    [pscustomobject]@{
        Ok = ($userOk -and $vaultOk)
        UserOk = $userOk
        VaultOk = $vaultOk
        Vault = $vault
    }
}

function Get-AdayConfig {
    if (-not (Test-Path -LiteralPath $adayConfigPath)) {
        throw "Missing private shader config: $adayConfigPath"
    }
    Get-Content -LiteralPath $adayConfigPath -Raw | ConvertFrom-Json
}

function Set-LaneEnvironment {
    param([string]$SelectedLane)

    $env:MACROVERSE_HOST_MODE = "desktop"
    $env:MACROVERSE_LANE = $SelectedLane

    if ($SelectedLane -ne "aday") {
        Clear-PrivateEnv
        $env:MACROVERSE_BIND_HOST = "0.0.0.0"
        Write-Host "Lane: $SelectedLane (repo library)" -ForegroundColor Cyan
        Write-Host "Bind: 0.0.0.0 (LAN reachable)" -ForegroundColor DarkGray
        return
    }

    $identity = Test-AdayIdentity
    if (-not $identity.Ok) {
        throw "Aday private lane refused. user=aday: $($identity.UserOk), Obsidian vault present: $($identity.VaultOk)"
    }

    $cfg = Get-AdayConfig
    $shaderSource = [string]$cfg.shadersSource
    if ([string]::IsNullOrWhiteSpace($shaderSource) -or -not (Test-Path -LiteralPath $shaderSource -PathType Container)) {
        throw "Aday private shader source missing. Check ops\aday-shaders.local.json: $shaderSource"
    }

    New-Item -ItemType Directory -Path $localStateDir -Force | Out-Null
    $env:MACROVERSE_PRIVATE_LIBRARY = "1"
    $env:MACROVERSE_FORCE_ENV_SOURCES = "1"
    $env:MACROVERSE_ADAY_AUTHORIZED = "1"
    $env:MACROVERSE_ADAY_OBSIDIAN_MARKER = [string]$identity.Vault
    $env:MACROVERSE_SOURCE_PATHS = $shaderSource
    $env:VFX_GLSL_ROOT = $shaderSource
    $env:SHADER_INDEX_DB = Join-Path $localStateDir "macroverse-aday.db"
    $env:MACROVERSE_BIND_HOST = "127.0.0.1"

    Write-Host "Lane: aday private" -ForegroundColor Green
    Write-Host "Bind: 127.0.0.1 (local machine only)" -ForegroundColor DarkGray
    Write-Host "Private source: $shaderSource" -ForegroundColor DarkGray
    Write-Host "Private index: $env:SHADER_INDEX_DB" -ForegroundColor DarkGray
}

$selectedLane = Select-RunLane
Set-LaneEnvironment $selectedLane

Set-Location $root
Write-Host ""
Write-Host "Starting Macroverse at http://localhost:8765 (auto-shifts if the port is busy)..." -ForegroundColor Cyan
& $exe
