@echo off
setlocal EnableExtensions
set "PROJECT=%~dp0BannerlordImportAutomation.csproj"
set "APP=%~dp0bin\Release\net48\BannerlordImportAutomation.exe"

dotnet build "%PROJECT%" --configuration Release --nologo --verbosity quiet
if errorlevel 1 exit /b %ERRORLEVEL%
if not exist "%APP%" (
    echo ERROR: The import automation app was not produced.
    exit /b 2
)

"%APP%" %*
exit /b %ERRORLEVEL%
