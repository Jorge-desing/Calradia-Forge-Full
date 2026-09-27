@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
set "SKIP_BUILD=0"
set "PAUSE_ON_EXIT=1"
set "RUN_CORE=1"
set "RUN_FORGEWEAVE=1"
set "RUN_DESKTOP=1"
set "RUN_ASSETS=1"
set "RENDER_OUTPUT="
set "FAILED=0"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--skip-build" set "SKIP_BUILD=1"
if /I "%~1"=="--no-pause" set "PAUSE_ON_EXIT=0"
if /I "%~1"=="--core-only" (
    set "RUN_DESKTOP=0"
    set "RUN_ASSETS=0"
)
if /I "%~1"=="--desktop-only" (
    set "RUN_CORE=0"
    set "RUN_FORGEWEAVE=0"
    set "RUN_ASSETS=0"
)
if /I "%~1"=="--assets-only" (
    set "RUN_CORE=0"
    set "RUN_FORGEWEAVE=0"
    set "RUN_DESKTOP=0"
)
if /I "%~1"=="--render-output" goto parse_render_output
shift
goto parse_args

:parse_render_output
shift
if "%~1"=="" (
    echo ERROR: --render-output requires a JSON output path.
    goto invalid_arguments
)
if defined RENDER_OUTPUT (
    echo ERROR: Specify only one render output path.
    goto invalid_arguments
)
set "RENDER_OUTPUT=%~1"
shift
goto parse_args

:args_done
pushd "%ROOT%"
if errorlevel 1 goto setup_failed

if "%SKIP_BUILD%"=="1" goto run_tests

rem A Core-only run still exercises the Core harness plus the standalone
rem ForgeWeave suite. Build those test dependency graphs instead of the full
rem product solution; the default run below continues to compile everything.
if not "%RUN_DESKTOP%"=="0" goto consider_desktop_only_build
if not "%RUN_ASSETS%"=="0" goto build_solution
if "%RUN_CORE%"=="0" if "%RUN_FORGEWEAVE%"=="0" goto run_tests
echo [Build] Compiling selected Core and ForgeWeave test dependency graphs only...
if "%RUN_CORE%"=="0" goto build_selected_forgeweave
dotnet build tests\CalradiaForge.Tests\CalradiaForge.Tests.csproj -c Release --nologo
if errorlevel 1 goto failed
if "%RUN_FORGEWEAVE%"=="0" goto run_tests
:build_selected_forgeweave
rem ForgeWeave tests target net8.0 and consume the Core/SDK net8.0 outputs.
rem Refresh that dependency graph so this selective run never tests stale assemblies.
dotnet build src\CalradiaForge.Core\CalradiaForge.Core.csproj -c Release -f net8.0 --nologo
if errorlevel 1 goto failed
dotnet build tests\CalradiaForge.ForgeWeave.Tests\CalradiaForge.ForgeWeave.Tests.csproj -c Release --nologo -p:BuildProjectReferences=false
if errorlevel 1 goto failed
goto run_tests

:consider_desktop_only_build
rem A Desktop-only run needs Core, Desktop, and the two Desktop test projects,
rem not the game module, ForgeWeave, and every other project in the solution.
if not "%RUN_DESKTOP%"=="1" goto build_solution
if not "%RUN_CORE%"=="0" goto build_solution
if not "%RUN_FORGEWEAVE%"=="0" goto build_solution
if not "%RUN_ASSETS%"=="0" goto build_solution
echo [Build] Compiling Desktop test dependency graphs only...
dotnet build CalradiaForge.Desktop.slnf -c Release --nologo
if errorlevel 1 goto failed
goto run_tests

:build_solution
echo [Build] Compiling Calradia Forge and test projects...
dotnet build CalradiaForge.sln -c Release
if errorlevel 1 goto failed

:run_tests
if "%RUN_ASSETS%"=="0" goto core_tests
call "%ROOT%\tests\CalradiaForge.ResourceBrowser.Tests.bat" --no-pause
if errorlevel 1 set "FAILED=1"
call "%ROOT%\tests\CalradiaForge.AssetPipeline.Tests.bat" --no-pause
if errorlevel 1 set "FAILED=1"
call "%ROOT%\tests\CalradiaForge.TpacInventoryCompare.Tests.bat" --no-pause
if errorlevel 1 set "FAILED=1"
call "%ROOT%\tests\CalradiaForge.AssetBatchPlan.Tests.bat"
if errorlevel 1 set "FAILED=1"
call "%ROOT%\tests\CalradiaForge.FbxPreflight.Tests.bat"
if errorlevel 1 set "FAILED=1"

:core_tests
if "%RUN_CORE%"=="0" goto forgeweave_tests
call "%ROOT%\tools\Run-CalradiaForge-Core-Tests.bat" --no-pause
if errorlevel 1 set "FAILED=1"

:forgeweave_tests
if "%RUN_FORGEWEAVE%"=="0" goto desktop_tests
call "%ROOT%\tools\Run-CalradiaForge-ForgeWeave-Tests.bat" --no-pause
if errorlevel 1 set "FAILED=1"

:desktop_tests
if "%RUN_DESKTOP%"=="0" goto finish_tests
call "%ROOT%\tools\Run-CalradiaForge-Desktop-Tests.bat" --no-pause
if errorlevel 1 set "FAILED=1"
if defined RENDER_OUTPUT (
    call "%ROOT%\tools\Run-CalradiaForge-Desktop-Render-Tests.bat" --output "%RENDER_OUTPUT%" --no-pause
) else (
    call "%ROOT%\tools\Run-CalradiaForge-Desktop-Render-Tests.bat" --no-pause
)
if errorlevel 1 set "FAILED=1"

:finish_tests
if "%FAILED%"=="1" goto failed
echo All selected Calradia Forge test suites passed.
popd
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b 0

:setup_failed
echo ERROR: Could not enter the repository directory.
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b 2

:invalid_arguments
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b 2

:failed
set "RESULT=1"
:failed_with_result
echo Calradia Forge test run failed with exit code %RESULT%.
popd
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
