@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0Plan-CalradiaForge-AssetBatch.ps1"
if not defined CALRADIAFORGE_POWERSHELL set "CALRADIAFORGE_POWERSHELL=powershell.exe"
"%CALRADIAFORGE_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
