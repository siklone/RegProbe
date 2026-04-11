@echo off
setlocal
set "WPR=%ProgramFiles(x86)%\Windows Kits\10\Windows Performance Toolkit\wpr.exe"
if not exist "%WPR%" set "WPR=%SystemRoot%\System32\wpr.exe"
if not exist "%WPR%" (
  echo wpr.exe was not found.
  exit /b 1
)
"%WPR%" %*
