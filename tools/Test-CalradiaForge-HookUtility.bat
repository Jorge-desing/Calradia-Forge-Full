@echo off
setlocal
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Test-CalradiaForge-HookUtility.ps1"
exit /b %ERRORLEVEL%
