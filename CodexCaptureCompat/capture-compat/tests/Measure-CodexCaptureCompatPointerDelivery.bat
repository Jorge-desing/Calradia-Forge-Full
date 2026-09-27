@echo off
setlocal EnableExtensions

if /i not "%~1"=="--measure" (
    echo Usage: %~nx0 --measure [duration-seconds]
    echo Measurement mode must be requested explicitly. Duration defaults to 30 seconds and is limited to 5-90 seconds.
    exit /b 2
)
if not "%~3"=="" (
    echo Usage: %~nx0 --measure [duration-seconds]
    exit /b 2
)

set "REQUESTED_SECONDS=%~2"
if not defined REQUESTED_SECONDS set "REQUESTED_SECONDS=30"
set "DURATION="
for /f "delims=" %%S in ('powershell.exe -NoProfile -NonInteractive -Command "$n=0; if ([int]::TryParse($env:REQUESTED_SECONDS,[ref]$n) -and $n -ge 5 -and $n -le 90) { $n }"') do set "DURATION=%%S"
if not defined DURATION (
    echo Duration must be a whole number from 5 through 90 seconds.
    exit /b 2
)

set "BUILD=%~dp0..\build.bat"
set "FIXTURE=%~dp0..\build\capture_test_window.exe"
if not exist "%BUILD%" (
    echo Missing existing fixture build launcher: "%BUILD%"
    exit /b 3
)

echo Building and validating through the existing capture-compat build BAT.
call "%BUILD%"
if errorlevel 1 (
    echo Fixture build or its BAT-driven validation failed; no measurement was started.
    exit /b 4
)
if not exist "%FIXTURE%" (
    echo Expected isolated fixture is missing after the build: "%FIXTURE%"
    exit /b 4
)

set "OUTDIR=%TEMP%\CodexCaptureCompat\pointer-delivery"
if not exist "%OUTDIR%" mkdir "%OUTDIR%"
if errorlevel 1 (
    echo Could not create the temporary measurement output directory.
    exit /b 5
)
set "STAMP="
for /f "delims=" %%G in ('powershell.exe -NoProfile -NonInteractive -Command "[guid]::NewGuid().ToString([char]78)"') do set "STAMP=%%G"
if not defined STAMP (
    echo Could not generate a unique output name.
    exit /b 5
)
set "CSV=%OUTDIR%\pointer-%STAMP%.csv"
if exist "%CSV%" (
    echo Unique output path already exists; refusing to overwrite it.
    exit /b 5
)

echo.
echo This diagnostic measures message delivery cadence only; it is not hardware-to-screen latency.
echo Move the physical mouse only across the blank client area of the fixture window.
echo If measuring window movement, drag only that fixture's title bar and release it before the timer ends.
echo Do not use input automation during this sample. The launcher will not inject or reposition input.
echo The isolated fixture closes after %DURATION% seconds. CSV output: "%CSV%"
echo.
start "" /wait "%FIXTURE%" --measure-pointer "%DURATION%" "%CSV%"
set "FIXTURE_RESULT=%ERRORLEVEL%"
if not "%FIXTURE_RESULT%"=="0" (
    echo Fixture measurement failed with exit code %FIXTURE_RESULT%.
    exit /b %FIXTURE_RESULT%
)
if not exist "%CSV%" (
    echo Fixture exited successfully but did not create the expected CSV.
    exit /b 6
)
for %%A in ("%CSV%") do if %%~zA LEQ 0 (
    echo Fixture created an empty CSV; no measurement can be trusted.
    exit /b 6
)
echo Measurement complete. Review the event counts and sample counts before comparing runs.
echo Percentiles describe WM_MOUSEMOVE / WM_MOVING inter-arrival cadence and approximate WM_MOUSEMOVE queue age only.
echo "%CSV%"
exit /b 0
