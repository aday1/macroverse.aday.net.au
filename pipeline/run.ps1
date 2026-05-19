# Run Macroverse 42 - The Wired Atelier from correct working directory.
# Refreshes shortcut on each launch so it stays up to date. Maximizes console so ASCII art is visible.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "Macroverse42.exe"
if (-not (Test-Path $exe)) {
    Write-Host "Macroverse42.exe not found. Run pipeline\build.ps1 first." -ForegroundColor Red
    exit 1
}
chcp 65001 | Out-Null

# Maximize the console window so ASCII art displays correctly
if ($host.Name -eq "ConsoleHost") {
    try {
        $null = Add-Type -Name Win -Namespace Console -MemberDefinition '[DllImport("kernel32.dll")] public static extern IntPtr GetConsoleWindow(); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);' -ErrorAction SilentlyContinue
        $hwnd = [Console.Win]::GetConsoleWindow()
        if ($hwnd -ne [IntPtr]::Zero) { [Console.Win]::ShowWindow($hwnd, 3) | Out-Null }
    } catch { }
}

$createShortcut = Join-Path $PSScriptRoot "create-shortcut.ps1"
if (Test-Path $createShortcut) { & $createShortcut 2>$null }
Set-Location $root
& $exe
