@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
set "VENV=%ROOT%\.venv"
set "VENV_PYTHON=%VENV%\Scripts\python.exe"
set "INSTALL_AGENTS=0"
set "PAUSE_ON_EXIT=1"
set "RESULT=0"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--agents" set "INSTALL_AGENTS=1"
if /I "%~1"=="--no-pause" set "PAUSE_ON_EXIT=0"
if /I not "%~1"=="--agents" if /I not "%~1"=="--no-pause" (
    echo ERROR: Unknown option "%~1".
    set "RESULT=2"
    goto finish
)
shift
goto parse_args

:args_done
if not exist "%ROOT%\requirements-dev.txt" (
    echo ERROR: Python tool requirements were not found:
    echo        "%ROOT%\requirements-dev.txt"
    set "RESULT=2"
    goto finish
)
if "%INSTALL_AGENTS%"=="1" if not exist "%ROOT%\agents\requirements.txt" (
    echo ERROR: Optional agent requirements were not found:
    echo        "%ROOT%\agents\requirements.txt"
    set "RESULT=2"
    goto finish
)

if exist "%VENV_PYTHON%" goto verify_venv

where py.exe >nul 2>nul
if not errorlevel 1 (
    py -3.12 -m venv "%VENV%"
    if not errorlevel 1 goto verify_venv
    echo Python Launcher did not create the environment with Python 3.12; checking python.exe on PATH.
)

where python.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Python 3.12 was not found. Install Python 3.12, then run this setup BAT again.
    set "RESULT=9009"
    goto finish
)
python.exe -c "import sys; raise SystemExit(0 if sys.version_info[:2] == (3, 12) else 1)" >nul 2>nul
if errorlevel 1 (
    echo ERROR: python.exe on PATH is not Python 3.12. Install Python 3.12 or make it available through py.exe.
    set "RESULT=2"
    goto finish
)
python.exe -m venv "%VENV%"
if errorlevel 1 (
    echo ERROR: Could not create the repository-local Python environment.
    set "RESULT=1"
    goto finish
)

:verify_venv
if not exist "%VENV_PYTHON%" (
    echo ERROR: The Python environment is incomplete: "%VENV_PYTHON%" was not created.
    set "RESULT=1"
    goto finish
)
"%VENV_PYTHON%" -c "import sys; raise SystemExit(0 if sys.version_info[:2] == (3, 12) else 1)" >nul 2>nul
if errorlevel 1 (
    echo ERROR: The repository .venv is not using Python 3.12.
    echo        Preserve the environment, then recreate it with this setup BAT after installing Python 3.12.
    set "RESULT=2"
    goto finish
)

echo [Python] Installing requirements-dev.txt into the repository .venv...
"%VENV_PYTHON%" -m pip install --disable-pip-version-check --timeout 120 --retries 5 --upgrade -r "%ROOT%\requirements-dev.txt"
if errorlevel 1 (
    set "RESULT=1"
    goto failed
)

echo [Python] Verifying utility imports in the repository .venv...
"%VENV_PYTHON%" -c "import sys, yaml, PIL, lxml, docx, ruff; from importlib.metadata import version; print('Python', sys.version.split()[0], '| PyYAML', version('PyYAML'), '| Pillow', version('Pillow'), '| lxml', version('lxml'), '| python-docx', version('python-docx'), '| Ruff', version('ruff'))"
if errorlevel 1 (
    echo ERROR: A required development utility could not be imported from the repository .venv.
    set "RESULT=1"
    goto failed
)

if not "%INSTALL_AGENTS%"=="1" goto success
echo [Python] Installing agents\requirements.txt into the repository .venv...
"%VENV_PYTHON%" -m pip install --disable-pip-version-check --timeout 120 --retries 5 --upgrade -r "%ROOT%\agents\requirements.txt"
if errorlevel 1 (
    set "RESULT=1"
    goto failed
)
echo [Python] Verifying optional agent imports in the repository .venv...
"%VENV_PYTHON%" -c "from google.antigravity import LocalAgentConfig, types; import google.genai, google.protobuf, pydantic, absl; from importlib.metadata import version; print('google-antigravity', version('google-antigravity'), '| google-genai', version('google-genai'), '| protobuf', version('protobuf'), '| pydantic', version('pydantic'), '| absl-py', version('absl-py'))"
if errorlevel 1 (
    echo ERROR: An optional agent dependency could not be imported from the repository .venv.
    set "RESULT=1"
    goto failed
)

:success
echo Calradia Forge Python environment is ready at "%VENV%".
goto finish

:failed
echo ERROR: Python environment setup failed with exit code %RESULT%.

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
