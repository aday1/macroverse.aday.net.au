# Start the Macroverse bridge agent against a local Macroverse server.
# Requires the app server to be running so the script can mint a bridge token.
param(
    [string]$ServerUrl = "http://localhost:8765",
    [string]$SessionId = "default",
    [string]$BridgeId = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$agentRoot = Join-Path $root "macroverse-bridge-agent"

if ([string]::IsNullOrWhiteSpace($BridgeId)) {
    $BridgeId = "mv-local-$env:COMPUTERNAME"
}

if (-not (Test-Path -LiteralPath $agentRoot -PathType Container)) {
    throw "Bridge agent folder not found: $agentRoot"
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm is not available on PATH. Install Node.js or open a fresh terminal after installing it."
}

Push-Location $agentRoot
try {
    if (-not (Test-Path -LiteralPath (Join-Path $agentRoot "node_modules"))) {
        Write-Host "Installing bridge dependencies..." -ForegroundColor Cyan
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    }

    Write-Host "Building bridge agent..." -ForegroundColor Cyan
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "bridge build failed" }
} finally {
    Pop-Location
}

$body = @{
    bridgeId = $BridgeId
    sessionId = $SessionId
    expiresIn = 86400
} | ConvertTo-Json

Write-Host "Minting bridge token from $ServerUrl..." -ForegroundColor Cyan
$tokenResponse = Invoke-RestMethod -Method Post -Uri ($ServerUrl.TrimEnd("/") + "/api/bridge/token") -ContentType "application/json" -Body $body
if (-not $tokenResponse.token) {
    throw "Bridge token was not returned. Is Macroverse running at $ServerUrl?"
}

$env:BRIDGE_TOKEN = [string]$tokenResponse.token
$env:CLOUD_URL = $ServerUrl
$env:BRIDGE_ID = $BridgeId
$env:BRIDGE_SESSION_ID = $SessionId

Write-Host "Starting bridge $BridgeId for session $SessionId" -ForegroundColor Green
Write-Host "Ableton Link dependency is installed if npm install succeeded." -ForegroundColor DarkGray
Push-Location $agentRoot
try {
    npm start -- --cloud-url $ServerUrl --session-id $SessionId --bridge-id $BridgeId
} finally {
    Pop-Location
}
