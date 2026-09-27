@echo off
setlocal
set "SCRIPT_DIRECTORY=%~dp0"

echo Running the short Calradia Forge build and test suite.
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIRECTORY%build.ps1" %*
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" (
    echo The short test suite exited with code %RESULT%.
)
exit /b %RESULT%
