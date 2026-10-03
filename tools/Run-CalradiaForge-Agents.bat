@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "SCRIPT=%ROOT%\tools\run_forge_agents.py"

if not exist "%PYTHON%" (
    echo ERROR: The repository Python environment is missing: "%PYTHON%"
    echo        Create it with: tools\Setup-CalradiaForge-Python.bat --no-pause
    exit /b 2
)

if not exist "%SCRIPT%" (
    echo ERROR: The Forge agent CLI was not found: "%SCRIPT%"
    exit /b 2
)

pushd "%ROOT%"
if errorlevel 1 (
    echo ERROR: Could not enter the repository root: "%ROOT%"
    exit /b 2
)

"%PYTHON%" "%SCRIPT%" %*
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
