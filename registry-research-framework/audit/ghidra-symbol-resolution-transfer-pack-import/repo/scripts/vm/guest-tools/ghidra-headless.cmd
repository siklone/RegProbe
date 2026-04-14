@echo off
setlocal
set "ROOT=C:\Tools\Ghidra"
for /f "delims=" %%I in ('dir /b /ad "%ROOT%\ghidra_*" 2^>nul') do (
  if exist "%ROOT%\%%I\support\analyzeHeadless.bat" (
    call "%ROOT%\%%I\support\analyzeHeadless.bat" %*
    exit /b %errorlevel%
  )
)
echo analyzeHeadless.bat was not found under %ROOT%.
exit /b 1
