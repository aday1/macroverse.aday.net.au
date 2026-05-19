# Fix failed shaders: list open/unrecoverable errors, launch cursor-agent in shader folder with fix prompt,
# and keep a record in pipeline/debug-fix-record.txt for the debug/improvement pipeline.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$errPath = Join-Path $root "shader-errors.json"
$unrecPath = Join-Path $root "unrecoverable-shaders.json"
$recordPath = Join-Path $PSScriptRoot "debug-fix-record.txt"
$exe = Join-Path $root "Macroverse42.exe"

function Find-CursorAgent {
    $agent = $null
    foreach ($name in @("cursor-agent", "agent")) {
        $agent = Get-Command $name -ErrorAction SilentlyContinue
        if ($agent) { return $agent.Source }
    }
    $exeDir = Split-Path -Parent $exe
    if (Test-Path $exeDir) {
        foreach ($f in @("cursor-agent.exe", "cursor-agent.cmd", "cursor-agent.bat")) {
            $p = Join-Path $exeDir $f
            if (Test-Path $p) { return $p }
        }
    }
    $local = Join-Path $env:LOCALAPPDATA "cursor-agent"
    if (Test-Path $local) {
        foreach ($f in @("cursor-agent.exe", "cursor-agent.cmd", "cursor-agent.bat")) {
            $p = Join-Path $local $f
            if (Test-Path $p) { return $p }
        }
    }
    return $null
}

function Get-ErrorEntries {
    $entries = @()
    if (Test-Path $errPath) {
        $data = Get-Content $errPath -Raw | ConvertFrom-Json
        foreach ($e in $data) {
            if ($e.status -eq "open") {
                $entries += [PSCustomObject]@{
                    Source = "shader-errors"
                    Path = $e.path
                    Filename = $e.filename
                    Error = $e.error
                    Id = $e.id
                }
            }
        }
    }
    if (Test-Path $unrecPath) {
        $data = Get-Content $unrecPath -Raw | ConvertFrom-Json
        foreach ($e in $data) {
            $entries += [PSCustomObject]@{
                Source = "unrecoverable"
                Path = $e.path
                Filename = $e.filename
                Error = $e.compileError
                Id = "unrec"
            }
        }
    }
    return $entries
}

function Resolve-FullPath($path, $filename) {
    $p = $path -replace '\|', [System.IO.Path]::DirectorySeparatorChar
    $p = $p -replace '/', [System.IO.Path]::DirectorySeparatorChar
    if (-not [System.IO.Path]::IsPathRooted($p)) {
        $p = Join-Path $root $p
    }
    if ($filename -and -not $p.EndsWith($filename)) {
        $dir = Split-Path -Parent $p
        if (-not $dir) { $dir = $root }
        $p = Join-Path $dir $filename
    }
    if (Test-Path $p) { return $p }
    $dir = Split-Path -Parent $p
    if (Test-Path $dir) { return $p }
    return $null
}

$entries = Get-ErrorEntries
if ($entries.Count -eq 0) {
    [System.Windows.Forms.MessageBox]::Show("No open shader errors or unrecoverable shaders found. (Check shader-errors.json and unrecoverable-shaders.json)", "Fix failed shaders", "OK", "Information")
    exit 0
}

$choices = $entries | ForEach-Object {
    $shortErr = $_.Error
    if ($shortErr.Length -gt 60) { $shortErr = $shortErr.Substring(0, 57) + "..." }
    "$($_.Filename) | $($_.Path) | $shortErr"
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "Fix failed shaders - pick one"
$scr = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$form.Size = New-Object System.Drawing.Size([Math]::Min(800, [int]($scr.Width * 0.75)), [Math]::Min(520, [int]($scr.Height * 0.7)))
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "Sizable"
$form.MaximizeBox = $true
$form.WindowState = [System.Windows.Forms.FormWindowState]::Maximized
$form.BackColor = [System.Drawing.Color]::FromArgb(28, 18, 52)
$form.ForeColor = [System.Drawing.Color]::FromArgb(220, 210, 255)
$form.Font = New-Object System.Drawing.Font("Consolas", 11)

$lb = New-Object System.Windows.Forms.ListBox
$lb.Location = New-Object System.Drawing.Point(16, 16)
$lb.Size = New-Object System.Drawing.Size(600, 340)
$lb.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
$lb.Font = New-Object System.Drawing.Font("Consolas", 11)
foreach ($c in $choices) { [void]$lb.Items.Add($c) }
$lb.SelectedIndex = 0

$btnFix = New-Object System.Windows.Forms.Button
$btnFix.Text = "Launch Agent and fix"
$btnFix.Location = New-Object System.Drawing.Point(16, 368)
$btnFix.Size = New-Object System.Drawing.Size(200, 40)
$btnFix.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left
$btnFix.BackColor = [System.Drawing.Color]::FromArgb(0, 255, 255)
$btnFix.ForeColor = [System.Drawing.Color]::Black
$btnFix.FlatStyle = "Flat"
$btnFix.Font = New-Object System.Drawing.Font("Consolas", 10)

$btnCancel = New-Object System.Windows.Forms.Button
$btnCancel.Text = "Cancel"
$btnCancel.Location = New-Object System.Drawing.Point(228, 368)
$btnCancel.Size = New-Object System.Drawing.Size(120, 40)
$btnCancel.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left
$btnCancel.FlatStyle = "Flat"
$btnCancel.Font = New-Object System.Drawing.Font("Consolas", 10)

$form.Controls.Add($lb)
$form.Controls.Add($btnFix)
$form.Controls.Add($btnCancel)

$agentExe = Find-CursorAgent
if (-not $agentExe) {
    $btnFix.Enabled = $false
    $form.Controls.Add((New-Object System.Windows.Forms.Label -Property @{
        Location = [System.Drawing.Point]::new(360, 376)
        Size = [System.Drawing.Size]::new(300, 40)
        Text = "cursor-agent not found in PATH or next to Macroverse42.exe"
        ForeColor = [System.Drawing.Color]::FromArgb(255, 120, 80)
    }))
}

$script:chosen = $null
$btnFix.Add_Click({
    $idx = $lb.SelectedIndex
    if ($idx -lt 0) { return }
    $script:chosen = $entries[$idx]
    $form.DialogResult = "OK"
    $form.Close()
})
$btnCancel.Add_Click({ $form.DialogResult = "Cancel"; $form.Close() })

if ($form.ShowDialog() -ne "OK" -or -not $script:chosen) { exit 0 }

$fullPath = Resolve-FullPath -path $script:chosen.Path -filename $script:chosen.Filename
if (-not $fullPath) {
    [System.Windows.Forms.MessageBox]::Show("Could not resolve path: $($script:chosen.Path) / $($script:chosen.Filename)", "Fix failed shaders", "OK", "Warning")
    exit 1
}

$shaderDir = Split-Path -Parent $fullPath
$fileName = Split-Path -Leaf $fullPath
$errShort = $script:chosen.Error
if ($errShort.Length -gt 400) { $errShort = $errShort.Substring(0, 397) + "..." }

$prompt = @"
You are an expert GLSL/ISF shader debugger in Macroverse - Wired Atelier. Edit ONLY the file $fileName in this directory.
The shader failed to compile. The compiler reported:

$errShort

Fix the compile error with minimal changes. Preserve the visual intent. For 'uniform only allowed at global scope': move ALL uniform declarations to the top. For for-loops use float i or int i, never uniform float i. Do not assign to uniforms - use local variables.
If this is ISF: use FRAMEINDEX for animation, include useFrameIndex INPUT, use IMG_NORM_PIXEL for image inputs, do not redeclare TIME/RENDERSIZE.
"@

$promptFile = Join-Path $shaderDir "MACROVERSE_FIX_PROMPT.txt"
$prompt | Set-Content -Path $promptFile -Encoding UTF8

$recordLine = (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + " | " + $fullPath + " | " + ($script:chosen.Error.Substring(0, [Math]::Min(120, $script:chosen.Error.Length))) + " | launched_agent"
$recordDir = Split-Path -Parent $recordPath
if (-not (Test-Path $recordDir)) { New-Item -ItemType Directory -Path $recordDir -Force | Out-Null }
Add-Content -Path $recordPath -Value $recordLine

if (-not $agentExe) {
    [System.Windows.Forms.MessageBox]::Show("cursor-agent not found. Prompt saved to $promptFile - open that folder and run the agent manually.", "Fix failed shaders", "OK", "Warning")
    Start-Process "explorer.exe" -ArgumentList "/select,`"$promptFile`""
    exit 0
}

$inner = "cd /d `"$shaderDir`" && `"$agentExe`" --trust"
Start-Process cmd -ArgumentList "/c", "start", "Cursor Agent - Fix shader", "cmd", "/k", $inner -WorkingDirectory $shaderDir
[System.Windows.Forms.MessageBox]::Show("Agent launched in: $shaderDir`nPrompt saved to MACROVERSE_FIX_PROMPT.txt - paste it into the agent. Logged to pipeline\debug-fix-record.txt", "Fix failed shaders", "OK", "Information")
