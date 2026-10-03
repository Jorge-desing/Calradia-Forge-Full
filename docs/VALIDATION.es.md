# Evidencia de validación de Calradia Forge 14.4.0 — 2026-09-22

Se muestran los resultados históricos de 14.4.0. La evidencia del hotfix actual está en [VALIDACION-14.4.1.es.md](VALIDACION-14.4.1.es.md). Las comprobaciones de código fuente de 14.4.0 no detectaron el fallo real de enlace al inicio de WPF; 14.4.1 añade una pasarela de renderizado real de ventana/plantilla y corrige ese fallo.

La validación fue intencionalmente breve. No se ejecutaron pruebas de resistencia, muestreos de rendimiento prolongados, sesiones de campaña o batalla, pruebas que cambien el estado, modificaciones de partidas guardadas ni operaciones de red.

## Comprobaciones automatizadas

- `tools/build.ps1` se completó con una compilación Release con **0 advertencias** y **0 errores**.
- `CalradiaForge.Tests`: **220 aprobadas, 0 fallidas** con el análisis de módulos instalados; el wrapper portátil por lotes también pasó su conjunto breve con el análisis omitido.
- `CalradiaForge.ForgeWeave.Tests`: **24 aprobadas, 0 fallidas**, incluidas las pasarelas de replay opt-in, contexto y escritor, prevención de bucles de replay, cuarentena, aislamiento de fallos, informes y evidencia retenida acotada.
- `CalradiaForge.Desktop.Tests`: **34 aprobadas, 0 fallidas**, con cobertura del protocolo de canalizaciones con nombre/reconexión, páginas MVVM enrutadas, cancelación y disposición, transporte de sesión, uso dinámico de recursos, paridad de claves de idioma y mediciones acotadas.
- `tools/generate_desktop_resources.py`, `tools/audit_localization.py` y `tools/desktop_visual_matrix.py` pasaron. Los archivos nativos tienen BOM UTF-8 y **169** entradas con paridad inglesa en cada uno de los 13 idiomas. Desktop tiene 13 catálogos con 169 claves y 13 diccionarios WPF con 19 claves de interfaz. La matriz registra **52 casos estructurales** para 13 idiomas a 100%, 125%, 150% y 200%; no afirma que se haya realizado una inspección visual renderizada. Evidencia: `artifacts/localization-audit-1440.json`, `artifacts/desktop-visual-matrix-1440.json` y `artifacts/language-regeneration-1440.json`.
- `tools/Run-ShortTests.bat -SkipInstalledScan` se ejecutó correctamente. Es un wrapper de conveniencia de Windows que conserva el código de salida y la salida de PowerShell; `tools/build.ps1` sigue siendo el comando canónico de CI y empaquetado.

## Empaquetado

`tools/package.ps1 -Version 14.4.0` creó exactamente:

- `artifacts/CalradiaForge-Modules-14.4.0.zip`
- `artifacts/CalradiaForge-Source-SDK-14.4.0.zip`
- `artifacts/CalradiaForge-Desktop-14.4.0.zip`

La auditoría de los archivos pasó para el contenido esperado, XML, versiones, hashes, rutas duplicadas e inseguras, ausencia de partidas guardadas, ausencia de ensamblados TaleWorlds y ausencia de ejecutables app-host. El archivo Desktop incluye `Desktop/Run-CalradiaForge-Desktop.bat`, que inicia la DLL publicada mediante el runtime .NET 8 Windows Desktop instalado. Evidencia: `artifacts/package-audit-1440.json` y `artifacts/package-sha256-1440.txt`. Las carpetas temporales de preparación se eliminaron después de la auditoría.

## Comprobación previa nativa y sondeo de Steam

El archivo Modules auditado se desplegó sobre los cuatro directorios de módulos Forge existentes. Luego, `tools/Test-BannerlordPreflight.ps1 -Version 14.4.0` confirmó la superficie del juego Native 1.4.8, los manifiestos desplegados `v14.4.0` y el orden del lanzador seleccionado. No modificó los datos del lanzador, Steam, BLSE, el orden de los módulos ni las partidas guardadas. Evidencia: `artifacts/bannerlord-preflight-1440.json`.

`tools/launch_bannerlord.ps1 -Version 14.4.0` inició el lanzador oficial mediante `steam://rungameid/261550` y detectó su proceso. Se cerró mediante el comando estándar de cierre del sistema de Windows tras el breve sondeo. La superficie disponible de Computer Use no expuso ventanas de aplicaciones nativas, por lo que no se pudieron usar los controles del lanzador para entrar al menú principal. La apertura/cierre del panel nativo, el diario de Framework, la repetición `ForgeReady` y la negociación de capacidades de Desktop siguen pendientes de una sesión interactiva breve de Steam. Evidencia: `artifacts/bannerlord-steam-probe-1440.json`.
