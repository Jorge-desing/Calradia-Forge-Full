# Ejecución de pruebas cortas

Desde la raíz del repositorio, ejecuta `tools\Run-CalradiaForge-Tests.bat`. El archivo `.bat` compila la solución en Release y ejecuta las pruebas de preparación del Resource Browser, Core, ForgeWeave, Desktop y renderizado Desktop mediante lanzadores `.bat`. Las pruebas de recursos verifican el modo de prueba, los hashes, el aviso de versiones distintas, la creación de respaldos y el rechazo de otro ID de módulo. No abras ejecutables de prueba directamente.

Las suites .NET 8 se ejecutan mediante `dotnet` y sus DLL, sin generar app-host nativo. Core para .NET Framework 4.7.2 se compila como biblioteca y su ejecutor interno se llama desde `powershell.exe`; no se genera ni inicia un `.exe` de pruebas Core. Cada lanzador se puede usar por separado tras compilar la solución. El archivo principal continúa con los grupos seleccionados, devuelve error si alguno falla y hace pausa al final cuando se inicia de forma interactiva.

`CalradiaForgeTests.bat` es un punto de entrada compatible para la suite completa. `tools\build.ps1` y el empaquetador usan `tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause` después de compilar. `--core-only` ejecuta Core y ForgeWeave, `--desktop-only` ejecuta las suites Desktop y `--assets-only` ejecuta las pruebas de preparación del Resource Browser. `--no-pause` se usa para automatización. Para escanear los módulos instalados, define `FORGE_TEST_MODULES` con la carpeta `Modules` del juego.

Las pruebas del Resource Browser también verifican la recolección del TPAC, su hash, la creación de respaldo al reemplazarlo y el rechazo de paquetes vacíos.
