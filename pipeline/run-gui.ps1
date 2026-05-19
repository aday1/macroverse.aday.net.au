# Run Macroverse 42 - The Wired Atelier in background and show GUI (open browser, stop server). Fullscreen-capable with animations.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
$port = "8765"
$webUrl = "http://localhost:$port"

if (-not (Test-Path $exe)) {
    [System.Windows.Forms.MessageBox]::Show("Macroverse42.exe not found. Run Macroverse - Wired Atelier and click Rebuild exe.", "Macroverse 42 - The Wired Atelier", "OK", "Warning")
    exit 1
}

Set-Location $root
$proc = Start-Process -FilePath $exe -PassThru -WindowStyle Hidden -WorkingDirectory $root

$textBright = [System.Drawing.Color]::FromArgb(241, 241, 241)

$form = New-Object System.Windows.Forms.Form
$form.Text = "Macroverse 42 - The Wired Atelier - Server"
$form.Size = New-Object System.Drawing.Size(320, 198)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(45, 45, 48)
$form.ForeColor = $textBright
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$lb = New-Object System.Windows.Forms.Label
$lb.Text = "Server running at " + $webUrl
$lb.ForeColor = [System.Drawing.Color]::FromArgb(0, 122, 204)
$lb.Location = New-Object System.Drawing.Point(20, 24)
$lb.AutoSize = $true
$form.Controls.Add($lb)

$btnOpen = New-Object System.Windows.Forms.Button
$btnOpen.Text = "Open in browser"
$btnOpen.Size = New-Object System.Drawing.Size(120, 28)
$btnOpen.Location = New-Object System.Drawing.Point(20, 56)
$btnOpen.FlatStyle = "Flat"
$btnOpen.BackColor = [System.Drawing.Color]::FromArgb(0, 122, 204)
$btnOpen.ForeColor = [System.Drawing.Color]::White
$btnOpen.Add_Click({ Start-Process $webUrl })
$form.Controls.Add($btnOpen)

$btnStop = New-Object System.Windows.Forms.Button
$btnStop.Text = "Stop server"
$btnStop.Size = New-Object System.Drawing.Size(120, 28)
$btnStop.Location = New-Object System.Drawing.Point(155, 56)
$btnStop.FlatStyle = "Flat"
$btnStop.BackColor = [System.Drawing.Color]::FromArgb(160, 60, 60)
$btnStop.ForeColor = [System.Drawing.Color]::White
$btnStop.Add_Click({
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
    $form.Close()
})
$form.Controls.Add($btnStop)

$launcherPs1 = Join-Path $PSScriptRoot "launcher.ps1"
$btnFull = New-Object System.Windows.Forms.Button
$btnFull.Text = "Full launcher (all options)"
$btnFull.Size = New-Object System.Drawing.Size(255, 28)
$btnFull.Location = New-Object System.Drawing.Point(20, 92)
$btnFull.FlatStyle = "Flat"
$btnFull.BackColor = [System.Drawing.Color]::FromArgb(70, 90, 110)
$btnFull.ForeColor = [System.Drawing.Color]::White
$btnFull.Add_Click({
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$launcherPs1`"" -WorkingDirectory $root
})
$form.Controls.Add($btnFull)

$form.Add_FormClosing({
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
})

$form.ShowDialog()
