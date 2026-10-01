@echo off
setlocal EnableExtensions
set "TARGET=CalradiaForge.sln"
if "%~1"=="" goto run
if /I not "%~1"=="--portable" (
    echo ERROR: Expected --portable or no argument.
    exit /b 2
)
if not "%~2"=="" exit /b 2
set "TARGET=CalradiaForge.Portable.slnf"
:run
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify_stateless_behavior.ps1" -BuildTarget "%TARGET%"
exit /b %ERRORLEVEL%
