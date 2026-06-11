# Macroverse 42 - The Wired Atelier - minimal shader index scanner
# Usage: .\shader-index.ps1 scan
# Scans shaders/ folder and updates shader-index.json

param(
    [Parameter(Position=0)]
    [ValidateSet("scan","list")]
    [string]$Command = "scan",
    [string]$Source = "",
    [string]$IndexPath = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$settingsPath = Join-Path $root "shader-preview-settings.json"
$idxPath = if ($IndexPath) { $IndexPath } else { Join-Path $root "shader-index.json" }
$srcPaths = @("shaders")
if (Test-Path $settingsPath) {
    try {
        $j = Get-Content $settingsPath -Raw | ConvertFrom-Json
        if ($j.sourcePaths -and $j.sourcePaths.Count -gt 0) { $srcPaths = @($j.sourcePaths) }
        if ($j.indexPath) { $idxPath = $j.indexPath }
    } catch {}
}
if ($Source) { $srcPaths = @($Source) }

$exts = @(".frag", ".fs", ".glsl", ".vert", ".txt")
$entries = @()
$id = 1
foreach ($src in $srcPaths) {
    $absSrc = if ([System.IO.Path]::IsPathRooted($src)) { $src } else { Join-Path $root $src }
    if (-not (Test-Path $absSrc)) { continue }
    Get-ChildItem -Path $absSrc -Recurse -File | Where-Object { $exts -contains $_.Extension.ToLower() } | ForEach-Object {
        $rel = $_.FullName.Replace($root + [System.IO.Path]::DirectorySeparatorChar, "").Replace("\", "/")
        $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
        $fmt = if ($_.Extension -eq ".fs") { "isf" } else { "glsl" }
        $parts = $rel -split "[/\\]"
        if ($parts.Length -ge 3 -and $parts[0] -eq "shaders") {
            if ($parts[1] -eq "VJ-Sorted-Production" -and $parts[2] -eq "ISF" -and $parts.Length -ge 4) { $cat = $parts[3] }
            elseif ($parts[1] -eq "core" -and $parts.Length -ge 3) { $cat = $parts[2] }
            else { $cat = $parts[1] }
        } elseif ($parts.Length -ge 2) { $cat = $parts[1] }
        else { $cat = if ($fmt -eq "isf") { "isf" } else { "glsl" } }
        $entries += @{ id = $id; path = $rel; name = $name; category = $cat; tags = @(); format = $fmt }
        $id++
    }
}
$json = $entries | ConvertTo-Json
$json | Set-Content -Path $idxPath -Encoding UTF8
Write-Host "Scanned $($entries.Count) shaders -> $idxPath" -ForegroundColor Green
