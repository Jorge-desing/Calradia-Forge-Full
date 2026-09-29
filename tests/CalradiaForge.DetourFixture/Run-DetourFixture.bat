@echo off
setlocal EnableExtensions
set "PROJECT=%~dp0CalradiaForge.DetourFixture.csproj"
set "OUTPUT_DIR=%TEMP%\CalradiaForge.DetourFixture-%RANDOM%-%RANDOM%"
if exist "%OUTPUT_DIR%" (
    set "RESULT=2"
    echo ERROR: Refusing to reuse an existing fixture output directory: "%OUTPUT_DIR%"
    goto finish
)
mkdir "%OUTPUT_DIR%" >nul 2>nul
if errorlevel 1 (
    echo ERROR: Could not create an isolated fixture output directory: "%OUTPUT_DIR%"
    set "RESULT=2"
    goto finish
)
set "OUTPUT_CREATED=1"
set "FIXTURE=%OUTPUT_DIR%\CalradiaForge.DetourFixture.exe"
set "PAUSE_ON_EXIT=1"
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"

where dotnet.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: .NET SDK is required to build the x64 detour fixture.
    set "RESULT=2"
    goto finish
)

echo [DetourFixture] Building disposable net472 x64 process...
dotnet.exe build "%PROJECT%" --configuration Release --framework net472 --nologo --verbosity minimal --output "%OUTPUT_DIR%"
if errorlevel 1 (
    set "RESULT=%ERRORLEVEL%"
    goto finish
)

if not exist "%FIXTURE%" (
    echo ERROR: Expected fixture executable was not produced in isolated output: "%FIXTURE%"
    set "RESULT=2"
    goto finish
)

echo [DetourFixture] Running serial native detour smoke test in isolated process...
"%FIXTURE%"
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: Detour fixture failed with exit code %RESULT%.

:finish
if defined OUTPUT_CREATED (
    del /q "%OUTPUT_DIR%\CalradiaForge.DetourFixture.exe" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.exe.config" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.pdb" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.deps.json" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.runtimeconfig.json" "%OUTPUT_DIR%\CalradiaForge.Sdk.dll" "%OUTPUT_DIR%\CalradiaForge.Sdk.pdb" "%OUTPUT_DIR%\CalradiaForge.Sdk.xml" >nul 2>nul
    rmdir "%OUTPUT_DIR%" >nul 2>nul
)
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
