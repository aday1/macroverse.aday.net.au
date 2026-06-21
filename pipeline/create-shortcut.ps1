# Create or update Desktop shortcuts for Macroverse 42 - The Wired Atelier.
# Creates console, GUI, and server-runner shortcuts; launchers refresh shortcuts on each run.
# Uses a sharp neon portal icon (generated or from pipeline/icon/macroverse.ico).
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
$launcherCliPs1 = Join-Path $PSScriptRoot "launcher-cli.ps1"
$launcherPs1 = Join-Path $PSScriptRoot "launcher.ps1"
$iconDir = Join-Path $PSScriptRoot "icon"
$iconPath = Join-Path $iconDir "macroverse.ico"
$iconSourcePath = Join-Path $iconDir "macroverse-icon.png"
$desktop = [Environment]::GetFolderPath("Desktop")
$desktopGroup = Join-Path $desktop "Desktop Groups\02 Projects and Vaults"

if (-not (Test-Path $launcherCliPs1) -or -not (Test-Path $launcherPs1)) {
    Write-Host "launcher-cli.ps1 or launcher.ps1 not found." -ForegroundColor Red
    exit 1
}

function Ensure-MacroverseIcon {
    if ((Test-Path $iconPath) -and $env:MACROVERSE_FORCE_ICON -ne "1") { return }
    if (-not (Test-Path $iconDir)) { New-Item -ItemType Directory -Path $iconDir -Force | Out-Null }
    $size = 256

    if (Test-Path -LiteralPath $iconSourcePath) {
        $src = [System.Drawing.Image]::FromFile($iconSourcePath)
        $bmp = New-Object System.Drawing.Bitmap($size, $size)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($src, 0, 0, $size, $size)
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $ms.ToArray()
        $ms.Dispose()
        $g.Dispose()
        $bmp.Dispose()
        $src.Dispose()
        $icoHeader = [byte[]]@(
            0, 0, 1, 0, 1, 0,
            0, 0, 0, 0, 1, 0, 32, 0,
            [byte]($pngBytes.Length -band 0xFF),
            [byte](($pngBytes.Length -shr 8) -band 0xFF),
            [byte](($pngBytes.Length -shr 16) -band 0xFF),
            [byte](($pngBytes.Length -shr 24) -band 0xFF),
            22, 0, 0, 0
        )
        [System.IO.File]::WriteAllBytes($iconPath, $icoHeader + $pngBytes)
        Write-Host "  Icon generated from source: $iconPath" -ForegroundColor Gray
        return
    }

    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::FromArgb(8, 12, 24))

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Point]::new(0, 0),
        [System.Drawing.Point]::new(0, $size),
        [System.Drawing.Color]::FromArgb(8, 12, 24),
        [System.Drawing.Color]::FromArgb(31, 18, 58)
    )
    $g.FillRectangle($bgBrush, 0, 0, $size, $size)
    $bgBrush.Dispose()

    $gridPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(42, 102, 126), 2)
    for ($i = 32; $i -lt $size; $i += 32) {
        $g.DrawLine($gridPen, $i, 24, $i - 48, 232)
        $g.DrawLine($gridPen, 24, $i, 232, $i - 48)
    }
    $gridPen.Dispose()

    $portalBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Rectangle]::new(34, 34, 188, 188),
        [System.Drawing.Color]::FromArgb(255, 45, 220, 225),
        [System.Drawing.Color]::FromArgb(255, 226, 61, 180),
        35
    )
    $g.FillEllipse($portalBrush, 36, 36, 184, 184)
    $portalBrush.Dispose()

    $innerBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        [System.Drawing.Rectangle]::new(58, 58, 140, 140),
        [System.Drawing.Color]::FromArgb(18, 22, 40),
        [System.Drawing.Color]::FromArgb(42, 22, 66),
        90
    )
    $g.FillEllipse($innerBrush, 58, 58, 140, 140)
    $innerBrush.Dispose()

    $ringPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(245, 239, 180, 72), 5)
    $g.DrawEllipse($ringPen, 49, 49, 158, 158)
    $ringPen.Dispose()

    $rayPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(185, 62, 232, 227), 3)
    foreach ($angle in @(0, 45, 90, 135, 180, 225, 270, 315)) {
        $rad = [Math]::PI * $angle / 180
        $x1 = 128 + [Math]::Cos($rad) * 74
        $y1 = 128 + [Math]::Sin($rad) * 74
        $x2 = 128 + [Math]::Cos($rad) * 106
        $y2 = 128 + [Math]::Sin($rad) * 106
        $g.DrawLine($rayPen, [int]$x1, [int]$y1, [int]$x2, [int]$y2)
    }
    $rayPen.Dispose()

    $font = New-Object System.Drawing.Font("Segoe UI Black", 58, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 246, 242, 214))
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(120, 0, 0, 0))
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $textRect = [System.Drawing.RectangleF]::new(30, 80, 196, 90)
    $shadowRect = [System.Drawing.RectangleF]::new(34, 84, 196, 90)
    $g.DrawString("M42", $font, $shadowBrush, $shadowRect, $format)
    $g.DrawString("M42", $font, $textBrush, $textRect, $format)
    $format.Dispose()
    $shadowBrush.Dispose()
    $textBrush.Dispose()
    $font.Dispose()

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
        Description = "Version 42. Run server in foreground and choose live/dev/aday at boot. Use main shortcut for menu (Reindex, NUKE, etc.)."
        Target = $launchMacroverseCmd
        Args = ""
        WindowStyle = 3
    },
    @{
        Name = "Macroverse - Aday Private"
        Description = "Version 42. Ask for boot lane; Enter defaults to local Aday private shader library on 127.0.0.1."
        Target = $launchMacroverseCmd
        Args = ""
        WindowStyle = 3
    }
)

function Save-MacroverseShortcut {
    param(
        [string]$Directory,
        [hashtable]$Spec
    )

    $shortcutPath = Join-Path $Directory "$($Spec.Name).lnk"
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.WorkingDirectory = $root
    $shortcut.TargetPath = $Spec.Target
    $shortcut.Arguments = $Spec.Args
    $shortcut.Description = $Spec.Description
    $shortcut.WindowStyle = $Spec.WindowStyle
    if ($iconLocation) { $shortcut.IconLocation = "$iconLocation,0" }
    $shortcut.Save()
    Write-Host "Shortcut updated: $shortcutPath" -ForegroundColor Green
}

foreach ($s in $shortcuts) {
    Save-MacroverseShortcut -Directory $desktop -Spec $s
}

if (Test-Path -LiteralPath $desktopGroup -PathType Container) {
    foreach ($s in $shortcuts) {
        $groupShortcut = Join-Path $desktopGroup "$($s.Name).lnk"
        if (Test-Path -LiteralPath $groupShortcut) {
            Save-MacroverseShortcut -Directory $desktopGroup -Spec $s
        }
    }
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
