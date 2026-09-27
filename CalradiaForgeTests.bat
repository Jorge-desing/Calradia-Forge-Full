@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"

call "%ROOT%tools\Run-CalradiaForge-Tests.bat" %*
exit /b %ERRORLEVEL%
