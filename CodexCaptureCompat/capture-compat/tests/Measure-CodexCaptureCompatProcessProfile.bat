@echo off
setlocal EnableExtensions

set "WPT=C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit"
set "OUT=%TEMP%\CodexCaptureCompat\process-profile"
if /I "%~1"=="--stop-owned" goto stop_owned

for /f %%I in ('powershell.exe -NoProfile -Command "[guid]::NewGuid().ToString('N')"') do set "STAMP=%%I"
set "TRACE=%OUT%\cua-process-%STAMP%.etl"
set "PROFILE=%OUT%\cua-profile-%STAMP%.txt"
set "CSWITCH=%OUT%\cua-cswitch-%STAMP%.txt"
set "STATUS=%OUT%\wpr-status-%STAMP%.txt"
set "STARTSTATUS=%OUT%\wpr-start-status-%STAMP%.txt"

if not defined STAMP (
  echo Could not generate a unique trace name.
  exit /b 2
)

if not exist "%WPT%\wpr.exe" (
  echo Windows Performance Recorder was not found at the Windows Kits path.
  exit /b 2
)
if not exist "%WPT%\xperf.exe" (
  echo xperf.exe was not found at the Windows Kits path.
  exit /b 2
)
if not exist "%OUT%" mkdir "%OUT%"
if errorlevel 1 (
  echo Could not create the trace output directory.
  exit /b 2
)
if exist "%TRACE%" (
  echo Trace path already exists; refusing to overwrite it.
  exit /b 2
)
if exist "%PROFILE%" (
  echo Profile path already exists; refusing to overwrite it.
  exit /b 2
)
if exist "%CSWITCH%" (
  echo Context report path already exists; refusing to overwrite it.
  exit /b 2
)
if exist "%STATUS%" (
  echo Status path already exists; refusing to overwrite it.
  exit /b 2
)
if exist "%STARTSTATUS%" (
  echo Start-status path already exists; refusing to overwrite it.
  exit /b 2
)

"%WPT%\wpr.exe" -status > "%STATUS%" 2>&1
if errorlevel 1 (
  echo WPR status could not be read. Existing recording was left untouched.
  type "%STATUS%"
  exit /b 3
)
findstr /I /C:"WPR is not recording" "%STATUS%" >nul
if errorlevel 1 (
  echo WPR is already recording or its status is uncertain. Existing recording was left untouched.
  type "%STATUS%"
  exit /b 3
)

"%WPT%\wpr.exe" -start GeneralProfile -filemode
if errorlevel 1 (
  echo WPR could not start GeneralProfile. No trace was collected.
  exit /b 4
)
"%WPT%\wpr.exe" -status > "%STARTSTATUS%" 2>&1
if errorlevel 1 (
  echo WPR status could not confirm a recording. No stop or cancel was issued.
  type "%STARTSTATUS%"
  exit /b 4
)
findstr /I /C:"WPR is recording" /C:"WPR recording is in progress" "%STARTSTATUS%" >nul
if errorlevel 1 (
  echo WPR did not enter a confirmed recording state. No stop or cancel was issued.
  type "%STARTSTATUS%"
  exit /b 4
)

echo Recording a 15-second system CPU profile. Keep the normal workload unchanged.
rem PowerShell sleep avoids TIMEOUT's interactive-input requirement when this BAT is launched with redirected stdin.
powershell.exe -NoProfile -Command "Start-Sleep -Seconds 15"
if errorlevel 1 (
  echo Wait failed. No automatic WPR stop or cancel was issued; inspect WPR status before recovery.
  exit /b 5
)
"%WPT%\wpr.exe" -stop "%TRACE%" "Codex Computer Use process profile"
if errorlevel 1 (
  echo WPR could not save the trace. No automatic cancel was issued because ownership of an active recorder cannot be revalidated safely.
  "%WPT%\wpr.exe" -status
  exit /b 5
)
if not exist "%TRACE%" (
  echo WPR reported stop completion but no ETL file was created.
  exit /b 5
)
for %%A in ("%TRACE%") do if %%~zA LEQ 0 (
  echo WPR created an empty ETL file; no report can be trusted.
  exit /b 5
)

"%WPT%\xperf.exe" -i "%TRACE%" -o "%PROFILE%" -a profile -detail
if errorlevel 1 goto :profile_export_failed
if not exist "%PROFILE%" goto :profile_export_failed
for %%A in ("%PROFILE%") do if %%~zA LEQ 0 goto :profile_export_failed
"%WPT%\xperf.exe" -i "%TRACE%" -o "%CSWITCH%" -a cswitch -process -thread
if errorlevel 1 goto :cswitch_export_failed
if not exist "%CSWITCH%" goto :cswitch_export_failed
for %%A in ("%CSWITCH%") do if %%~zA LEQ 0 goto :cswitch_export_failed

echo.
echo Trace:   "%TRACE%"
echo Profile: "%PROFILE%"
echo Context: "%CSWITCH%"
echo.
findstr /I /C:"codex-computer-use" /C:"ChatGPT" /C:"codex.exe" /C:"uiautomation" /C:"version.dll" "%PROFILE%" "%CSWITCH%" >nul
if errorlevel 1 (
  echo Trace and reports were created, but they contain no matching CUA process or module names.
  exit /b 0
)
echo Matching process/module lines:
findstr /I /C:"codex-computer-use" /C:"ChatGPT" /C:"codex.exe" /C:"uiautomation" /C:"version.dll" "%PROFILE%" "%CSWITCH%"
exit /b 0

:profile_export_failed
  echo Trace captured, but CPU profile export failed. Raw trace: "%TRACE%"
  exit /b 6

:cswitch_export_failed
  echo CPU profile exported; context-switch export failed. Raw trace: "%TRACE%"
  exit /b 7

:stop_owned
set "STAMP=%~2"
if not defined STAMP (
  echo Usage: %~nx0 --stop-owned <32-character-session-id>
  exit /b 2
)
set "TRACE=%OUT%\cua-process-%STAMP%.etl"
set "STARTSTATUS=%OUT%\wpr-start-status-%STAMP%.txt"
set "RECOVERYSTATUS=%OUT%\wpr-recovery-status-%STAMP%.txt"
if not exist "%STARTSTATUS%" (
  echo No matching start-status record exists; WPR was left untouched.
  exit /b 3
)
if exist "%RECOVERYSTATUS%" (
  echo Recovery status path already exists; refusing to overwrite it.
  exit /b 3
)
"%WPT%\wpr.exe" -status > "%RECOVERYSTATUS%" 2>&1
if errorlevel 1 (
  echo WPR status is uncertain; no stop or cancel was issued.
  type "%RECOVERYSTATUS%"
  exit /b 3
)
findstr /I /C:"WPR is recording" /C:"WPR recording is in progress" "%RECOVERYSTATUS%" >nul
if errorlevel 1 (
  echo WPR does not show an active recording; no stop or cancel was issued.
  type "%RECOVERYSTATUS%"
  exit /b 3
)
findstr /I /C:"WPR recording is in progress" "%STARTSTATUS%" >nul
if errorlevel 1 (
  echo The matching start record does not confirm this BAT started a recording; no stop or cancel was issued.
  exit /b 3
)
if exist "%TRACE%" (
  echo Trace target already exists; refusing to overwrite it.
  exit /b 3
)
echo Stopping only the active GeneralProfile session paired with the matching unique start-status record.
"%WPT%\wpr.exe" -stop "%TRACE%" "Codex Computer Use process profile recovery"
if errorlevel 1 (
  echo WPR stop failed. No cancel was issued; check elevated WPR status manually.
  exit /b 5
)
if not exist "%TRACE%" (
  echo WPR reported stop completion but did not create the trace file.
  exit /b 5
)
for %%A in ("%TRACE%") do if %%~zA LEQ 0 (
  echo WPR created an empty trace; it is not a valid profile.
  exit /b 5
)
echo Recovered trace: "%TRACE%"
exit /b 0
