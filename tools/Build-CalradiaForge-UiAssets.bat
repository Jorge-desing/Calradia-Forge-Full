@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT=%~dp0Build-CalradiaForge-UiAssets.ps1"
set "PS_ARGS="
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do (
    if /I "%%~A"=="--dry-run" set "PS_ARGS=!PS_ARGS! -DryRun"
    if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
)

if not defined CALRADIAFORGE_POWERSHELL set "CALRADIAFORGE_POWERSHELL=powershell.exe"
"%CALRADIAFORGE_POWERSHELL%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %PS_ARGS%
set "RESULT=%ERRORLEVEL%"
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
