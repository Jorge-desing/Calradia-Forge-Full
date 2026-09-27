# Rev010 — Importador C# acotado para Resource Browser

## Motivo y decisión

La infografía 1.3 propone automatizar la interfaz del Editor mediante PyAutoGUI, teclas rápidas y esperas fijas. Ese ejemplo no comprueba que el foco, la ruta, el diálogo, la importación ni el TPAC correspondan al recurso solicitado. Se conserva como referencia evaluada y no se convierte en una ruta de ejecución. El helper canónico continúa siendo `BannerlordImportAutomation` con FlaUI y confirmación visible `IMPORT`.

## Límites de entrada y destino

El helper limita la selección a archivos `.fbx` y `.png` del nivel superior y rechaza patrones con separadores de carpeta. La enumeración se detiene en el archivo 101 y el lote queda limitado a 100 entradas. Conserva la detección de nombres base duplicados entre extensiones y rechaza entradas que sean reparse points. El modo de solo lectura `--dry-run` usa estas mismas reglas sin iniciar UI Automation.

La importación exige `--module-name` con el nombre visible exacto de la carpeta del módulo. Antes de pedir autorización, lee el árbol del Resource Browser y busca una única ruta visible donde ese módulo sea el padre directo de `Assets`. Una ruta ausente, ambigua, no legible o distinta tras confirmar detiene el trabajo antes de continuar con otra importación. Los nombres visibles del menú se filtran además por el proceso del Editor asociado al Resource Browser.

## Flujo y evidencia

El helper muestra el módulo resuelto y la lista completa antes de solicitar `IMPORT`. Envía un recurso por vez. Si falla un control, aparece un estado inesperado o queda un diálogo abierto, detiene el lote sin reintentar. Espera a que el diálogo de confirmación se cierre antes de etiquetar la operación `SUBMITTED`.

`SUBMITTED` solo representa la secuencia de acciones de UI observada por el helper. No acredita una importación exitosa, una compilación, un archivo TPAC válido ni carga dentro del juego. Las guías actualizadas explican esta frontera en inglés y español y recuerdan preservar `AssetSources` y `Assets` en el árbol de autoría. El prototipo `BannerlordEditorImport.csproj` permanece como referencia; su BAT antiguo anuncia la deprecación y no compila ni ejecuta ese prototipo.

La guía de TaleWorlds para sprites confirma una ruta distinta y específica: generar la hoja con `SpriteSheetGenerator`, escanear archivos nuevos desde Resource Browser, seleccionar la categoría e importar. No establece las teclas, comandos, tiempos fijos ni métricas de rendimiento de la propuesta Python. Las cifras de velocidad y reducción de cierres no se reproducen como mediciones.

## Verificación y límites

`Test-BannerlordImportAutomation.bat` compiló el helper y ejecutó fixtures aislados de políticas y del punto de entrada CLI, cargados desde PowerShell sin iniciar un ejecutable. Pasaron los casos de extensión, filtro no recursivo, nombres Unicode/con espacios, duplicados, carpeta vacía/inexistente, argumentos de destino, rutas exactas/ausentes/ambiguas y límites de 100/101 archivos. No se abrió Resource Browser ni se importaron recursos.

La ejecución del helper mediante `Run-BannerlordImportAutomation.bat --help` fue bloqueada por el entorno con `Acceso denegado` al iniciar el ejecutable compilado. Por tanto, la invocación externa, la automatización UI real y la importación manual siguen sin verificarse en esta sesión. No se modificó la versión 22.0.0 ni se reconstruyeron ZIP; se mantiene la retención de distribución ya documentada mientras el TPAC del paquete existente siga pendiente de validación.

## Seguimiento de revisión de seguridad

La lectura de rutas del árbol ahora conserva candidatos ilegibles para que la resolución los rechace explícitamente; no los elimina antes de validar la unicidad. Solo se acepta una ruta terminal donde el módulo indicado sea el padre directo de `Assets`, evitando seleccionar una carpeta `Assets` anidada. Se añadieron fixtures para ambas condiciones. El detector de nombres duplicados compara el lote, pero no enumera lo ya instalado en el destino: revisar posibles colisiones contra recursos existentes sigue siendo una comprobación manual y el helper no garantiza si el Editor reemplazará o conservará un archivo homónimo.

La suite cubre políticas, argumentos CLI, y resolución de rutas sin abrir el Editor. `--inspect` no envía acciones según el código, pero no tuvo una prueba UI real en esta sesión. El entorno denegó el arranque del helper EXE incluso mediante `Run-BannerlordImportAutomation.bat --help`; por ello no se afirma que la automatización externa o la importación hayan sido verificadas.
