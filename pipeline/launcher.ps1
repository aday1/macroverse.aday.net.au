# Macroverse 42 - The Wired Atelier Pipeline Launcher - GUI (Amiga / Synthwave / Demoscene aesthetic)
# Desktop shortcut targets this script. Launcher refreshes the shortcut on each run.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
$buildPs1 = Join-Path $PSScriptRoot "build.ps1"
$runPs1 = Join-Path $PSScriptRoot "run.ps1"
$createShortcut = Join-Path $PSScriptRoot "create-shortcut.ps1"
$laneUpdatePs1 = Join-Path $PSScriptRoot "Update-MacroverseLane.ps1"
$runBridgePs1 = Join-Path $PSScriptRoot "run-bridge.ps1"
$indexPs1 = Join-Path $root "shader-index.ps1"
$bulkThumbs = Join-Path $root "scripts\bulk-thumbnails.js"
$defaultSettings = Join-Path $root "shader-preview-settings.default.json"
$settingsPath = Join-Path $root "shader-preview-settings.json"
$port = "8765"
$webUrl = "http://localhost:$port"

# Read skipSplash from settings (same key as app uses)
$skipSplash = $false
if (Test-Path $settingsPath) {
    try {
        $settingsJson = Get-Content $settingsPath -Raw
        if ($settingsJson -match '"skipSplash"\s*:\s*true') { $skipSplash = $true }
    } catch { }
}

if (Test-Path $createShortcut) { & $createShortcut 2>$null }

if (-not $skipSplash) {
    $splash = New-Object System.Windows.Forms.Form
    $splash.FormBorderStyle = "None"
    $splash.Size = New-Object System.Drawing.Size(380, 120)
    $splash.StartPosition = "CenterScreen"
    $splash.Text = "Macroverse - Wired Atelier"
    $splash.BackColor = [System.Drawing.Color]::FromArgb(37, 37, 38)
    $splash.Topmost = $true
    $splash.Opacity = 0
    $splash.ShowInTaskbar = $false

    $splashLb = New-Object System.Windows.Forms.Label
    $splashLb.Text = "Updating shortcut..."
    $splashLb.ForeColor = [System.Drawing.Color]::FromArgb(220, 220, 220)
    $splashLb.Font = New-Object System.Drawing.Font("Segoe UI", 11)
    $splashLb.AutoSize = $true
    $splashLb.Location = New-Object System.Drawing.Point(28, 28)
    $splash.Controls.Add($splashLb)

    $splashBar = New-Object System.Windows.Forms.ProgressBar
    $splashBar.Style = "Marquee"
    $splashBar.MarqueeAnimationSpeed = 40
    $splashBar.Location = New-Object System.Drawing.Point(28, 68)
    $splashBar.Size = New-Object System.Drawing.Size(324, 18)
    $splash.Controls.Add($splashBar)

    $splashPhase = 0
    $splashMessages = @("Updating shortcut...", "Checking paths...", "Almost ready...", "Macroverse 42 - The Wired Atelier")
    $splashCloseTimer = New-Object System.Windows.Forms.Timer
    $splashCloseTimer.Interval = 2600
    $splashCloseTimer.Add_Tick({
        $splashCloseTimer.Stop()
        $splashCloseTimer.Dispose()
        $splashAnim.Stop()
        $splashAnim.Dispose()
        $splashFade.Stop()
        $splashFade.Dispose()
        $splash.Close()
    })

    $splashAnim = New-Object System.Windows.Forms.Timer
    $splashAnim.Interval = 480
    $splashAnim.Add_Tick({
        $script:splashPhase = ($script:splashPhase + 1) % $splashMessages.Length
        $splashLb.Text = $splashMessages[$script:splashPhase]
        $r = 200 + ($script:splashPhase * 15)
        $g = 200 + ($script:splashPhase * 10)
        $b = 220
        if ($r -gt 255) { $r = 255 }; if ($g -gt 255) { $g = 255 }
        $splashLb.ForeColor = [System.Drawing.Color]::FromArgb($r, $g, $b)
    })

    $splashFade = New-Object System.Windows.Forms.Timer
    $splashFade.Interval = 30
    $fadeStep = 0
    $splashFade.Add_Tick({
        $script:fadeStep += 1
        if ($script:fadeStep -le 12) {
            $splash.Opacity = [Math]::Min(1.0, $script:fadeStep / 12.0)
        }
    })

    $splash.Add_Shown({
        $splashFade.Start()
        $splashAnim.Start()
        $splashCloseTimer.Start()
    })
    [void]$splash.ShowDialog()
}

$repoRoot = $root
$gitDir = Join-Path $repoRoot ".git"
if (Test-Path $gitDir) {
    $oldErr = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    Push-Location $repoRoot | Out-Null
    git pull 2>$null | Out-Null
    Pop-Location -ErrorAction SilentlyContinue
    $ErrorActionPreference = $oldErr
}

function Get-BuildAge {
    if (-not (Test-Path $exe)) { return $null }
    $age = (Get-Date) - (Get-Item $exe).LastWriteTimeUtc
    if ($age.TotalMinutes -lt 1) { return "just now" }
    if ($age.TotalMinutes -lt 60) { return ([int]$age.TotalMinutes) + "m ago" }
    if ($age.TotalHours -lt 24) { return ([int]$age.TotalHours) + "h ago" }
    return ([int]$age.TotalDays) + "d ago"
}

function Get-GitInfo {
    if (-not (Test-Path $gitDir)) { return $null }
    $oldErr = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    try {
        Push-Location $repoRoot | Out-Null
        $branch = git rev-parse --abbrev-ref HEAD 2>$null
        $rev = git rev-parse --short HEAD 2>$null
        $msg = git log -1 --format="%s" 2>$null
        if (-not $rev) { return $null }
        return (@{ Branch = $branch; Rev = $rev; Message = $msg })
    } finally {
        $ErrorActionPreference = $oldErr
        Pop-Location -ErrorAction SilentlyContinue
    }
}

function Test-GitBehind {
    if (-not (Test-Path $gitDir)) { return $null }
    $oldErr = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    try {
        Push-Location $repoRoot | Out-Null
        git fetch 2>$null | Out-Null
        foreach ($branch in @("origin/main", "origin/master")) {
            $c = git rev-list "HEAD..$branch" --count 2>$null
            if ($c -match '^\d+$') { return [int]$c }
        }
    } catch { }
    finally {
        $ErrorActionPreference = $oldErr
        Pop-Location -ErrorAction SilentlyContinue
    }
    return $null
}

# Colors: dark theme, single accent (all as variables so AddButton never gets comma-separated FromArgb in args)
$bgDark = [System.Drawing.Color]::FromArgb(37, 37, 38)
$bgPanel = [System.Drawing.Color]::FromArgb(45, 45, 48)
$textBright = [System.Drawing.Color]::FromArgb(241, 241, 241)
$textDim = [System.Drawing.Color]::FromArgb(180, 180, 180)
$accent = [System.Drawing.Color]::FromArgb(0, 122, 204)
$cRed = [System.Drawing.Color]::FromArgb(180, 60, 60)
$cRedDark = [System.Drawing.Color]::FromArgb(140, 40, 40)
$cBlue = [System.Drawing.Color]::FromArgb(60, 100, 140)
$cGreen = [System.Drawing.Color]::FromArgb(60, 120, 80)
$cThumb = [System.Drawing.Color]::FromArgb(80, 100, 120)
$cFactory = [System.Drawing.Color]::FromArgb(160, 60, 80)
$cCache = [System.Drawing.Color]::FromArgb(100, 80, 60)
$cUpdate = [System.Drawing.Color]::FromArgb(90, 70, 50)
$cShortcuts = [System.Drawing.Color]::FromArgb(70, 90, 110)
$cNuke = [System.Drawing.Color]::FromArgb(120, 40, 40)
$cFixFailed = [System.Drawing.Color]::FromArgb(100, 80, 140)
$cAday = [System.Drawing.Color]::FromArgb(130, 80, 40)
$cyan = $accent
$magenta = [System.Drawing.Color]::FromArgb(180, 100, 160)
$gold = [System.Drawing.Color]::FromArgb(200, 160, 80)
$copper = $accent

$formWidth = 436
$formHeight = 736
$form = New-Object System.Windows.Forms.Form
$form.Text = "Macroverse - Wired Atelier"
$form.Size = New-Object System.Drawing.Size($formWidth, $formHeight)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = $bgDark
$form.ForeColor = $textBright
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

# Title bar
$titlePanel = New-Object System.Windows.Forms.Panel
$titlePanel.Height = 36
$titlePanel.Dock = [System.Windows.Forms.DockStyle]::Top
$titlePanel.BackColor = $bgPanel
$form.Controls.Add($titlePanel)

$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Text = "Macroverse 42 - The Wired Atelier Launcher"
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = $textBright
$titleLabel.AutoSize = $true
$titleLabel.Location = New-Object System.Drawing.Point(12, 10)
$titlePanel.Controls.Add($titleLabel)

# Update-available banner
$updateLabel = New-Object System.Windows.Forms.Label
$updateLabel.Location = New-Object System.Drawing.Point(12, 42)
$updateLabel.Size = New-Object System.Drawing.Size(412, 24)
$updateLabel.AutoSize = $false
$updateLabel.Text = ""
$updateLabel.ForeColor = [System.Drawing.Color]::FromArgb(255, 200, 80)
$updateLabel.BackColor = [System.Drawing.Color]::FromArgb(60, 50, 20)
$updateLabel.Visible = $false
$form.Controls.Add($updateLabel)

$behind = $null
try { $behind = Test-GitBehind } catch { }
if ($null -ne $behind -and $behind -gt 0) {
    $updateLabel.Text = "  Update available: $behind commit(s) behind origin. Pull then Rebuild."
    $updateLabel.Visible = $true
}

$y = 72
$btnH = 28
$btnW = 132
$gap = 8

$laneLabel = New-Object System.Windows.Forms.Label
$laneLabel.Text = "Boot lane"
$laneLabel.Location = New-Object System.Drawing.Point(12, $y)
$laneLabel.Size = New-Object System.Drawing.Size(74, 22)
$laneLabel.ForeColor = $textDim
$laneLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($laneLabel)

$laneSelect = New-Object System.Windows.Forms.ComboBox
$laneSelect.DropDownStyle = [System.Windows.Forms.ComboBoxStyle]::DropDownList
$laneSelect.Location = New-Object System.Drawing.Point(92, ($y - 2))
$laneSelect.Size = New-Object System.Drawing.Size(150, 24)
[void]$laneSelect.Items.Add("aday")
[void]$laneSelect.Items.Add("live")
[void]$laneSelect.Items.Add("dev")
$laneSelect.SelectedItem = "aday"
$laneSelect.BackColor = $bgPanel
$laneSelect.ForeColor = $textBright
$form.Controls.Add($laneSelect)

$laneHint = New-Object System.Windows.Forms.Label
$laneHint.Text = "Aday uses private source on 127.0.0.1"
$laneHint.Location = New-Object System.Drawing.Point(252, $y)
$laneHint.Size = New-Object System.Drawing.Size(172, 24)
$laneHint.ForeColor = $textDim
$laneHint.Font = New-Object System.Drawing.Font("Segoe UI", 8)
$form.Controls.Add($laneHint)

$y += 42

function AddButton($text, $x, $refY, $color, $click) {
    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = $text
    $btn.Size = New-Object System.Drawing.Size($btnW, $btnH)
    $btn.Location = New-Object System.Drawing.Point($x, $refY)
    $btn.FlatStyle = "Flat"
    $btn.BackColor = $color
    $btn.ForeColor = [System.Drawing.Color]::White
    $btn.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    $btn.FlatAppearance.BorderColor = [System.Drawing.Color]::FromArgb(80, 80, 80)
    $btn.FlatAppearance.BorderSize = 1
    $btn.Add_Click($click)
    $form.Controls.Add($btn)
    return $btn
}

function Get-SelectedRunLane {
    if ($laneSelect -and $laneSelect.SelectedItem) { return [string]$laneSelect.SelectedItem }
    return "aday"
}

$btnRun = AddButton "Choose lane" 12 $y $accent {
    Set-Location $root
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$runPs1`"" -WorkingDirectory $root -WindowStyle Maximized
    $statusLabel.Text = "Opening boot lane selector. Enter defaults to Aday private."
}
$btnKill = AddButton "Kill sessions" (12 + $btnW + $gap) $y $cRed {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $statusLabel.Text = "Killed Macroverse42 sessions."
}
$btnKillAll = AddButton "Kill all MV" (12 + 2*($btnW + $gap)) $y $cRedDark {
    $procs = Get-Process | Where-Object { $_.ProcessName -like "*macroverse*" }
    $count = ($procs | Measure-Object).Count
    $procs | Stop-Process -Force -ErrorAction SilentlyContinue
    $statusLabel.Text = "Killed $count process(es)."
}

$y += $btnH + $gap
$btnNewSession = AddButton "New session" 12 $y $accent {
    Set-Location $root
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$runPs1`"" -WorkingDirectory $root -WindowStyle Maximized
    $statusLabel.Text = "New session started with boot lane selector."
}
$btnWeb = AddButton "Open in browser" (12 + $btnW + $gap) $y $cBlue {
    Start-Process $webUrl
}
$btnRebuild = AddButton "Rebuild exe" (12 + 2*($btnW + $gap)) $y $magenta {
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$buildPs1`"" -WorkingDirectory $root
}

$y += $btnH + $gap
$btnReindex = AddButton "Reindex" 12 $y $cGreen {
    if (Test-Path $indexPs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$indexPs1`" scan" -WorkingDirectory $root
    } else {
        [System.Windows.Forms.MessageBox]::Show("shader-index.ps1 not found. Run Rebuild first.", "Reindex", "OK", "Warning")
    }
}
$btnThumbs = AddButton "Regen thumbnails" (12 + $btnW + $gap) $y $cThumb {
    if (-not (Test-Path $bulkThumbs)) {
        [System.Windows.Forms.MessageBox]::Show("scripts\bulk-thumbnails.js not found. Start Macroverse - Wired Atelier first and use in-app Generate thumbnails.", "Thumbnails", "OK", "Warning")
        return
    }
    $statusLabel.Text = "Regenerating thumbnails in background (server must be on $port)."
    Start-Process powershell -WindowStyle Hidden -ArgumentList "-NoProfile -ExecutionPolicy Bypass -Command `"cd '$root'; node scripts/bulk-thumbnails.js`"" -WorkingDirectory $root
}
$btnHardReset = AddButton "Hard reset DB" (12 + 2*($btnW + $gap)) $y $gold {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    $dbPath = Join-Path $root "macroverse.db"
    if (Test-Path $dbPath) {
        Remove-Item $dbPath -Force
        $statusLabel.Text = "macroverse.db deleted. Reindex or launch to rebuild."
    } else {
        $statusLabel.Text = "No macroverse.db found."
    }
}

$y += $btnH + $gap
$btnFactory = AddButton "Factory reset" 12 $y $cFactory {
    $q = [System.Windows.Forms.MessageBox]::Show("Reset settings, DB, thumbnails, errors to defaults? Macroverse - Wired Atelier will be killed.", "Factory reset", "YesNo", "Warning")
    if ($q -ne "Yes") { return }
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
    $statusLabel.Text = "Factory reset done. Launch exe to start fresh."
}
$btnClearCache = AddButton "Clear cache" (12 + $btnW + $gap) $y $cCache {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 200
    $thumbPath = Join-Path $root "thumbnails.json"
    if (Test-Path $thumbPath) {
        Remove-Item $thumbPath -Force
        $statusLabel.Text = "thumbnails.json deleted. Reload app to regenerate."
    } else {
        $statusLabel.Text = "No thumbnails cache found."
    }
}
$btnPull = AddButton "Update lane" (12 + 2*($btnW + $gap)) $y $cUpdate {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
    $statusLabel.Text = "Killed sessions. Choose live/dev/aday in the update window."
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$laneUpdatePs1`"" -WorkingDirectory $repoRoot
}

$y += $btnH + $gap
$nukePs1 = Join-Path $PSScriptRoot "nuke-vj-production.ps1"
$fixFailedPs1 = Join-Path $PSScriptRoot "fix-failed-shaders.ps1"
$btnShortcuts = AddButton "Update shortcuts" 12 $y $cShortcuts {
    & $createShortcut
    $statusLabel.Text = "Desktop shortcuts updated."
}
$btnNuke = AddButton "NUKE" (12 + $btnW + $gap) $y $cNuke {
    $q = [System.Windows.Forms.MessageBox]::Show("Hard reset VJ-Sorted-Production (or configured path) with backup branch? This cannot be undone except by checking out the backup branch.", "NUKE", "YesNo", "Warning")
    if ($q -ne "Yes") { return }
    if (Test-Path $nukePs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$nukePs1`"" -WorkingDirectory $root
        $statusLabel.Text = "NUKE script running in new window (backup branch created)."
    } else {
        $statusLabel.Text = "nuke-vj-production.ps1 not found."
    }
}
$btnFixFailed = AddButton "Fix failed shaders" (12 + 2*($btnW + $gap)) $y $cFixFailed {
    if (Test-Path $fixFailedPs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$fixFailedPs1`"" -WorkingDirectory $root
        $statusLabel.Text = "Fix-failed-shaders: pick a shader to launch agent + log to pipeline."
    } else {
        $statusLabel.Text = "fix-failed-shaders.ps1 not found."
    }
}

$y += $btnH + $gap
$btnBridge = AddButton "Link bridge" 12 $y $cGreen {
    if (Test-Path $runBridgePs1) {
        Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$runBridgePs1`"" -WorkingDirectory $root
        $statusLabel.Text = "Ableton Link bridge starting. Launch Macroverse first if token minting fails."
    } else {
        $statusLabel.Text = "run-bridge.ps1 not found."
    }
}
$btnBackendViewer = AddButton "Open app" (12 + $btnW + $gap) $y $cBlue {
    Start-Process $webUrl
    $statusLabel.Text = "Opened app; Backend viewer is bottom right."
}
$btnAdayRun = AddButton "Run Aday" (12 + 2*($btnW + $gap)) $y $cAday {
    Set-Location $root
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$runPs1`" -Lane aday" -WorkingDirectory $root -WindowStyle Maximized
    $statusLabel.Text = "Launching Aday private lane."
}

$y += $btnH + $gap
$btnLiveLane = AddButton "Live main" 12 $y $cUpdate {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $statusLabel.Text = "Updating live/main lane, then rebuilding and refreshing shortcuts."
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$laneUpdatePs1`" -Lane live" -WorkingDirectory $repoRoot
}
$btnDevLane = AddButton "Dev branch" (12 + $btnW + $gap) $y $magenta {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $statusLabel.Text = "Updating dev/test lane, then rebuilding and refreshing shortcuts."
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$laneUpdatePs1`" -Lane dev" -WorkingDirectory $repoRoot
}
$btnAdayLane = AddButton "Aday private" (12 + 2*($btnW + $gap)) $y $cAday {
    Get-Process -Name "Macroverse42" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $statusLabel.Text = "Updating main plus the local private Aday source if it is a git checkout."
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$laneUpdatePs1`" -Lane aday" -WorkingDirectory $repoRoot
}

$y += $btnH + $gap + 8

# Build age + Git info
$infoPanel = New-Object System.Windows.Forms.Panel
$infoPanel.Location = New-Object System.Drawing.Point(12, $y)
$infoPanel.Size = New-Object System.Drawing.Size(412, 64)
$infoPanel.BackColor = $bgPanel
$infoPanel.BorderStyle = "FixedSingle"
$form.Controls.Add($infoPanel)

$buildAgeLabel = New-Object System.Windows.Forms.Label
$buildAgeLabel.Location = New-Object System.Drawing.Point(8, 6)
$buildAgeLabel.AutoSize = $true
$buildAgeLabel.ForeColor = $textDim
$buildAgeLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$infoPanel.Controls.Add($buildAgeLabel)

$gitLabel = New-Object System.Windows.Forms.Label
$gitLabel.Location = New-Object System.Drawing.Point(8, 24)
$gitLabel.Size = New-Object System.Drawing.Size(396, 34)
$gitLabel.AutoSize = $false
$gitLabel.ForeColor = $textDim
$gitLabel.Font = New-Object System.Drawing.Font("Segoe UI", 8)
$gitLabel.Text = ""
$infoPanel.Controls.Add($gitLabel)

$y += 72
$statusLabel = New-Object System.Windows.Forms.Label
$statusLabel.Location = New-Object System.Drawing.Point(12, $y)
$statusLabel.Size = New-Object System.Drawing.Size(412, 48)
$statusLabel.AutoSize = $false
$statusLabel.Text = ""
$statusLabel.ForeColor = $textDim
$statusLabel.Font = New-Object System.Drawing.Font("Segoe UI", 8)
$form.Controls.Add($statusLabel)

# Populate build age and git
$exeStatus = if (Test-Path $exe) { "OK" } else { "MISSING - Rebuild" }
$age = Get-BuildAge
$buildAgeLabel.Text = "Build: $exeStatus" + $(if ($age) { "  |  Age: $age" } else { "" })

$gitInfo = Get-GitInfo
if ($gitInfo) {
    $shortMsg = $gitInfo.Message
    if ($shortMsg.Length -gt 52) { $shortMsg = $shortMsg.Substring(0, 49) + "..." }
    $gitLabel.Text = "Git: $($gitInfo.Branch) @ $($gitInfo.Rev)`n$shortMsg"
} else {
    $gitLabel.Text = "Git: (not a repo or no commits)"
}

$statusLabel.Text = "Shortcut refreshed on launch. Web: $webUrl"

$form.ShowDialog()
