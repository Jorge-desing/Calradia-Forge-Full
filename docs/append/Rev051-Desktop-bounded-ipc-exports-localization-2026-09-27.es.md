# Rev051 — IPC Desktop acotado, exportación segura e interfaz localizada

**Fecha:** 27 de septiembre de 2026<br>
**Versión:** Calradia Forge 25.2.0, sin cambios<br>
**Alcance:** Desktop WPF independiente: lectura acotada de respuestas IPC, exportación de informes, controles accesibles para copiar y localización de visualizadores.

## Problema observado y justificación técnica

El cliente Desktop utilizaba una lectura de línea completa para respuestas IPC delimitadas por salto de línea. Un emisor podía hacer que el cliente acumulase la respuesta antes de comprobar el límite existente. Las escrituras de informes también necesitaban un comportamiento predecible ante exportaciones concurrentes y fallos, y las acciones de copia del dossier requerían nombres accesibles distintos y resultados anunciados. Algunos rótulos estáticos de visualizadores seguían incrustados en XAML a pesar de existir catálogos para 13 idiomas.

## Solución técnica y decisiones

`BoundedLineReader` lee de forma incremental con un búfer acotado, observa la cancelación y aplica el límite existente de 32 Mi caracteres UTF-16 durante la acumulación. Una respuesta excesiva falla antes de deserializar y sigue la ruta existente de desconexión; el formato de transporte y el límite no cambian.

`DesktopReportExportService` realiza las exportaciones de forma asíncrona. Prepara cada informe en un archivo temporal exclusivo dentro del directorio de destino, lo publica en una ruta única sin sobrescribir archivos existentes y elimina los temporales ante cancelación o fallo. El ViewModel recibe estados localizados y conserva intacta la evidencia retenida.

Los controles de copia del dossier usan IDs de automatización únicos; el nombre accesible de cada comando de consola incluye el comando, el control CLI tiene su propio nombre localizado y una región viva cortés anuncia el éxito o fallo. Los rótulos estáticos restantes de los visualizadores de tropas, talleres, seguridad de código, topología de módulos, diplomacia y componentes ahora provienen de los 13 diccionarios. Los rótulos largos se ajustan; se conservan nombres de ejemplo, comandos, identificadores y valores numéricos/técnicos de muestra.

## Cambios en activos, código y dependencias

- Se actualizaron la lectura IPC acotada y la exportación asíncrona de informes sin sobrescritura para Desktop.
- Se añadieron rótulos localizados y regresiones para paridad de 13 catálogos, referencias de recursos desde XAML, renderizado por ruta y controles del dossier.
- No se agregaron ni cambiaron dependencias, API pública, rutas, comandos, permisos, campos del protocolo IPC ni paquetes de distribución. La versión del producto sigue en 25.2.0; no se regeneraron ZIPs.

## Validación y límites de la evidencia

- `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-robustness-after-20260927-final.json <nul"` compiló con cero advertencias/errores, pasó Desktop 63/63 y 289 casos de render WPF con 182 pases de layout.
- El artefacto de render registra 11.758 ms totales del arnés y 3.483,7 ms en llamadas de layout. Son tiempos del arnés, no latencia de la aplicación abierta.
- `cmd.exe /d /c "tools\Test-CalradiaForge-Desktop-Uia.bat -OutputPath artifacts\desktop-robustness-after-20260927-final-uia.json -TimeoutSeconds 30 <nul"` pasó 23/23 comprobaciones UIA de solo lectura y registra `ForegroundUnchanged=true`, `ForegroundChangedByOwnedProcess=false` y ningún evento de primer plano observado del proceso inspeccionado.
- UIA inspeccionó únicamente el árbol accesible de la ventana principal lanzada. No ejercitó selectores de archivos/carpetas, ejecución de trabajos, cambios de preferencias ni aprobación visual. No se inició Bannerlord, campaña ni batalla.

Este anexo añade evidencia a la revisión anterior sin reemplazar sus párrafos. Rev071 del changelog fuente y Rev051 del Registro protegido son contadores independientes.
