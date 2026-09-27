@echo off
setlocal DisableDelayedExpansion
set "SCRIPT=%~dp0Publish-CalradiaForge-ImageGenSource.ps1"
set "PAUSE_ON_EXIT=1"
set "STAGE_ARGS="
set "RESULT=0"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--no-pause" (
    set "PAUSE_ON_EXIT=0"
    shift
    goto parse_args
)
if /I "%~1"=="--stage-root" (
    if "%~2"=="" goto missing_value
    set "STAGE_ARGS=-StageRoot "%~2""
    shift
    shift
    goto parse_args
)
echo ERROR: Unknown option: %~1
goto usage_error

:args_done
if not defined STAGE_ARGS (
    echo ERROR: --stage-root is required.
    goto usage_error
)
where powershell.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Windows PowerShell was not found on PATH.
    set "RESULT=9009"
    goto finish
)
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %STAGE_ARGS%
set "RESULT=%ERRORLEVEL%"
goto finish

:missing_value
echo ERROR: --stage-root requires a path.
goto usage_error

:usage_error
set "RESULT=2"

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
