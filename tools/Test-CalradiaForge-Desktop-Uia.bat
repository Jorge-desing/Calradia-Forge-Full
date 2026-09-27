@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Test-CalradiaForge-Desktop-Uia.ps1" %*
exit /b %ERRORLEVEL%
