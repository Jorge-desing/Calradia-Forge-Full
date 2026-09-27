@echo off
setlocal EnableExtensions
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0CalradiaForge.TpacInventory.Tests.ps1" %* <nul
set "RESULT=%ERRORLEVEL%"
exit /b %RESULT%
