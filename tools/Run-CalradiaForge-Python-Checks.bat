@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0.."
set "PYTHON=%ROOT%\.venv\Scripts\python.exe"
set "MODE=--ci"
set "RUN_AGENT_TESTS=0"
set "PAUSE_ON_EXIT=1"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--ci" set "MODE=--ci"
if /I "%~1"=="--ledger" set "MODE=--ledger"
if /I "%~1"=="--agents" set "RUN_AGENT_TESTS=1"
if /I "%~1"=="--no-pause" set "PAUSE_ON_EXIT=0"
if /I not "%~1"=="--ci" if /I not "%~1"=="--ledger" if /I not "%~1"=="--agents" if /I not "%~1"=="--no-pause" (
    echo ERROR: Unknown option "%~1".
    set "RESULT=2"
    goto finish
)
shift
goto parse_args

:args_done
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
set "PUSHED=1"

if /I "%MODE%"=="--ledger" goto ledger_checks

echo [Python] Verifying the isolated tool environment...
"%PYTHON%" -c "import sys, yaml, PIL, lxml, docx; print('Python', sys.version.split()[0], '| PyYAML', yaml.__version__, '| Pillow', PIL.__version__, '| lxml', lxml.__version__, '| python-docx', docx.__version__)"
if errorlevel 1 goto failed
"%PYTHON%" -m ruff --version
if errorlevel 1 goto failed

echo [Python] Running Ruff checks for maintained scripts and tests...
"%PYTHON%" -m ruff check
if errorlevel 1 goto failed

echo [Python] Running archive and asset pipeline unit suite...
call "%ROOT%\tests\CalradiaForge.AssetPipeline.Tests.bat" --no-pause
if errorlevel 1 goto failed
echo [Python] Running image pipeline unit suite...
"%PYTHON%" -m unittest tests\test_high_quality_image_pipeline.py
if errorlevel 1 goto failed
if "%RUN_AGENT_TESTS%"=="0" goto ci_success
"%PYTHON%" -c "from google.antigravity import LocalAgentConfig, types; import google.genai"
if errorlevel 1 (
    echo ERROR: The optional Antigravity profile is missing. Run tools\Setup-CalradiaForge-Python.bat --agents first.
    set "RESULT=2"
    goto finish
)
echo [Python] Running optional Antigravity agent unit suite...
"%PYTHON%" -m unittest tests\test_forge_agents.py
if errorlevel 1 goto failed
:ci_success
echo [Python] Validating decorative sprites and imported icon metadata...
call "%ROOT%\tools\Validate-CalradiaForge-DecorativeSprites.bat" --no-pause
if errorlevel 1 goto failed
"%PYTHON%" tools\validate_game_icon_assets.py --require-tpac
if errorlevel 1 goto failed
echo Calradia Forge Python checks passed.
set "RESULT=0"
goto finish

:ledger_checks
echo [Python] Running bilingual documentation and integrity audits...
"%PYTHON%" tools\Run-CalradiaForge-Ledger-Audits.py
set "RESULT=!ERRORLEVEL!"
goto finish

:failed
set "RESULT=!ERRORLEVEL!"
if "!RESULT!"=="0" set "RESULT=1"
echo ERROR: Python validation stopped with exit code !RESULT!.

:finish
if not defined RESULT set "RESULT=0"
if defined PUSHED popd
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
