@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT=%~dp0Test-CalradiaForge-DecorativeTextureDeterminism.py"
set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"

if not exist "%PYTHON%" (
    echo ERROR: Calradia Forge Python environment is missing. Run tools\Setup-CalradiaForge-Python.bat first.
    set "RESULT=2"
) else (
    "%PYTHON%" "%SCRIPT%"
    set "RESULT=!ERRORLEVEL!"
)
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b !RESULT!
