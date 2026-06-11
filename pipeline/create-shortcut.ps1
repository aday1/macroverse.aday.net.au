# Create or update Desktop shortcuts for Macroverse 42 - The Wired Atelier.
# Creates console, GUI, and server-runner shortcuts; launchers refresh shortcuts on each run.
# Uses a copper/synthwave-style icon (generated or from pipeline/icon/macroverse.ico).
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
$launcherCliPs1 = Join-Path $PSScriptRoot "launcher-cli.ps1"
$launcherPs1 = Join-Path $PSScriptRoot "launcher.ps1"
$iconDir = Join-Path $PSScriptRoot "icon"
$iconPath = Join-Path $iconDir "macroverse.ico"
$desktop = [Environment]::GetFolderPath("Desktop")

if (-not (Test-Path $launcherCliPs1) -or -not (Test-Path $launcherPs1)) {
    Write-Host "launcher-cli.ps1 or launcher.ps1 not found." -ForegroundColor Red
    exit 1
}

function Ensure-MacroverseIcon {
    if (Test-Path $iconPath) { return }
    if (-not (Test-Path $iconDir)) { New-Item -ItemType Directory -Path $iconDir -Force | Out-Null }
    $size = 256
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.Clear([System.Drawing.Color]::FromArgb(18, 10, 38))
    $copper = [System.Drawing.Color]::FromArgb(255, 127, 0)
    $magenta = [System.Drawing.Color]::FromArgb(255, 0, 128)
    $cyan = [System.Drawing.Color]::FromArgb(0, 255, 255)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Point]::new(0, 0),
        [System.Drawing.Point]::new($size, $size),
        $copper,
        $magenta
    )
    $g.FillEllipse($brush, 40, 40, $size - 80, $size - 80)
    $pen = New-Object System.Drawing.Pen($cyan, 4)
    $g.DrawEllipse($pen, 44, 44, $size - 88, $size - 88)
    $pen.Dispose()
    $brush.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    $ms.Dispose()
    $g.Dispose()
    $bmp.Dispose()
    $icoHeader = [byte[]]@(
        0, 0, 1, 0, 1, 0,
        0, 0, 0, 0, 1, 0, 32, 0,
        [byte]($pngBytes.Length -band 0xFF),
        [byte](($pngBytes.Length -shr 8) -band 0xFF),
        [byte](($pngBytes.Length -shr 16) -band 0xFF),
        [byte](($pngBytes.Length -shr 24) -band 0xFF),
        22, 0, 0, 0
    )
    $icoBytes = $icoHeader + $pngBytes
    [System.IO.File]::WriteAllBytes($iconPath, $icoBytes)
    Write-Host "  Icon generated: $iconPath" -ForegroundColor Gray
}

$runPs1 = Join-Path $PSScriptRoot "run.ps1"
$runGuiPs1 = Join-Path $PSScriptRoot "run-gui.ps1"
Ensure-MacroverseIcon
$iconLocation = $iconPath
if (-not (Test-Path $iconPath)) { $iconLocation = $null }

$shell = New-Object -ComObject WScript.Shell

# Use .cmd launchers as shortcut target so Arguments stay empty and the .lnk doesn't get corrupted
$launchPipelineCmd = Join-Path $PSScriptRoot "launch-pipeline.cmd"
$launchMacroverseCmd = Join-Path $PSScriptRoot "launch-macroverse.cmd"
# Main = full launcher (CLI menu). GUI = splash/button launcher. Server only = run exe in foreground.
$shortcuts = @(
    @{
        Name = "Macroverse - Wired Atelier"
        Description = "Version 42. Full shell launcher: run, rebuild, reindex, live/dev/aday-private updates, shortcuts, cleanup tools."
        Target = $launchPipelineCmd
        Args = ""
        WindowStyle = 3
    },
    @{
        Name = "Macroverse - Wired Atelier - GUI"
        Description = "Version 42. GUI launcher with splash screen and live/dev/aday-private update buttons."
        Target = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
        Args = "-NoProfile -ExecutionPolicy Bypass -File `"$launcherPs1`""
        WindowStyle = 1
    },
    @{
        Name = "Macroverse - Wired Atelier - Server only"
        Description = "Version 42. Run server in foreground in full-screen console. Use main shortcut for menu (Reindex, NUKE, etc.)."
        Target = $launchMacroverseCmd
        Args = ""
        WindowStyle = 3
    }
)

foreach ($s in $shortcuts) {
    $shortcutPath = Join-Path $desktop "$($s.Name).lnk"
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.WorkingDirectory = $root
    $shortcut.TargetPath = $s.Target
    $shortcut.Arguments = $s.Args
    $shortcut.Description = $s.Description
    $shortcut.WindowStyle = $s.WindowStyle
    if ($iconLocation) { $shortcut.IconLocation = "$iconLocation,0" }
    $shortcut.Save()
    Write-Host "Shortcut updated: $shortcutPath" -ForegroundColor Green
}

foreach ($old in @("Macroverse V3", "Macroverse V5", "Macroverse", "Macroverse - Run app", "Macroverse - Server only", "MV Pipeline", "MV Run", "Macroverse Pipeline")) {
    $oldPath = Join-Path $desktop "$old.lnk"
    if (Test-Path $oldPath) {
        Remove-Item $oldPath -Force
        Write-Host "Removed old shortcut: $oldPath" -ForegroundColor Gray
    }
}

[System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell) | Out-Null
Write-Host "  Macroverse - Wired Atelier = shell menu.  GUI = splash/buttons.  Server only = exe foreground." -ForegroundColor Gray
Write-Host "  Start In: $root" -ForegroundColor Gray
Write-Host "  Icon: $iconPath" -ForegroundColor Gray
