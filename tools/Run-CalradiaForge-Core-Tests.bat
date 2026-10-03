@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "PAUSE_ON_EXIT=1"
set "RESULT=0"
set "LEGACY_OUTPUT_CREATED="
set "LEGACY_OUTPUT="
set "LEGACY_SUFFIX="
for %%A in (%*) do if /I "%%~A"=="--no-pause" set "PAUSE_ON_EXIT=0"
set "CORE_TESTS=%ROOT%\tests\CalradiaForge.Tests\bin\Release\net472\CalradiaForge.Tests.dll"

if not exist "%CORE_TESTS%" (
    echo ERROR: Core test library is missing. Build the solution first.
    set "RESULT=2"
    goto finish
)

echo [Core] Running SDK, ForgeWeave, and module tests through the batch launcher...
where powershell.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Windows PowerShell is required to host the .NET Framework test assembly.
    set "RESULT=2"
    goto finish
)
where dotnet.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: The .NET SDK is required to build the isolated SDK v12 consumer fixture.
    set "RESULT=2"
    goto finish
)
set "LEGACY_SUFFIX=%RANDOM%-%RANDOM%"
set "LEGACY_OUTPUT=%TEMP%\CalradiaForge.LegacySdkV12-%LEGACY_SUFFIX%"
if exist "%LEGACY_OUTPUT%" (
    echo ERROR: Refusing to reuse the existing legacy SDK fixture directory: "%LEGACY_OUTPUT%"
    set "RESULT=2"
    goto finish
)
mkdir "%LEGACY_OUTPUT%" >nul 2>nul
if errorlevel 1 (
    echo ERROR: Could not create the isolated legacy SDK fixture directory.
    set "RESULT=2"
    goto finish
)
set "LEGACY_OUTPUT_CREATED=1"
echo [Core] Building an isolated binary consumer against the v12 SDK contract...
dotnet.exe build "%ROOT%\tests\CalradiaForge.LegacySdkV12.Consumer\CalradiaForge.LegacySdkV12.Consumer.csproj" --configuration Release --framework net472 --output "%LEGACY_OUTPUT%" --nologo --verbosity minimal
if errorlevel 1 (
    echo ERROR: The isolated SDK v12 compatibility fixture failed to build.
    set "RESULT=1"
    goto finish
)
if not exist "%LEGACY_OUTPUT%\CalradiaForge.LegacySdkV12.Consumer.dll" (
    echo ERROR: The legacy consumer assembly was not produced.
    set "RESULT=2"
    goto finish
)
rem Remove the v12 contract assembly so the test host must bind the consumer to the current v13 SDK.
del /q "%LEGACY_OUTPUT%\CalradiaForge.Sdk.dll" "%LEGACY_OUTPUT%\CalradiaForge.Sdk.pdb" "%LEGACY_OUTPUT%\CalradiaForge.Sdk.xml" "%LEGACY_OUTPUT%\CalradiaForge.Sdk.deps.json" >nul 2>nul
if exist "%LEGACY_OUTPUT%\CalradiaForge.Sdk.dll" (
    echo ERROR: The v12 reference contract must not be present beside the legacy consumer at runtime.
    set "RESULT=2"
    goto finish
)
set "CALRADIAFORGE_LEGACY_V12_CLIENT_DLL=%LEGACY_OUTPUT%\CalradiaForge.LegacySdkV12.Consumer.dll"
set "CALRADIAFORGE_CORE_TEST_ASSEMBLY=%CORE_TESTS%"
set "CORE_TEST_RUNNER=$ErrorActionPreference='Stop'; try { $assemblyPath=(Resolve-Path -LiteralPath $env:CALRADIAFORGE_CORE_TEST_ASSEMBLY).Path; $assemblyDirectory=Split-Path -Parent $assemblyPath; [AppDomain]::CurrentDomain.SetData('APPBASE',($assemblyDirectory.TrimEnd('\')+'\')); $assembly=[Reflection.Assembly]::LoadFrom($assemblyPath); $program=$assembly.GetType('Program',$true,$false); $flags=[Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Public; $entryPoint=$program.GetMethod('Main',$flags); if ($null -eq $entryPoint) { throw 'Internal test runner not found.' }; $testArguments=[object[]]::new(1); if ([string]::IsNullOrWhiteSpace($env:FORGE_TEST_MODULES)) { $testArguments[0]=[string[]]@() } else { $testArguments[0]=[string[]]@($env:FORGE_TEST_MODULES) }; Write-Host ('[Core] Hosted by {0}; test library invoked in-process.' -f (Get-Process -Id $PID).ProcessName); try { $null=$entryPoint.Invoke($null,$testArguments) } catch { $exception=$_.Exception; if ($exception -is [Reflection.TargetInvocationException] -and $null -ne $exception.InnerException) { $exception=$exception.InnerException }; [Console]::Error.WriteLine('[Core] Test host exception: '+$exception.ToString()); exit 1 }; exit [Environment]::ExitCode } catch { [Console]::Error.WriteLine('[Core] Could not host the Core test assembly: '+$_.Exception.ToString()); exit 2 }"
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "%CORE_TEST_RUNNER%" <nul
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: Core suite failed with exit code %RESULT%.
if not "%RESULT%"=="0" goto finish

rem Native executable-memory writes run only in a disposable x64 .NET-hosted
rem fixture process launched by its dedicated BAT, with no fixture EXE apphost.
call "%ROOT%\tests\CalradiaForge.DetourFixture\Run-DetourFixture.bat" --no-pause
set "RESULT=%ERRORLEVEL%"
if not "%RESULT%"=="0" echo ERROR: Isolated native detour fixture failed with exit code %RESULT%.

:finish
set "CALRADIAFORGE_LEGACY_V12_CLIENT_DLL="
if defined LEGACY_OUTPUT_CREATED for %%I in ("%TEMP%") do set "TEMP_ROOT_ABS=%%~fI"
if defined LEGACY_OUTPUT_CREATED for %%I in ("%LEGACY_OUTPUT%") do set "LEGACY_OUTPUT_ABS=%%~fI"
if defined LEGACY_OUTPUT_CREATED set "EXPECTED_LEGACY_OUTPUT_ABS=%TEMP_ROOT_ABS%\CalradiaForge.LegacySdkV12-%LEGACY_SUFFIX%"
if defined LEGACY_OUTPUT_CREATED if /I not "%LEGACY_OUTPUT_ABS%"=="%EXPECTED_LEGACY_OUTPUT_ABS%" (
    echo ERROR: Refusing to remove a legacy fixture path that does not match its generated temporary path.
    if "%RESULT%"=="0" set "RESULT=2"
)
if defined LEGACY_OUTPUT_CREATED if /I "%LEGACY_OUTPUT_ABS%"=="%EXPECTED_LEGACY_OUTPUT_ABS%" rmdir /s /q "%EXPECTED_LEGACY_OUTPUT_ABS%"
if defined LEGACY_OUTPUT_CREATED if exist "%EXPECTED_LEGACY_OUTPUT_ABS%" (
    echo ERROR: Could not remove the owned temporary legacy SDK fixture directory: "%EXPECTED_LEGACY_OUTPUT_ABS%"
    if "%RESULT%"=="0" set "RESULT=2"
)
set "CALRADIAFORGE_CORE_TEST_ASSEMBLY="
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
