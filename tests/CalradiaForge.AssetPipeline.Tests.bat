@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
if /I "%~1"=="--no-pause" set "PAUSE_ON_EXIT=0"
pushd "%ROOT%"
if errorlevel 1 goto setup_failed
echo [Assets] Running bounded PNG and archive-audit fixtures via Python...
python tests\CalradiaForge.AssetPipeline.Tests.py
if errorlevel 1 goto failed
popd
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b 0
:setup_failed
echo ERROR: Could not enter the repository directory.
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b 2
:failed
set "RESULT=%ERRORLEVEL%"
popd
echo Asset pipeline test run failed with exit code %RESULT%.
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
