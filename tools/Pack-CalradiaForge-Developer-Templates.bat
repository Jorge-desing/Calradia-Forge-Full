@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
set "VENV_PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "PAUSE_ON_EXIT=1"
set "RESULT=0"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--no-pause" set "PAUSE_ON_EXIT=0"
if /I not "%~1"=="--no-pause" (
    echo ERROR: Unknown option "%~1".
    set "RESULT=2"
    goto finish
)
shift
goto parse_args

:args_done
if not exist "%VENV_PYTHON%" (
    echo ERROR: Repository Python environment is missing: "%VENV_PYTHON%"
    echo        Run tools\Setup-CalradiaForge-Python.bat first.
    set "RESULT=2"
    goto finish
)
"%VENV_PYTHON%" -c "import sys; raise SystemExit(0 if sys.version_info[:2] == (3, 12) else 1)" >nul 2>nul
if errorlevel 1 (
    echo ERROR: Repository .venv must use Python 3.12.
    set "RESULT=2"
    goto finish
)

"%VENV_PYTHON%" "%ROOT%\tools\run_developer_onboarding_smoke.py" pack --repo "%ROOT%"
set "RESULT=%ERRORLEVEL%"

:finish
if not "%PAUSE_ON_EXIT%"=="0" pause
exit /b %RESULT%
