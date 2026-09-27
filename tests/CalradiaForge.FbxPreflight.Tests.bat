@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0CalradiaForge.FbxPreflight.Tests.ps1"
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %* <nul
exit /b %ERRORLEVEL%
