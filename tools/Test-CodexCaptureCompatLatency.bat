@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%..\CodexCaptureCompat\capture-compat"
set "PROBE=%PROJECT_DIR%\dist\compat_probe.exe"
if /i "%~1"=="--self-test" goto :SelfTest
if not exist "%PROBE%" (
    echo Missing probe: "%PROBE%"
    exit /b 1
)

set "ITERATIONS=5"
if not "%~1"=="" set "ITERATIONS=%~1"
for /f "delims=0123456789" %%A in ("%ITERATIONS%") do (
    echo Iterations must be a positive integer.
    exit /b 2
)
if %ITERATIONS% LSS 3 (
    echo Use at least 3 samples for a stable median.
    exit /b 2
)
if %ITERATIONS% GTR 20 (
    echo Use no more than 20 samples per run.
    exit /b 2
)

echo Running %ITERATIONS% isolated WGC captures. Each test captures only its own fixture window.
for /L %%I in (1,1,%ITERATIONS%) do (
    set "OUTPUT=%TEMP%\codex-capture-compat-!RANDOM!-!RANDOM!.log"
    "%PROBE%" --expect-shim --capture --expect-deferred > "!OUTPUT!" 2>&1
    if errorlevel 1 (
        echo Capture probe failed on sample %%I/%ITERATIONS%.
        type "!OUTPUT!"
        del /q "!OUTPUT!" >nul 2>&1
        exit /b 1
    )
    set "TIMING_LINE="
    for /f "tokens=*" %%T in ('findstr /c:"start_capture_to_first_frame_us=" "!OUTPUT!"') do set "TIMING_LINE=%%T"
    if not defined TIMING_LINE (
        echo Probe binary has no StartCapture timing. Rebuild with Build-Test-CodexCaptureCompat.bat.
        type "!OUTPUT!"
        del /q "!OUTPUT!" >nul 2>&1
        exit /b 1
    )
    set "CALL_MEASUREMENT="
    set "CALL_SAMPLE="
    for /f "tokens=3" %%T in ("!TIMING_LINE!") do set "CALL_MEASUREMENT=%%T"
    for /f "tokens=2 delims==" %%T in ("!CALL_MEASUREMENT!") do set "CALL_SAMPLE=%%T"
    for /f "delims=0123456789" %%A in ("!CALL_SAMPLE!") do (
        echo Probe returned an invalid StartCapture call duration on sample %%I/%ITERATIONS%.
        type "!OUTPUT!"
        del /q "!OUTPUT!" >nul 2>&1
        exit /b 1
    )
    set "MEASUREMENT="
    set "SAMPLE="
    for /f "tokens=4" %%T in ("!TIMING_LINE!") do set "MEASUREMENT=%%T"
    for /f "tokens=2 delims==" %%T in ("!MEASUREMENT!") do set "SAMPLE=%%T"
    for /f "delims=0123456789" %%A in ("!SAMPLE!") do (
        echo Probe returned a non-numeric or unavailable first-frame timing on sample %%I/%ITERATIONS%.
        type "!OUTPUT!"
        del /q "!OUTPUT!" >nul 2>&1
        exit /b 1
    )
    if not defined CALL_SAMPLE (
        echo Probe returned an empty StartCapture call duration on sample %%I/%ITERATIONS%.
        type "!OUTPUT!"
        del /q "!OUTPUT!" >nul 2>&1
        exit /b 1
    )
    if not defined SAMPLE (
        echo Probe returned an empty first-frame timing on sample %%I/%ITERATIONS%.
        type "!OUTPUT!"
        del /q "!OUTPUT!" >nul 2>&1
        exit /b 1
    )
    set "SAMPLE_%%I=!SAMPLE!"
    set "CALL_SAMPLE_%%I=!CALL_SAMPLE!"
    echo Sample %%I: StartCapture=!CALL_SAMPLE! us; start-to-first-frame=!SAMPLE! us
    del /q "!OUTPUT!" >nul 2>&1
)

call :SortSamples
set "START_TO_FRAME_MEDIAN=!MEDIAN!"
for /L %%I in (1,1,%ITERATIONS%) do set "SAMPLE_%%I=!CALL_SAMPLE_%%I!"
call :SortSamples
echo STARTCAPTURE_CALL_MEDIAN_US=!MEDIAN!
echo STARTCAPTURE_TO_FIRST_FRAME_MEDIAN_US=!START_TO_FRAME_MEDIAN!
exit /b 0

:SortSamples
rem Sort the bounded sample set so cmd.exe can report a numeric median without PowerShell.
for /L %%P in (1,1,%ITERATIONS%) do (
    set /a NEXT=%%P+1
    for /L %%Q in (!NEXT!,1,%ITERATIONS%) do call :CompareSamples %%P %%Q
)
set /a IS_ODD=ITERATIONS-ITERATIONS/2*2
if !IS_ODD! EQU 1 goto :MedianOdd
set /a LOWER=ITERATIONS/2
set /a UPPER=LOWER+1
for %%M in (!LOWER!) do set "LOWER_SAMPLE=!SAMPLE_%%M!"
for %%M in (!UPPER!) do set "UPPER_SAMPLE=!SAMPLE_%%M!"
set /a MEDIAN=LOWER_SAMPLE+UPPER_SAMPLE
set /a MEDIAN=MEDIAN/2
goto :SortComplete

:MedianOdd
set /a MIDDLE=ITERATIONS/2+1
for %%M in (!MIDDLE!) do set "MEDIAN=!SAMPLE_%%M!"

:SortComplete
exit /b 0

:SelfTest
set "ITERATIONS=5"
set "SAMPLE_1=500"
set "SAMPLE_2=100"
set "SAMPLE_3=400"
set "SAMPLE_4=300"
set "SAMPLE_5=200"
call :SortSamples
if not "!MEDIAN!"=="300" (
    echo FAIL: odd-sample median expected 300, got !MEDIAN!.
    exit /b 1
)
set "ITERATIONS=4"
set "SAMPLE_1=40"
set "SAMPLE_2=10"
set "SAMPLE_3=30"
set "SAMPLE_4=20"
call :SortSamples
if not "!MEDIAN!"=="25" (
    echo FAIL: even-sample median expected 25, got !MEDIAN!.
    exit /b 1
)
echo PASS: batch median handles odd and even sample counts.
set "TIMING_LINE=frame_events=1 callback_thread=20 start_capture_call_us=310 start_capture_to_first_frame_us=12345 frame_wait_after_start_return_us=12034"
for /f "tokens=3" %%T in ("!TIMING_LINE!") do set "CALL_MEASUREMENT=%%T"
for /f "tokens=2 delims==" %%T in ("!CALL_MEASUREMENT!") do set "CALL_SAMPLE=%%T"
if not "!CALL_SAMPLE!"=="310" (
    echo FAIL: StartCapture timing parse expected 310, got !CALL_SAMPLE!.
    exit /b 1
)
set "MEASUREMENT="
set "SAMPLE="
for /f "tokens=4" %%T in ("!TIMING_LINE!") do set "MEASUREMENT=%%T"
for /f "tokens=2 delims==" %%T in ("!MEASUREMENT!") do set "SAMPLE=%%T"
for /f "delims=0123456789" %%A in ("!SAMPLE!") do set "SAMPLE="
if not "!SAMPLE!"=="12345" (
    echo FAIL: first-frame timing parse expected 12345, got !SAMPLE!.
    exit /b 1
)
set "TIMING_LINE=frame_events=1 callback_thread=20 start_capture_call_us=310 start_capture_to_first_frame_us=unavailable(timeout) frame_wait_after_start_return_us=8000000"
set "MEASUREMENT="
set "SAMPLE="
for /f "tokens=4" %%T in ("!TIMING_LINE!") do set "MEASUREMENT=%%T"
for /f "tokens=2 delims==" %%T in ("!MEASUREMENT!") do set "SAMPLE=%%T"
for /f "delims=0123456789" %%A in ("!SAMPLE!") do set "SAMPLE="
if defined SAMPLE (
    echo FAIL: unavailable timing should not parse as a latency sample.
    exit /b 1
)
echo PASS: exact first-frame metric parsing rejects unavailable timings.
exit /b 0

:CompareSamples
set "LEFT=!SAMPLE_%~1!"
set "RIGHT=!SAMPLE_%~2!"
if !LEFT! GTR !RIGHT! (
    set "SAMPLE_%~1=!RIGHT!"
    set "SAMPLE_%~2=!LEFT!"
)
exit /b 0
