@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "SKILL_PATH=%~1"

if "%SKILL_PATH%"=="" goto usage
if not "%~2"=="" goto usage

if not exist "%PYTHON%" (
    echo ERROR: The repository virtual environment is missing: "%PYTHON%"
    echo Create it with: py -3.12 -m venv .venv
    echo Install development dependencies with: .venv\Scripts\python.exe -m pip install -r requirements-dev.txt
    exit /b 2
)

if defined CODEX_HOME (
    set "VALIDATOR=%CODEX_HOME%\skills\.system\skill-creator\scripts\quick_validate.py"
) else (
    set "VALIDATOR=%USERPROFILE%\.codex\skills\.system\skill-creator\scripts\quick_validate.py"
)

if not exist "%VALIDATOR%" (
    echo ERROR: Codex quick_validate.py was not found:
    echo        "%VALIDATOR%"
    exit /b 2
)

pushd "%ROOT%"
if errorlevel 1 (
    echo ERROR: Could not enter the repository root: "%ROOT%"
    exit /b 2
)

"%PYTHON%" "%VALIDATOR%" "%SKILL_PATH%"
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%

:usage
echo Usage: tools\Validate-CalradiaForge-Skills.bat ^<skill-directory^>
echo Example: tools\Validate-CalradiaForge-Skills.bat .agents\skills\calradia-forge-desktop
exit /b 2
