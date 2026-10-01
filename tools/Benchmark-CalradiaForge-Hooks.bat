@echo off
setlocal EnableExtensions
call "%~dp0..\tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat" --benchmark --no-pause
exit /b %ERRORLEVEL%
