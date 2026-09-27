@echo off
setlocal EnableExtensions
set "SCRIPT=%~dp0CalradiaForge.ResourceBrowser.Tests.ps1"
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"

powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %* <nul
set "RESULT=%ERRORLEVEL%"
if not "%PAUSE_ON_EXIT%"=="0" pause
exit /b %RESULT%
