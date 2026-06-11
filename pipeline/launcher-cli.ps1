# Macroverse - Wired Atelier (41) Pipeline Launcher - Console (no GUI). Full-screen terminal.
# Desktop shortcut runs this for shell-only, maximized console.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
$buildPs1 = Join-Path $PSScriptRoot "build.ps1"
$runPs1 = Join-Path $PSScriptRoot "run.ps1"
$createShortcut = Join-Path $PSScriptRoot "create-shortcut.ps1"
$laneUpdatePs1 = Join-Path $PSScriptRoot "Update-MacroverseLane.ps1"
$indexPs1 = Join-Path $root "shader-index.ps1"
$bulkThumbs = Join-Path $root "scripts\bulk-thumbnails.js"
$defaultSettings = Join-Path $root "shader-preview-settings.default.json"
$settingsPath = Join-Path $root "shader-preview-settings.json"
$nukePs1 = Join-Path $PSScriptRoot "nuke-vj-production.ps1"
$fixFailedPs1 = Join-Path $PSScriptRoot "fix-failed-shaders.ps1"
$port = "8765"
$webUrl = "http://localhost:$port"
$repoRoot = $root

chcp 65001 | Out-Null
if ($host.Name -eq "ConsoleHost") {
    try {
        $null = Add-Type -Name Win -Namespace Console -MemberDefinition '[DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);' -ErrorAction SilentlyContinue
        $hwnd = [Console.Win]::GetConsoleWindow()
        if ($hwnd -ne [IntPtr]::Zero) { [Console.Win]::ShowWindow($hwnd, 3) | Out-Null }
    } catch { }
}

if (Test-Path $createShortcut) { & $createShortcut 2>$null }

Set-Location $root

function Do-LaunchExe {
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$runPs1`"" -WorkingDirectory $root -WindowStyle Maximized
    Write-Host "Launched server in new maximized window." -ForegroundColor Green
}

function Do-KillSessions {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "Killed Macroverse42 sessions." -ForegroundColor Yellow
}

function Do-KillAll {
    $procs = Get-Process | Where-Object { $_.ProcessName -like "*macroverse*" }
    $count = ($procs | Measure-Object).Count
    $procs | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "Killed $count process(es)." -ForegroundColor Yellow
}

function Do-OpenBrowser {
    Start-Process $webUrl
    Write-Host "Opened $webUrl" -ForegroundColor Green
}

function Do-Rebuild {
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$buildPs1`"" -WorkingDirectory $root
    Write-Host "Rebuild running in new window." -ForegroundColor Green
}

function Do-Reindex {
    if (Test-Path $indexPs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$indexPs1`" scan" -WorkingDirectory $root
        Write-Host "Reindex running in new window." -ForegroundColor Green
    } else {
        Write-Host "shader-index.ps1 not found. Run Rebuild first." -ForegroundColor Red
    }
}

function Do-RegenThumbs {
    if (-not (Test-Path $bulkThumbs)) {
        Write-Host "scripts\bulk-thumbnails.js not found. Start Macroverse - Wired Atelier first and use in-app Generate thumbnails." -ForegroundColor Red
        return
    }
    Start-Process powershell -WindowStyle Hidden -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"cd '$root'; node scripts/bulk-thumbnails.js`"" -WorkingDirectory $root
    Write-Host "Regenerating thumbnails in background (server must be on $port)." -ForegroundColor Green
}

function Do-HardResetDb {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    $dbPath = Join-Path $root "macroverse.db"
    if (Test-Path $dbPath) {
        Remove-Item $dbPath -Force
        Write-Host "macroverse.db deleted. Reindex or launch to rebuild." -ForegroundColor Yellow
    } else {
        Write-Host "No macroverse.db found." -ForegroundColor Gray
    }
}

function Do-FactoryReset {
    Write-Host "Reset settings, DB, thumbnails, errors to defaults? Macroverse - Wired Atelier will be killed. (y/N): " -NoNewline
    $q = Read-Host
    if ($q -ne "y" -and $q -ne "Y") { return }
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    $dbPath = Join-Path $root "macroverse.db"
    $thumbPath = Join-Path $root "thumbnails.json"
    $errPath = Join-Path $root "shader-errors.json"
    foreach ($p in @($dbPath, $thumbPath, $errPath)) {
        if (Test-Path $p) { Remove-Item $p -Force -ErrorAction SilentlyContinue }
    }
    if (Test-Path $defaultSettings) {
        Copy-Item $defaultSettings -Destination $settingsPath -Force
    }
    Write-Host "Factory reset done. Launch exe to start fresh." -ForegroundColor Green
}

function Do-ClearCache {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 200
    $thumbPath = Join-Path $root "thumbnails.json"
    if (Test-Path $thumbPath) {
        Remove-Item $thumbPath -Force
        Write-Host "thumbnails.json deleted. Reload app to regenerate." -ForegroundColor Yellow
    } else {
        Write-Host "No thumbnails cache found." -ForegroundColor Gray
    }
}

function Do-Update {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    Write-Host "Choose a lane, then Macroverse will fetch, pull, rebuild, and refresh shortcuts." -ForegroundColor Cyan
    & $laneUpdatePs1
}

function Do-UpdateLane {
    param([string]$Lane)
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    & $laneUpdatePs1 -Lane $Lane
}

function Do-DeployAday {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    & $laneUpdatePs1 -Lane aday -DeployAday
}

function Do-UpdateShortcuts {
    & $createShortcut
    Write-Host "Desktop shortcuts updated." -ForegroundColor Green
}

function Do-Nuke {
    Write-Host "Hard reset VJ-Sorted-Production (or configured path) with backup branch? (y/N): " -NoNewline
    $q = Read-Host
    if ($q -ne "y" -and $q -ne "Y") { return }
    if (Test-Path $nukePs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$nukePs1`"" -WorkingDirectory $root
        Write-Host "NUKE script running in new window (backup branch created)." -ForegroundColor Yellow
    } else {
        Write-Host "nuke-vj-production.ps1 not found." -ForegroundColor Red
    }
}

function Do-FixFailed {
    if (Test-Path $fixFailedPs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$fixFailedPs1`"" -WorkingDirectory $root
        Write-Host "Fix-failed-shaders: pick a shader to launch agent + log to pipeline." -ForegroundColor Green
    } else {
        Write-Host "fix-failed-shaders.ps1 not found." -ForegroundColor Red
    }
}

while ($true) {
    Write-Host ""
    Write-Host "Macroverse - Wired Atelier (41) Launcher (shell)" -ForegroundColor Cyan
    Write-Host "  Web: $webUrl" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  1. Launch exe (new window)    2. Kill sessions    3. Kill all MV"
    Write-Host "  4. New session (new window)  5. Open in browser  6. Rebuild exe"
    Write-Host "  7. Reindex                   8. Regen thumbnails  9. Hard reset DB"
    Write-Host "  A. Factory reset             B. Clear cache      C. Update lane menu"
    Write-Host "  D. Update shortcuts         E. NUKE             F. Fix failed shaders"
    Write-Host "  G. Live main update          H. Dev update       I. Aday private update"
    Write-Host "  J. Deploy Aday private shaders"
    Write-Host "  Q. Quit"
    Write-Host ""
    $choice = Read-Host "Choice"
    switch ($choice) {
        "1" { Do-LaunchExe }
        "2" { Do-KillSessions }
        "3" { Do-KillAll }
        "4" { Do-LaunchExe; Write-Host "New session started in separate window." -ForegroundColor Green }
        "5" { Do-OpenBrowser }
        "6" { Do-Rebuild }
        "7" { Do-Reindex }
        "8" { Do-RegenThumbs }
        "9" { Do-HardResetDb }
        "A" { Do-FactoryReset }
        "a" { Do-FactoryReset }
        "B" { Do-ClearCache }
        "b" { Do-ClearCache }
        "C" { Do-Update }
        "c" { Do-Update }
        "D" { Do-UpdateShortcuts }
        "d" { Do-UpdateShortcuts }
        "E" { Do-Nuke }
        "e" { Do-Nuke }
        "F" { Do-FixFailed }
        "f" { Do-FixFailed }
        "G" { Do-UpdateLane "live" }
        "g" { Do-UpdateLane "live" }
        "H" { Do-UpdateLane "dev" }
        "h" { Do-UpdateLane "dev" }
        "I" { Do-UpdateLane "aday" }
        "i" { Do-UpdateLane "aday" }
        "J" { Do-DeployAday }
        "j" { Do-DeployAday }
        "Q" { Write-Host "Bye." -ForegroundColor Gray; exit 0 }
        "q" { Write-Host "Bye." -ForegroundColor Gray; exit 0 }
        default { Write-Host "Unknown option." -ForegroundColor DarkGray }
    }
}
