@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0Inspect-CalradiaForge-Fbx.ps1"
if not defined CALRADIAFORGE_POWERSHELL set "CALRADIAFORGE_POWERSHELL=powershell.exe"
"%CALRADIAFORGE_POWERSHELL%" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
