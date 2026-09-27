@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT=%~dp0Prepare-CalradiaForge-ResourceBrowser.ps1"
set "PS_ARGS="
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do (
    if /I "%%~A"=="--dry-run" set "PS_ARGS=!PS_ARGS! -DryRun"
    if /I "%%~A"=="--launch-editor" set "PS_ARGS=!PS_ARGS! -LaunchEditor"
    if /I "%%~A"=="--collect-tpac" set "PS_ARGS=!PS_ARGS! -CollectTpac"
    if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %PS_ARGS%
set "RESULT=%ERRORLEVEL%"
if not "%PAUSE_ON_EXIT%"=="0" pause
exit /b %RESULT%
