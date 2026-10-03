@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PROJECT=%ROOT%\tests\CalradiaForge.HookGameFixture\CalradiaForge.HookGameFixture.csproj"
set "SOURCE_MODULE=%ROOT%\tests\CalradiaForge.HookGameFixture"
set "GAME_PATH=%BANNERLORD_GAME_PATH%"
if not defined GAME_PATH set "GAME_PATH=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
set "GAME_BIN=%GAME_PATH%\bin\Win64_Shipping_Client"
set "OUTPUT=%ROOT%\artifacts\hook-game-fixture-%RANDOM%-%RANDOM%"
set "BUILD_OUTPUT=%OUTPUT%\build"
set "STAGED_MODULE=%OUTPUT%\Modules\CalradiaForgeHookFixture"
set "RESULT=0"

if not exist "%GAME_BIN%\TaleWorlds.MountAndBlade.dll" (
    echo ERROR: Bannerlord references were not found at "%GAME_BIN%".
    echo Set BANNERLORD_GAME_PATH to the installed game root and rerun this BAT.
    set "RESULT=2"
    goto finish
)
if exist "%OUTPUT%" (
    echo ERROR: Refusing to reuse the existing fixture output directory "%OUTPUT%".
    set "RESULT=2"
    goto finish
)
mkdir "%BUILD_OUTPUT%" >nul 2>nul
if errorlevel 1 (
    echo ERROR: Could not create the isolated fixture build output.
    set "RESULT=2"
    goto finish
)

echo [HookGameFixture] Building a test-only net472 Bannerlord module through this batch launcher...
dotnet.exe build "%PROJECT%" --configuration Release --framework net472 --output "%BUILD_OUTPUT%" --nologo --verbosity minimal "-p:GamePath=%GAME_PATH%"
if errorlevel 1 (
    echo ERROR: The hook game fixture did not compile.
    set "RESULT=1"
    goto finish
)
if not exist "%BUILD_OUTPUT%\CalradiaForge.HookGameFixture.dll" (
    echo ERROR: The expected fixture library was not produced.
    set "RESULT=2"
    goto finish
)
if exist "%BUILD_OUTPUT%\CalradiaForge.HookGameFixture.exe" (
    echo ERROR: The fixture must be a library, not an executable.
    set "RESULT=2"
    goto finish
)

mkdir "%STAGED_MODULE%\bin\Win64_Shipping_Client" >nul 2>nul
if errorlevel 1 (
    echo ERROR: Could not create the isolated module staging folder.
    set "RESULT=2"
    goto finish
)
copy /y "%SOURCE_MODULE%\SubModule.xml" "%STAGED_MODULE%\SubModule.xml" >nul
if errorlevel 1 (
    echo ERROR: Could not stage SubModule.xml.
    set "RESULT=2"
    goto finish
)
copy /y "%BUILD_OUTPUT%\CalradiaForge.HookGameFixture.dll" "%STAGED_MODULE%\bin\Win64_Shipping_Client\CalradiaForge.HookGameFixture.dll" >nul
if errorlevel 1 (
    echo ERROR: Could not stage the fixture library.
    set "RESULT=2"
    goto finish
)

findstr /L /C:CalradiaForgeHookFixture "%STAGED_MODULE%\SubModule.xml" >nul
if errorlevel 1 (
    echo ERROR: The staged manifest does not declare the fixture module ID.
    set "RESULT=2"
    goto finish
)
findstr /L /C:CalradiaForge.HookGameFixture.HookGameFixtureSubModule "%STAGED_MODULE%\SubModule.xml" >nul
if errorlevel 1 (
    echo ERROR: The staged manifest does not point to the compiled SubModule class.
    set "RESULT=2"
    goto finish
)
for /f %%C in ('dir /b /s /a-d "%STAGED_MODULE%" ^| find /c /v ""') do set "STAGED_FILE_COUNT=%%C"
if not "%STAGED_FILE_COUNT%"=="2" (
    echo ERROR: The isolated module staging folder must contain only SubModule.xml and the fixture DLL.
    set "RESULT=2"
    goto finish
)
if exist "%STAGED_MODULE%\bin\Win64_Shipping_Client\CalradiaForge.HookGameFixture.exe" (
    echo ERROR: An executable must not be staged with the fixture module.
    set "RESULT=2"
    goto finish
)

echo PASS: isolated module staged at "%STAGED_MODULE%".
echo This BAT did not install the fixture or launch Bannerlord.

:finish
exit /b %RESULT%
