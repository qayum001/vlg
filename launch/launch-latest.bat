@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0launch.ps1"
set "launchExit=%errorlevel%"
if not "%launchExit%"=="0" (
    echo.
    pause
)
exit /b %launchExit%
