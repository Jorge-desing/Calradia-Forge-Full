@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0Inspect-CalradiaForge-Fbx.ps1"
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
