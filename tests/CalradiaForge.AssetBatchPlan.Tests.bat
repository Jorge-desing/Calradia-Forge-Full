@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0CalradiaForge.AssetBatchPlan.Tests.ps1"
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %* <nul
exit /b %ERRORLEVEL%
