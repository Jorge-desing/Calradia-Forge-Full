@echo off
setlocal EnableExtensions
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Measure-CodexCaptureCompatProcessCpu.ps1"
set "RESULT=%ERRORLEVEL%"
exit /b %RESULT%
