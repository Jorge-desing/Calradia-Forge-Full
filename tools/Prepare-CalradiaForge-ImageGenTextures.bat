@echo off
setlocal DisableDelayedExpansion
set "SCRIPT=%~dp0Prepare-CalradiaForge-ImageGenTextures.ps1"
set "PAUSE_ON_EXIT=1"
set "MODE_ARGS="
set "CLOTH_ARGS="
set "RAIL_ARGS="
set "OVERLAY_ARGS="
set "BRASS_ARGS="
set "FELT_ARGS="
set "HEADER_SUMMARY_ARGS="
set "HEADER_MODULES_ARGS="
set "HEADER_LOGS_ARGS="
set "HEADER_INSPECTOR_ARGS="
set "HEADER_TESTS_ARGS="
set "HEADER_METRICS_ARGS="
set "HEADER_FRAMEWORK_ARGS="
set "HEADER_EXTENSIONS_ARGS="
set "SHOW_HELP=0"
set "RESULT=0"

:parse_args
if "%~1"=="" goto args_done
if /I "%~1"=="--no-pause" (
    set "PAUSE_ON_EXIT=0"
    shift
    goto parse_args
)
if /I "%~1"=="--help" (
    set "SHOW_HELP=1"
    shift
    goto parse_args
)
if /I "%~1"=="--check" (
    if defined MODE_ARGS goto conflicting_mode
    set "MODE_ARGS=-Check"
    shift
    goto parse_args
)
if /I "%~1"=="--prepare" (
    if defined MODE_ARGS goto conflicting_mode
    set "MODE_ARGS=-Prepare"
    shift
    goto parse_args
)
if /I "%~1"=="--war-table-cloth-v2" (
    if "%~2"=="" goto missing_value
    set "CLOTH_ARGS=-WarTableClothV2Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--rail-cartographic-field-v1" (
    if "%~2"=="" goto missing_value
    set "RAIL_ARGS=-RailCartographicFieldV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--heraldic-field-journal-overlay" (
    if "%~2"=="" goto missing_value
    set "OVERLAY_ARGS=-HeraldicFieldJournalOverlayMaster "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--aged-brass-patina" (
    if "%~2"=="" goto missing_value
    set "BRASS_ARGS=-AgedBrassPatinaMaster "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--pine-felt" (
    if "%~2"=="" goto missing_value
    set "FELT_ARGS=-PineFeltMaster "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-summary-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_SUMMARY_ARGS=-HeaderSummaryV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-modules-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_MODULES_ARGS=-HeaderModulesV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-logs-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_LOGS_ARGS=-HeaderLogsV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-inspector-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_INSPECTOR_ARGS=-HeaderInspectorV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-tests-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_TESTS_ARGS=-HeaderTestsV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-metrics-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_METRICS_ARGS=-HeaderMetricsV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-framework-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_FRAMEWORK_ARGS=-HeaderFrameworkV1Master "%~2""
    shift
    shift
    goto parse_args
)
if /I "%~1"=="--header-extensions-v1" (
    if "%~2"=="" goto missing_value
    set "HEADER_EXTENSIONS_ARGS=-HeaderExtensionsV1Master "%~2""
    shift
    shift
    goto parse_args
)
echo ERROR: Unknown option: %~1
goto usage_error

:args_done
if "%SHOW_HELP%"=="1" goto usage
if not defined MODE_ARGS (
    echo ERROR: Choose --check or --prepare.
    goto usage_error
)
where powershell.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Windows PowerShell was not found on PATH.
    set "RESULT=9009"
    goto finish
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %MODE_ARGS% %CLOTH_ARGS% %RAIL_ARGS% %OVERLAY_ARGS% %BRASS_ARGS% %FELT_ARGS% %HEADER_SUMMARY_ARGS% %HEADER_MODULES_ARGS% %HEADER_LOGS_ARGS% %HEADER_INSPECTOR_ARGS% %HEADER_TESTS_ARGS% %HEADER_METRICS_ARGS% %HEADER_FRAMEWORK_ARGS% %HEADER_EXTENSIONS_ARGS%
set "RESULT=%ERRORLEVEL%"
goto finish

:conflicting_mode
echo ERROR: Specify only one mode: --check or --prepare.
goto usage_error

:missing_value
echo ERROR: The preceding option requires a PNG path.
goto usage_error

:usage
echo Usage:
echo   Prepare-CalradiaForge-ImageGenTextures.bat --check [options] [--no-pause]
echo   Prepare-CalradiaForge-ImageGenTextures.bat --prepare [options] [--no-pause]
echo Options:
echo   --war-table-cloth-v2 ^<path^> Override forge_war_table_cloth_v2.png
echo   --rail-cartographic-field-v1 ^<path^> Override forge_rail_cartographic_field_v1.png
echo   --heraldic-field-journal-overlay ^<path^> Override heraldic_field_journal_overlay_v1.png
echo   --aged-brass-patina ^<path^> Override aged_brass_patina.png
echo   --pine-felt ^<path^>       Override pine_felt.png
echo   --header-summary-v1 ^<path^>   Override forge_header_summary_v1.png
echo   --header-modules-v1 ^<path^>   Override forge_header_modules_v1.png
echo   --header-logs-v1 ^<path^>      Override forge_header_logs_v1.png
echo   --header-inspector-v1 ^<path^> Override forge_header_inspector_v1.png
echo   --header-tests-v1 ^<path^>     Override forge_header_tests_v1.png
echo   --header-metrics-v1 ^<path^>   Override forge_header_metrics_v1.png
echo   --header-framework-v1 ^<path^> Override forge_header_framework_v1.png
echo   --header-extensions-v1 ^<path^> Override forge_header_extensions_v1.png
echo Relative master paths resolve from the repository root.
goto finish

:usage_error
set "RESULT=2"
goto usage

:finish
if "%PAUSE_ON_EXIT%"=="1" pause
exit /b %RESULT%
