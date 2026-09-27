@echo off
setlocal
set "ACTION=%~1"
if "%ACTION%"=="" set "ACTION=Status"
if /i not "%ACTION%"=="Status" if /i not "%ACTION%"=="Test" if /i not "%ACTION%"=="Apply" if /i not "%ACTION%"=="Rollback" (
  echo Usage: Manage-CodexCaptureCompatKeyAliases.bat [Status^|Test^|Apply^|Rollback] "^<exact-runtime-path^>" "^<exact-@oai-sky-package-path^>"
  exit /b 2
)
set "RUNTIME_PATH=%~2"
set "PACKAGE_PATH=%~3"
if "%RUNTIME_PATH%"=="" (
  echo An exact runtime path is required.
  echo Example: "%%LOCALAPPDATA%%\OpenAI\Codex\runtimes\cua_node\b35a688736d56912"
  exit /b 2
)
if "%PACKAGE_PATH%"=="" (
  echo An exact @oai/sky package path is required.
  echo Example: "%%LOCALAPPDATA%%\OpenAI\Codex\runtimes\cua_node\b35a688736d56912\bin\node_modules\@oai\sky"
  exit /b 2
)
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0Manage-CodexCaptureCompatKeyAliases.ps1" -Action "%ACTION%" -RuntimePath "%RUNTIME_PATH%" -PackagePath "%PACKAGE_PATH%"
exit /b %ERRORLEVEL%
