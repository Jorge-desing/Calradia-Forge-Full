@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"

set "TEST_PROJECT=%ROOT%tests\BannerlordFbxImporter.Tests\BannerlordFbxImporter.Tests.csproj"
set "TEST_ASSEMBLY=%ROOT%tests\BannerlordFbxImporter.Tests\bin\Release\net8.0-windows\BannerlordFbxImporter.Tests.dll"

dotnet build "%TEST_PROJECT%" --configuration Release --nologo --verbosity minimal
if errorlevel 1 exit /b %ERRORLEVEL%

dotnet "%TEST_ASSEMBLY%"
exit /b %ERRORLEVEL%
