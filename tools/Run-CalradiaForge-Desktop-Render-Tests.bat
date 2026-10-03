@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
set "RENDER_OUTPUT="
set "RESULT=0"
rem Explicitly scope the WPF harness to non-activating off-screen test windows.
set "CALRADIA_FORGE_RENDER_NO_ACTIVATE=1"

:parseArgs
if "%~1"=="" goto argsDone
if /I "%~1"=="--no-pause" (
    set "PAUSE_ON_EXIT=0"
    shift
    goto parseArgs
)
if /I "%~1"=="--output" goto parseOutput
set "CURRENT_ARGUMENT=%~1"
setlocal EnableDelayedExpansion
if "!CURRENT_ARGUMENT:~0,2!"=="--" (
    endlocal
    echo ERROR: Unknown option. Use --no-pause, --output, or one positional JSON path.
    set "RESULT=2"
    goto finish
)
endlocal
if defined RENDER_OUTPUT (
    echo ERROR: Specify only one render output path.
    set "RESULT=2"
    goto finish
)
set "RENDER_OUTPUT=%~1"
shift
goto parseArgs

:parseOutput
shift
if "%~1"=="" (
    echo ERROR: --output requires a JSON output path.
    set "RESULT=2"
    goto finish
)
if defined RENDER_OUTPUT (
    echo ERROR: Specify only one render output path.
    set "RESULT=2"
    goto finish
)
set "RENDER_OUTPUT=%~1"
shift
goto parseArgs

:argsDone
set "TEST_ASSEMBLY=%ROOT%\tests\CalradiaForge.Desktop.RenderTests\bin\Release\net8.0-windows\CalradiaForge.Desktop.RenderTests.dll"

if not exist "%TEST_ASSEMBLY%" (
    echo ERROR: WPF render test output is missing. Build the solution first.
    set "RESULT=2"
    goto finish
)
echo [Desktop Render] Running WPF render and resource tests via this batch launcher...
if defined RENDER_OUTPUT (
    dotnet "%TEST_ASSEMBLY%" "%RENDER_OUTPUT%"
) else (
    dotnet "%TEST_ASSEMBLY%"
)
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: Desktop render suite failed with exit code %RESULT%.

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
