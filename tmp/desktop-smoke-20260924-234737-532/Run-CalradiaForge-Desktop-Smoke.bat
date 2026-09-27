@echo off
setlocal
dotnet "C:\Users\Alex\Documents\Mod Desarrolladores\src\CalradiaForge.Desktop\bin\Release\net8.0-windows\CalradiaForge.Desktop.dll" %*
set "RESULT=%ERRORLEVEL%"
exit /b %RESULT%

