@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT=%~dp0Inspect-CalradiaForge-Tpac.ps1"
set "PS_ARGS="
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do (
    if /I "%%~A"=="--validate-only" set "PS_ARGS=!PS_ARGS! -ValidateOnly"
    if /I "%%~A"=="--structural-only" set "PS_ARGS=!PS_ARGS! -StructuralOnly"
    if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
    if /I "%%~A"=="--installed" set "PS_ARGS=!PS_ARGS! -InstalledModule"
)

where pwsh.exe >nul 2>nul
if errorlevel 1 goto use_windows_powershell
pwsh.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %PS_ARGS% <nul
goto store_result

:use_windows_powershell
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%SCRIPT%" %PS_ARGS% <nul

:store_result
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo TPAC inspection preflight failed with code %RESULT%.
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
