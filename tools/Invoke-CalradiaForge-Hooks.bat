@echo off
setlocal
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Invoke-CalradiaForge-Hooks.ps1" %*
exit /b %ERRORLEVEL%
