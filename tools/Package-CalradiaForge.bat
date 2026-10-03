@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
pushd "%ROOT%"
if errorlevel 1 exit /b 2
rem Keep the canonical pipeline; this BAT is the development entry point.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\tools\package.ps1" %*
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
