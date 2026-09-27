@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0Plan-CalradiaForge-AssetBatch.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
