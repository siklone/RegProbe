@echo off
setlocal
set SCRIPT_DIR=%~dp0
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%run-dpc-timer-etw-trace-guest.ps1" -GuestRoot "."
echo.
echo Press any key to close this window.
pause >nul
