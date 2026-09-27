@echo off
setlocal EnableExtensions
rem Optional interactive-desktop smoke test; not part of build.bat or the native regression suite.
rem Exit code 3 means foreground acquisition was unavailable, and no keys were sent.
set "RUNNER=%~dp0Test-CodexCaptureCompatKeyboard.ps1"
set "FIXTURE=%~dp0..\build\capture_test_window.exe"
if not exist "%RUNNER%" (
    echo Missing keyboard regression runner: "%RUNNER%"
    exit /b 1
)
if not exist "%FIXTURE%" (
    echo Missing isolated fixture: "%FIXTURE%"
    echo Run build.bat first to build the isolated fixture.
    exit /b 1
)
set "MODE_ARGS="
if /i "%~1"=="--modifier-probe" (
    set "MODE_ARGS=-IncludeModifierProbe"
) else if not "%~1"=="" (
    echo Usage: Test-CodexCaptureCompatKeyboard.bat [--modifier-probe]
    exit /b 2
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%RUNNER%" -FixturePath "%FIXTURE%" %MODE_ARGS%
exit /b %ERRORLEVEL%
