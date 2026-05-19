# Macroverse 42 - The Wired Atelier build: Vite frontend + Go backend
# Use -GoOnly to skip frontend build (rebuild Go binary only)
param([switch]$GoOnly)
$ErrorActionPreference = "Stop"

# Refresh PATH so npm from recent Node.js install is found
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
$nodePaths = @("C:\Program Files\nodejs", "C:\Program Files (x86)\nodejs", "$env:APPDATA\npm")
foreach ($p in $nodePaths) {
    if (Test-Path $p) { $env:Path = $p + ";" + $env:Path }
}

$root = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $root "frontend"
$api = Join-Path $root "api"
$dist = Join-Path $frontend "dist"
$frontendBuild = Join-Path $root "frontend-build"

Write-Host ""
Write-Host "  Macroverse 42 build" -ForegroundColor Cyan
Write-Host ""

$defaultSettings = Join-Path $root "shader-preview-settings.default.json"
$rootSettings = Join-Path $root "shader-preview-settings.json"
if (-not (Test-Path $rootSettings) -and (Test-Path $defaultSettings)) {
    Copy-Item $defaultSettings -Destination $rootSettings -Force
    Write-Host "  created shader-preview-settings.json from default" -ForegroundColor Gray
}

# Step 1: Vite build (or use existing dist if npm unavailable; skip if -GoOnly)
if (-not $GoOnly) {
if (-not (Test-Path $frontend)) { throw "frontend folder not found: $frontend" }
$hasNpm = Get-Command npm -ErrorAction SilentlyContinue
if ($hasNpm) {
    Push-Location $frontend
    try {
        npm install 2>&1
        if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
        npm run build 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Vite build failed (npm run build exited $LASTEXITCODE)" }
    } finally {
        Pop-Location
    }
} elseif (Test-Path $dist) {
    Write-Host "  npm not in PATH - using existing frontend/dist" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "  npm not found. To build the frontend:" -ForegroundColor Red
    Write-Host "    1. Install Node.js from https://nodejs.org" -ForegroundColor Gray
    Write-Host "    2. Open a new terminal (to refresh PATH)" -ForegroundColor Gray
    Write-Host "    3. Run: cd $frontend; npm install; npm run build" -ForegroundColor Gray
    Write-Host ""
    throw "npm not found and no existing dist folder. Install Node.js or build frontend manually."
}
if (-not (Test-Path $dist)) {
    throw "Vite build did not produce dist folder. Fix TypeScript/build errors and retry."
}

# Step 2: Copy to frontend-build (dist only)
if (Test-Path $frontendBuild) { Remove-Item $frontendBuild -Recurse -Force }
New-Item -ItemType Directory -Path $frontendBuild -Force | Out-Null
Copy-Item (Join-Path $dist "*") -Destination $frontendBuild -Recurse -Force
Write-Host "  frontend-build ready" -ForegroundColor Green
} elseif (-not (Test-Path $frontendBuild)) {
    throw "No frontend-build and -GoOnly skip. Run full build once with npm first."
}

# Step 3: Go build - kill any running Macroverse so exe can be overwritten
$out = Join-Path $root "Macroverse42.exe"
Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Stopping Macroverse42 (PID $($_.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}
Push-Location $api
try {
    go build -o $out .
    if ($LASTEXITCODE -ne 0) { throw "Go build failed" }
    Write-Host "  Macroverse42.exe built" -ForegroundColor Green
} finally {
    Pop-Location
}

$createShortcut = Join-Path $PSScriptRoot "create-shortcut.ps1"
if (Test-Path $createShortcut) { & $createShortcut 2>$null }
Write-Host ""
Write-Host "  BUILD COMPLETE. Run: .\Macroverse42.exe or use desktop shortcut" -ForegroundColor Green
Write-Host ""
