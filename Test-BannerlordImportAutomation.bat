@echo off
setlocal EnableExtensions
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-BannerlordImportAutomation.ps1"
exit /b %ERRORLEVEL%
