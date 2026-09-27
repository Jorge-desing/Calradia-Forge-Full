@echo off
setlocal EnableExtensions EnableDelayedExpansion
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%.."
set "RUNTIME_BASE=%LOCALAPPDATA%\OpenAI\Codex\runtimes\cua_node"
set "RUNTIME_DIR="
set "RUNTIME_AMBIGUOUS=0"

if defined CODEX_CUA_RUNTIME_DIR (
    set "RUNTIME_DIR=%CODEX_CUA_RUNTIME_DIR%"
) else (
    for /d %%R in ("%RUNTIME_BASE%\*") do (
        if exist "%%~fR\bin\node.exe" if exist "%%~fR\bin\node_modules\@oai\sky\package.json" (
            if defined RUNTIME_DIR set "RUNTIME_AMBIGUOUS=1"
            set "RUNTIME_DIR=%%~fR"
        )
    )
)

if not defined RUNTIME_DIR (
    echo Could not find a complete Codex cua_node runtime under "%RUNTIME_BASE%".
    echo Set CODEX_CUA_RUNTIME_DIR to the exact runtime directory and retry.
    exit /b 2
)
if "%RUNTIME_AMBIGUOUS%"=="1" if not defined CODEX_CUA_RUNTIME_DIR (
    echo Multiple cua_node runtimes are installed. Set CODEX_CUA_RUNTIME_DIR to the exact runtime directory.
    exit /b 2
)

set "NODE=%RUNTIME_DIR%\bin\node.exe"
rem Keep the benchmark's identity check aligned with Start-CodexCaptureCompatFixture.bat.
set "FIXTURE=%PROJECT_DIR%\build\capture_test_window.exe"
set "HARNESS=%SCRIPT_DIR%Measure-CodexCaptureCompatSky.mjs"
if not exist "%NODE%" (echo Missing runtime Node executable: "%NODE%"& exit /b 2)
if not exist "%HARNESS%" (echo Missing benchmark harness: "%HARNESS%"& exit /b 2)

if /i not "%~1"=="--self-test" if /i not "%~1"=="--help" if /i not "%~1"=="-h" (
    set "HAS_FIXTURE_WINDOW_ID=0"
    for %%A in (%*) do if /i "%%~A"=="--fixture-window-id" set "HAS_FIXTURE_WINDOW_ID=1"
    if "!HAS_FIXTURE_WINDOW_ID!"=="0" (
        echo Live mode requires --fixture-window-id from the verified Windows window inventory.
        echo Start the isolated fixture with tests\Start-CodexCaptureCompatFixture.bat, inspect its exact window, then pass that ID.
        exit /b 2
    )
    if not exist "%FIXTURE%" (
        echo Missing isolated fixture: "%FIXTURE%"
        echo Building it through the existing .bat launcher.
        call "%PROJECT_DIR%\build.bat"
        if errorlevel 1 exit /b 2
    )
    if not exist "%FIXTURE%" (echo Fixture build did not produce "%FIXTURE%".& exit /b 2)
    call :EnsureFixture
    if errorlevel 1 exit /b !ERRORLEVEL!
)

set "CODEX_CUA_RUNTIME_DIR=%RUNTIME_DIR%"
"%NODE%" "%HARNESS%" --runtime-dir "%RUNTIME_DIR%" --fixture-path "%FIXTURE%" %*
exit /b %ERRORLEVEL%

:EnsureFixture
set "CODEX_CAPTURE_FIXTURE=%FIXTURE%"
call :ProbeFixture
if "!PROBE_RESULT!"=="0" (
    echo Reusing the unique fixture process with the exact expected executable path.
    exit /b 0
)
if not "!PROBE_RESULT!"=="1" (
    echo Fixture process identity is ambiguous or could not be read. No process will be started or stopped.
    exit /b 2
)

echo Starting only the isolated Codex capture fixture.
start "Codex Computer Use fixture" "%FIXTURE%"
for /l %%I in (1,1,25) do (
    timeout /t 1 /nobreak >nul
    call :ProbeFixture
    if "!PROBE_RESULT!"=="0" (
        echo Fixture executable path verified; the benchmark will check its exact window title and app path again.
        exit /b 0
    )
    if not "!PROBE_RESULT!"=="1" (
        echo A same-name process has an ambiguous or different identity. Leaving it untouched.
        exit /b 2
    )
)
echo Fixture process did not start within 25 seconds.
exit /b 1

:ProbeFixture
set "PROBE_RESULT="
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; try { $expected=[IO.Path]::GetFullPath($env:CODEX_CAPTURE_FIXTURE); $items=@(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object { $_.Name -ieq 'capture_test_window.exe' }); if($items.Count -eq 0){exit 1}; if($items.Count -ne 1){exit 2}; $actual=$items[0].ExecutablePath; if([string]::IsNullOrWhiteSpace($actual)){exit 3}; if(-not [string]::Equals([IO.Path]::GetFullPath($actual),$expected,[StringComparison]::OrdinalIgnoreCase)){exit 4}; exit 0 } catch { exit 5 }" >nul 2>nul
set "PROBE_EXIT=!ERRORLEVEL!"
if "!PROBE_EXIT!"=="0" set "PROBE_RESULT=0"
if "!PROBE_EXIT!"=="1" set "PROBE_RESULT=1"
if not defined PROBE_RESULT set "PROBE_RESULT=unsafe"
exit /b 0
