@echo off
setlocal EnableExtensions
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0CalradiaForge.TpacInventoryCompare.Tests.ps1" %* <nul
exit /b %ERRORLEVEL%
