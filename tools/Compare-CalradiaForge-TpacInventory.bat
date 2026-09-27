@echo off
setlocal EnableExtensions
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Compare-CalradiaForge-TpacInventory.ps1" %*
exit /b %ERRORLEVEL%
