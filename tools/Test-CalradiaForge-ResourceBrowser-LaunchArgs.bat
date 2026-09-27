@echo off
setlocal
set "SCRIPT=%~dp0Test-CalradiaForge-ResourceBrowser-LaunchArgs.ps1"
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%"
set "RESULT=%ERRORLEVEL%"
if not "%PAUSE_ON_EXIT%"=="0" pause
exit /b %RESULT%
