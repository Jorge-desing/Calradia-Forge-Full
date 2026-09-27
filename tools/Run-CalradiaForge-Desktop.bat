@echo off
setlocal
set "SCRIPT_DIRECTORY=%~dp0"
set "DESKTOP_ASSEMBLY=%SCRIPT_DIRECTORY%CalradiaForge.Desktop.dll"

if not exist "%DESKTOP_ASSEMBLY%" (
    echo Calradia Forge Desktop files were not found beside this launcher.
    echo Extract the complete Desktop folder before launching it.
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET 8 Windows Desktop Runtime is required to run Calradia Forge Desktop.
    echo Install it from https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

dotnet "%DESKTOP_ASSEMBLY%" %*
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" (
    echo Calradia Forge Desktop exited with code %RESULT%.
    pause
)
exit /b %RESULT%
