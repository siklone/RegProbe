@echo off
setlocal

set "STATE=%~1"
if "%STATE%"=="" set "STATE=0"

set "OUTDIR=C:\Tools\Perf\GameModeSuiteV2"
set "LOG=C:\Users\Administrator\Desktop\gm-state-%STATE%.log"

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

powershell -ExecutionPolicy Bypass -File C:\Tools\Scripts\gamemode-occt-wpr-v2.ps1 -State %STATE% -OutputDirectory "%OUTDIR%" -WaitSeconds 20 > "%LOG%" 2>&1
exit /b %ERRORLEVEL%
