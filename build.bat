@echo off
:: ─────────────────────────────────────────────────────────────────
:: Macroverse 42 — build script (Windows)
:: Requires: Go 1.21+, Node.js 20+
:: ─────────────────────────────────────────────────────────────────
setlocal

set ROOT=%~dp0
set BIN=%ROOT%Macroverse42.exe
if "%~1"=="" (set VERSION=dev) else (set VERSION=%~1)

echo.
echo   MACROVERSE 42 -- build
echo   -----------------------------------------

:: Frontend
echo   [1/2] Building frontend...
cd /d "%ROOT%frontend"
call npm ci --silent
call npm run build --silent
if errorlevel 1 (
    echo   ERROR: frontend build failed
    exit /b 1
)
xcopy /E /Y /Q "%ROOT%frontend\dist\" "%ROOT%frontend-build\" >nul
echo         frontend-build -^> dist\ (synced to frontend-build\)

:: Go binary
echo   [2/2] Building Go binary...
cd /d "%ROOT%api"
set CGO_ENABLED=0
go build -ldflags="-s -w -X main.releaseTag=%VERSION%" -o "%BIN%" .
if errorlevel 1 (
    echo   ERROR: Go build failed
    exit /b 1
)

echo.
echo   Built: Macroverse42.exe
echo   Run:   Macroverse42.exe
echo   Then open http://localhost:8765
echo.
endlocal
