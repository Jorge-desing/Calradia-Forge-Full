@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT=%~dp0deploy_to_game.ps1"
set "PS_ARGS="
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do (
    if /I "%%~A"=="--dry-run" set "PS_ARGS=!PS_ARGS! -DryRun"
    if /I "%%~A"=="--launch-modkit" set "PS_ARGS=!PS_ARGS! -LaunchModKit"
    if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %PS_ARGS%
set "RESULT=%ERRORLEVEL%"
if not "%PAUSE_ON_EXIT%"=="0" pause
exit /b %RESULT%
