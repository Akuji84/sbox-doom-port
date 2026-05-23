@echo off
setlocal

set "ROOT=%~dp0"
set "PORT=8090"
set "TARGET_DIR=%ROOT%third_party\W95-Web-Project"
set "URL=http://127.0.0.1:%PORT%/third_party/W95-Web-Project/index.html"

if not exist "%TARGET_DIR%\index.html" (
    echo W95-Web-Project was not found at:
    echo %TARGET_DIR%
    pause
    exit /b 1
)

where python >nul 2>nul
if errorlevel 1 (
    echo Python was not found on PATH.
    echo Install Python or update PATH, then run this file again.
    pause
    exit /b 1
)

echo Starting W95 Web Project server on port %PORT%...
echo Open this URL in your browser:
echo %URL%
echo.

cd /d "%ROOT%"
python -m http.server %PORT%

endlocal
