@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0CalradiaForge.AssetBatchPlan.Tests.ps1"
if not defined CALRADIAFORGE_POWERSHELL set "CALRADIAFORGE_POWERSHELL=powershell.exe"
"%CALRADIAFORGE_POWERSHELL%" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %* <nul
exit /b %ERRORLEVEL%
