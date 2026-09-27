@echo off
setlocal
set "ACTION=%~1"
if "%ACTION%"=="" set "ACTION=Status"
if /i not "%ACTION%"=="Install" if /i not "%ACTION%"=="Status" if /i not "%ACTION%"=="Uninstall" (
  echo Usage: Manage-CodexCaptureCompatPersistence.bat [Install^|Status^|Uninstall]
  exit /b 2
)
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0persistence.ps1" -Action "%ACTION%"
exit /b %ERRORLEVEL%
