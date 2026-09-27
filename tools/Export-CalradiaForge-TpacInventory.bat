@echo off
setlocal EnableExtensions
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Export-CalradiaForge-TpacInventory.ps1" %*
exit /b %ERRORLEVEL%
