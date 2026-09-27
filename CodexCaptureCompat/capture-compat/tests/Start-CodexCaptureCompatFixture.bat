@echo off
setlocal EnableExtensions
set "FIXTURE=%~dp0..\build\capture_test_window.exe"
if not exist "%FIXTURE%" (
    echo Missing isolated fixture: "%FIXTURE%"
    echo Run build.bat first to build the current fixture from source.
    exit /b 1
)
start "Codex Computer Use fixture" "%FIXTURE%"
if errorlevel 1 (
    echo Failed to launch the isolated Computer Use fixture.
    exit /b 1
)
echo Launched only the isolated fixture. It closes itself after ten minutes.
exit /b 0
