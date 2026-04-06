@echo off
setlocal
set "XPERF=%ProgramFiles(x86)%\Windows Kits\10\Windows Performance Toolkit\xperf.exe"
if not exist "%XPERF%" (
  echo xperf.exe was not found.
  exit /b 1
)
"%XPERF%" %*
