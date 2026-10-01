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
set "FIXTURE_DLL=%OUTPUT_DIR%\CalradiaForge.DetourFixture.dll"
set "POWERSHELL_HOST=%WINDIR%\System32\WindowsPowerShell\v1.0\powershell.exe"
if defined PROCESSOR_ARCHITEW6432 set "POWERSHELL_HOST=%WINDIR%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
set "PAUSE_ON_EXIT=1"
set "DOTNET_HOST="
set "RUN_BENCHMARK=0"
for %%A in (%*) do (
    if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
    if /I "%%~A"=="--benchmark" set "RUN_BENCHMARK=1"
)

echo [DetourFixture] Checking the serial hook fixture's stage labels...
for %%S in (1 2 3 4 5 6 7 8 9 10) do (
    findstr /L /C:"[HookFixture] Stage %%S/10:" "%~dp0Program.cs" >nul
    if errorlevel 1 (
        echo ERROR: Expected HookFixture stage %%S/10 is missing.
        set "RESULT=2"
        goto finish
    )
)
for %%S in (1 2 3) do (
    findstr /L /C:"[HookFixture] Stage %%S/9:" "%~dp0Program.cs" >nul
    if not errorlevel 1 (
        echo ERROR: Stale HookFixture stage %%S/9 label remains.
        set "RESULT=2"
        goto finish
    )
)
findstr /L /C:"prefixReapplyDuringCallback = prefixHandle.Apply();" "%~dp0Program.cs" >nul
if errorlevel 1 (
    echo ERROR: The self-reverting Prefix must verify reapply is blocked during its active dispatch lease.
    set "RESULT=2"
    goto finish
)
findstr /L /C:"Postfix disposes its own handle; active result completes and future calls are unhooked." "%~dp0Program.cs" >nul
if errorlevel 1 (
    echo ERROR: The self-disposing Postfix regression is missing.
    set "RESULT=2"
    goto finish
)
findstr /L /C:"No Undo or disposal was attempted" "%~dp0Program.cs" >nul
if errorlevel 1 (
    echo ERROR: The context-transition regression must assert that deferred cleanup performs no off-context mutation.
    set "RESULT=2"
    goto finish
)
findstr /L /C:"restore raw application acceptance if the host gate closes after preflight" "%~dp0Program.cs" >nul
if errorlevel 1 (
    echo ERROR: The ForgeApi disconnect retry regression is missing.
    set "RESULT=2"
    goto finish
)
findstr /L /C:"Runtime unload: disable retained callbacks before a disconnect that may fail" "%~dp0Program.cs" >nul
if errorlevel 1 (
    echo ERROR: The runtime unload callback-shutdown regression is missing.
    set "RESULT=2"
    goto finish
)

if not exist "%POWERSHELL_HOST%" (
    echo ERROR: The 64-bit Windows PowerShell host is required for the isolated fixture.
    set "RESULT=2"
    goto finish
)
if defined ProgramW6432 if exist "%ProgramW6432%\dotnet\dotnet.exe" set "DOTNET_HOST=%ProgramW6432%\dotnet\dotnet.exe"
if not defined DOTNET_HOST (
    where dotnet.exe >nul 2>nul
    if not errorlevel 1 set "DOTNET_HOST=dotnet.exe"
)
if not defined DOTNET_HOST (
    echo ERROR: .NET SDK is required to build the x64 detour fixture.
    set "RESULT=2"
    goto finish
)

echo [DetourFixture] Building disposable net472 x64 fixture library...
"%DOTNET_HOST%" build "%PROJECT%" --configuration Release --framework net472 --nologo --verbosity minimal --output "%OUTPUT_DIR%"
if errorlevel 1 (
    rem Percent expansion occurs when this parenthesized block is parsed, so do
    rem not copy ERRORLEVEL here; the build failure must always stay nonzero.
    set "RESULT=1"
    goto finish
)

if not exist "%FIXTURE_DLL%" (
    echo ERROR: Expected fixture assembly was not produced in isolated output: "%FIXTURE_DLL%"
    set "RESULT=2"
    goto finish
)
if exist "%OUTPUT_DIR%\CalradiaForge.DetourFixture.exe" (
    echo ERROR: The library build unexpectedly emitted CalradiaForge.DetourFixture.exe. Refusing to run it; tests must use this BAT.
    set "RESULT=2"
    goto finish
)

echo [DetourFixture] Running serial native detour smoke test in disposable x64 PowerShell host...
set "CALRADIAFORGE_FIXTURE_DLL=%FIXTURE_DLL%"
set "CALRADIAFORGE_FIXTURE_BENCHMARK=%RUN_BENCHMARK%"
"%POWERSHELL_HOST%" -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "$ErrorActionPreference='Stop'; $architecture=[System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture; if ($architecture -ne [System.Runtime.InteropServices.Architecture]::X64) { Write-Error ('Fixture host must be x64 (AMD64); detected ' + $architecture + '.'); exit 2 }; try { $assembly=[Reflection.Assembly]::LoadFrom($env:CALRADIAFORGE_FIXTURE_DLL); $fixtureType=$assembly.GetType('CalradiaForge.DetourFixture.Program',$true); $flags=[Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::Public; $method=$fixtureType.GetMethod('RunFixture',$flags); if ($null -eq $method) { throw 'Fixture entry point was not found.' }; $result=[int]$method.Invoke($null,[object[]]@()); if ($result -eq 0 -and $env:CALRADIAFORGE_FIXTURE_BENCHMARK -eq '1') { $benchmark=$fixtureType.GetMethod('RunHookBenchmark',$flags); if ($null -eq $benchmark) { throw 'Benchmark entry point was not found.' }; $result=[int]$benchmark.Invoke($null,[object[]]@()) }; exit $result } catch { Write-Error $_; exit 1 }"
set "RESULT=%ERRORLEVEL%"
set "CALRADIAFORGE_FIXTURE_DLL="
set "CALRADIAFORGE_FIXTURE_BENCHMARK="
if not "%RESULT%"=="0" echo ERROR: Detour fixture failed with exit code %RESULT%.

:finish
if defined OUTPUT_CREATED (
    del /q "%OUTPUT_DIR%\CalradiaForge.DetourFixture.dll" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.pdb" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.deps.json" "%OUTPUT_DIR%\CalradiaForge.DetourFixture.runtimeconfig.json" "%OUTPUT_DIR%\CalradiaForge.Sdk.dll" "%OUTPUT_DIR%\CalradiaForge.Sdk.pdb" "%OUTPUT_DIR%\CalradiaForge.Sdk.xml" "%OUTPUT_DIR%\CalradiaForge.Sdk.deps.json" "%OUTPUT_DIR%\CalradiaForge.Core.dll" "%OUTPUT_DIR%\CalradiaForge.Core.pdb" "%OUTPUT_DIR%\CalradiaForge.Core.xml" "%OUTPUT_DIR%\CalradiaForge.Core.deps.json" "%OUTPUT_DIR%\Mono.Cecil.dll" "%OUTPUT_DIR%\Mono.Cecil.Mdb.dll" "%OUTPUT_DIR%\Mono.Cecil.Pdb.dll" "%OUTPUT_DIR%\Mono.Cecil.Rocks.dll" "%OUTPUT_DIR%\MonoMod.Backports.dll" "%OUTPUT_DIR%\MonoMod.Core.dll" "%OUTPUT_DIR%\MonoMod.Iced.dll" "%OUTPUT_DIR%\MonoMod.ILHelpers.dll" "%OUTPUT_DIR%\MonoMod.RuntimeDetour.dll" "%OUTPUT_DIR%\MonoMod.Utils.dll" "%OUTPUT_DIR%\System.ValueTuple.dll" >nul 2>nul
    rmdir "%OUTPUT_DIR%" >nul 2>nul
)
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
