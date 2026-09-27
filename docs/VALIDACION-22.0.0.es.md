# Validación de Calradia Forge 22.0.0

Primera validación: 22-09-2026. Las comprobaciones fueron breves y locales. No se iniciaron Bannerlord ni la interfaz de TpacTool; no se cargaron campañas o batallas ni se hicieron pruebas de resistencia o mediciones prolongadas.

## Correcto

- `tools/Run-CalradiaForge-Tests.bat --no-pause`: compilación con 0 advertencias y 0 errores; Core 231/231; ForgeWeave 31/31; Desktop 39/39; renderizado/recursos WPF 263 casos en 14,569 segundos.
- Los fixtures de recursos del BAT pasaron 6/6: lectura de firma/IHDR PNG, rechazo de cabecera malformada, aceptación de rutas de sprite del paquete, rechazo de respaldos temporales, instrucciones veraces de Resource Browser y rechazo de una afirmación de TPAC listo sin verificar.
- `python tools/validate_game_icon_assets.py --require-tpac` validó los 9 iconos atribuidos, `SpriteData`, referencias a piezas, coordenadas, dimensiones PNG, atlas de 2048×256 y presencia del TPAC de ejecución.
- `python tools/audit_localization.py` validó los 13 catálogos nativos con 195 claves por idioma y Desktop con 193 claves por idioma.
- `tools/Inspect-CalradiaForge-Tpac.bat --validate-only --no-pause` se ejecutó contra el paquete fuente y el instalado por Steam. Ambos miden 538 bytes y tienen SHA-256 `7DAC28A71B23C00D28F59F5E110B62F17692F2D82910F05A185DC713F5A579E8`; ambos fallan en el lector de metadatos TpacTool con `Byte array for Guid must be exactly 16 bytes long`.
- El empaquetado regenera DocFX y la ayuda contextual, reconstruye los recursos de iconos, ejecuta las pruebas por BAT y audita los tres ZIP: versiones, XML, integridad, contenido esperado, duplicados/rutas inseguras, respaldos temporales, DLL de TaleWorlds, partidas y ejecutables anfitrión `.exe`.

## Corrección estructural posterior (22-09-2026)

Una comprobación de solo lectura posterior usó `TpacTool.Lib.dll` 0.1.0 disponible localmente. El archivo Native `_shared.tpac` de esta instalación de Bannerlord se abrió correctamente y enumeró 64 activos. En cambio, el paquete de iconos de Calradia Forge, de 538 bytes, falla al leer una GUID de activo. Esto demuestra que el archivo no es legible por este parser; no certifica los datos de textura ni el renderizado. El ZIP Modules 22.0.0 contiene ese mismo archivo, así que no lo publiques como una compilación de iconos verificada.

La recolección de Resource Browser ahora exige este análisis de metadatos antes de copiar un TPAC; `tools/package.ps1` detiene la creación de archivos si el análisis falla. La prueba corta por `.bat` pasó con un paquete Native conocido, rechazó un archivo falso que solo tenía el marcador y confirmó que los reemplazos rechazados conservan la copia fuente. Hace falta repetir la importación del atlas en Resource Browser antes de reconstruir los paquetes. Bannerlord permaneció cerrado y no se repitió la comprobación visual dentro del juego.

## Límites

- El análisis PNG lee solo firma e IHDR. No decodifica píxeles, valida CRC ni evalúa la apariencia del icono.
- El auditor Desktop comprueba solo la cabecera fija v2 TPAC de 36 bytes, el conteo declarado acotado y el rango de la tabla de contenido. El preflight BAT aparte lee metadatos de activos, pero los paquetes fuente e instalado actuales fallan. TpacTool no decodifica los bloques de textura ni demuestra compatibilidad con Bannerlord 1.4.8 o renderizado visual.
- Esta entrega no repitió una comprobación en vivo de Resource Browser ni del menú del juego.

## Corrección de documentación de recursos (22-09-2026)

El generador de iconos ya no reescribe `GUI/SpriteParts/README.md` afirmando sin pruebas que el TPAC está incluido. Ahora describe las etapas separadas de atlas y Resource Browser, exige `Metadata validation: PASS` antes de recopilar o empaquetar, y aclara que leer metadatos no decodifica texturas ni demuestra el renderizado. El validador comprueba esas instrucciones con fixtures positivos y negativos. El BAT de inspección ahora acepta `--installed` y respeta `BANNERLORD_GAME_DIR` para instalaciones Steam no predeterminadas; un fixture lo verificó con una TPAC Native conocida.

Tras ese cambio se repitió el BAT de pruebas cortas completo: compilación con 0 advertencias/errores, Core 231/231, ForgeWeave 31/31, Desktop 39/39, fixtures de recursos 6/6 y 263 casos de render/recursos WPF en 14,569 segundos. Juego y editor permanecieron cerrados. Los TPAC fuente e instalado siguen sin poder leerse con el parser disponible; el cambio documental no libera el empaquetado.

## Corrección de contraste de temas (22-09-2026)

El monograma del sello ahora usa `LogoTextBrush`, con un color específico por tema que contrasta con el escudo. Los selectores y sus opciones usan los recursos dinámicos de entrada, texto y selección; el foco de teclado conserva el token existente. La ejecución BAT solo de Desktop pasó con 0 advertencias/errores, 39/39 pruebas MVVM/protocolo y 263 casos de render WPF en 19,572 segundos. La matriz ahora comprueba el contraste del monograma y valida el primer plano y fondo de los selectores en cada tema.

## Diagnóstico de cabecera TPAC (22-09-2026)

El analizador local de recursos ahora lee la cabecera fija TPAC v2 completa de 36 bytes, en vez de tratar el marcador de cuatro bytes como única señal estructural. Los fixtures cubren una cantidad plausible y acotada de entradas, cabecera truncada, cantidad cero o excesiva y una tabla fuera del archivo. Una cabecera plausible todavía no certifica metadatos de activos ni datos de textura; el hallazgo dirige al preflight separado de metadatos TpacTool. Los TPAC fuente e instalado de Calradia Forge siguen sin poder leerse con el parser disponible; no se reconstruyeron ZIP ni se afirmó una validación dentro del juego.

La ejecución corta `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` reconstruyó la solución con 0 advertencias/errores y pasó Core 231/231 y ForgeWeave 31/31. El preflight de solo lectura del módulo instalado sigue terminando con código 1 e informa `Byte array for Guid must be exactly 16 bytes long`; este fallo esperado confirma que el bloqueo de publicación continúa.

El preflight ahora separa el resultado de cabecera/tabla del fallo del lector de metadatos e incluye el motivo del parser, sin una traza extensa de PowerShell. No infiere corrupción ni ordena reimportar sin revisar el resultado. La prueba BAT de Resource Browser pasó con metadatos malformados y confirmó que la recolección rechazada conserva intacto el hash del TPAC fuente.

## Corrección del alcance de formato (22-09-2026)

El analizador v2 ahora se detiene antes de leer campos específicos cuando el archivo declara otra versión y la marca como no compatible con este análisis. Los chequeos de rango de la cabecera v2 son solo evidencia estructural. El flujo oficial de sprites de Bannerlord documenta generación del atlas seguida de importación por Resource Browser; este proyecto no encontró una API pública C# de importación FBX por lotes ni un contrato documentado de sidecars `.meta` en los ensamblados administrados instalados o en las instrucciones oficiales consultadas, por lo que no genera metadatos especulativos.

## Validación breve del seguimiento de fuentes (22-09-2026)

La solución se recompiló con 0 advertencias y 0 errores. Pasaron las suites `.bat`: Core 231/231, ForgeWeave 31/31, Desktop 40/40, renderizado y recursos WPF 264 casos en 15,921 segundos, Asset Pipeline 7/7 y los fixtures de staging/parser de Resource Browser. Desktop comprueba selección con diálogos de archivo/carpeta, conservación de la entrada al cancelar y desactivación de selectores al liberar la página. El preflight del módulo instalado continúa devolviendo error deliberadamente: los límites de cabecera/tabla v2 pasan, pero el parseo TpacTool falla con `Byte array for Guid must be exactly 16 bytes long`; el archivo no se modificó. No se reconstruyeron ZIP y Bannerlord permaneció cerrado.

`tests/CalradiaForge.ResourceBrowser.Tests.bat --no-pause` pasó los fixtures de staging y parser, incluidos el diagnóstico breve y la conservación del hash fuente. `tests/CalradiaForge.AssetPipeline.Tests.bat --no-pause` pasó 7/7 fixtures. `tools/Build-CalradiaForge-UiAssets.bat --dry-run --no-pause` validó nueve iconos atribuidos y el atlas 2048×256; no inició el generador oficial.
