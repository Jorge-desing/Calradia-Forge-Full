@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT=%~dp0Build-CalradiaForge-UiAssets.ps1"
set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "PS_ARGS="
set "PAUSE_ON_EXIT=1"
set "PREFAB_ONLY=0"
for %%A in (%*) do (
    if /I "%%~A"=="--dry-run" set "PS_ARGS=!PS_ARGS! -DryRun"
    if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
    if /I "%%~A"=="--prefab-only" set "PREFAB_ONLY=1"
)

if "%PREFAB_ONLY%"=="1" (
    if not exist "%PYTHON%" (
        echo ERROR: Calradia Forge Python environment is missing. Run tools\Setup-CalradiaForge-Python.bat first.
        set "RESULT=2"
        goto finish
    )
    pushd "%ROOT%"
    if errorlevel 1 (
        echo ERROR: Could not enter the repository root.
        set "RESULT=2"
        goto finish
    )
    "%PYTHON%" tools\generate_assets.py --prefab-only
    set "RESULT=!ERRORLEVEL!"
    popd
) else (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %PS_ARGS%
    set "RESULT=!ERRORLEVEL!"
)

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b !RESULT!
