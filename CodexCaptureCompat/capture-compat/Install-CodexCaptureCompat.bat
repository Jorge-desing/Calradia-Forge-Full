@echo off
setlocal
if "%~1"=="" (
  echo Usage: Install-CodexCaptureCompat.bat "full path to codex-computer-use.exe" [Install^|Uninstall] [-WhatIf]
  exit /b 2
)
set "ACTION=Install"
if not "%~2"=="" set "ACTION=%~2"
if /i not "%ACTION%"=="Install" if /i not "%ACTION%"=="Uninstall" (
  echo Action must be Install or Uninstall.
  exit /b 2
)
set "WHATIF="
if /i "%~3"=="-WhatIf" set "WHATIF=-WhatIf"
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -HelperPath "%~1" -Action "%ACTION%" %WHATIF%
exit /b %ERRORLEVEL%
