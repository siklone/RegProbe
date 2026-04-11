@echo off
setlocal
set "WPA=%ProgramFiles(x86)%\Windows Kits\10\Windows Performance Toolkit\wpa.exe"
if not exist "%WPA%" (
  echo wpa.exe was not found.
  exit /b 1
)
"%WPA%" %*
