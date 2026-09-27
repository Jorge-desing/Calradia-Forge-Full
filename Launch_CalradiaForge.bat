@echo off
setlocal
set "FORGE_LAUNCHER=%~dp0artifacts\Desktop\Run-CalradiaForge-Desktop.bat"

echo.
echo ========================================================
echo   Calradia Forge - Secure Desktop Terminal
echo ========================================================
echo.
echo Launching framework-dependent application...
echo Ensure you have the .NET 8.0 Desktop Runtime installed.
echo.

if not exist "%FORGE_LAUNCHER%" (
  echo Extract the latest Desktop release into the artifacts folder first.
  pause
  exit /b 1
)
call "%FORGE_LAUNCHER%" %*

exit /b %ERRORLEVEL%
