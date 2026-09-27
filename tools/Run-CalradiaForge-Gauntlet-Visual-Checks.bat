@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
set "PYTHON_COMMAND="
set "RESULT=0"
set "PUSHED=0"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--no-pause" set "PAUSE_ON_EXIT=0"
shift
goto parse_args

:args_done
pushd "%ROOT%"
if errorlevel 1 (
    echo ERROR: Could not enter the repository root: "%ROOT%"
    set "RESULT=2"
    goto finish
)
set "PUSHED=1"

set "DECORATIVE_VALIDATOR=%ROOT%\tools\Validate-CalradiaForge-DecorativeSprites.py"
set "GAUNTLET_AUDITOR=%ROOT%\tools\audit_gauntlet_ui.py"
set "CORE_TESTS=%ROOT%\tools\Run-CalradiaForge-Core-Tests.bat"

if not exist "!DECORATIVE_VALIDATOR!" (
    echo ERROR: Decorative sprite validator is missing:
    echo        "!DECORATIVE_VALIDATOR!"
    set "RESULT=2"
    goto finish
)
if not exist "!GAUNTLET_AUDITOR!" (
    echo ERROR: Gauntlet UI structural auditor is missing. Add tools\audit_gauntlet_ui.py before running this suite.
    echo        Expected: "!GAUNTLET_AUDITOR!"
    set "RESULT=2"
    goto finish
)
if not exist "!CORE_TESTS!" (
    echo ERROR: Core test batch launcher is missing:
    echo        "!CORE_TESTS!"
    set "RESULT=2"
    goto finish
)

where py.exe >nul 2>nul
if not errorlevel 1 (
    set "PYTHON_COMMAND=py -3"
) else (
    where python.exe >nul 2>nul
    if errorlevel 1 (
        echo ERROR: Python 3 was not found. Install Python or add py.exe/python.exe to PATH.
        set "RESULT=9009"
        goto finish
    )
    set "PYTHON_COMMAND=python"
)

echo [Visual] Validating decorative sprite resources...
!PYTHON_COMMAND! "!DECORATIVE_VALIDATOR!"
set "RESULT=!ERRORLEVEL!"
if not "!RESULT!"=="0" goto failed

echo [Visual] Auditing Gauntlet prefab, bindings, and layout...
!PYTHON_COMMAND! "!GAUNTLET_AUDITOR!"
set "RESULT=!ERRORLEVEL!"
if not "!RESULT!"=="0" goto failed

echo [Core] Running core tests through the batch launcher...
call "!CORE_TESTS!" --no-pause
set "RESULT=!ERRORLEVEL!"
if not "!RESULT!"=="0" goto failed

echo Gauntlet visual structural checks and core tests passed.
goto finish

:failed
echo ERROR: Gauntlet visual check sequence stopped at the first failing check, exit code !RESULT!.

:finish
if "!PUSHED!"=="1" popd
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
