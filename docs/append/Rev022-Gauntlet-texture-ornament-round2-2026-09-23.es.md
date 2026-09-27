# Rev022 — Refinamiento de texturas decorativas de Gauntlet

**Fecha:** 2026-09-23  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** Fuentes originales de sprites decorativos de Gauntlet y sus ubicaciones acotadas en el prefab.  
**Distribución:** Actualización posterior del código fuente; los ZIP existentes no se modificaron ni regeneraron.

## Fuentes y ubicación de las texturas decorativas

La segunda ronda visual añade tres fuentes de sprites originales, generadas de forma reproducible: `forge_table_grain` (128 × 64), una superposición sutil y transparente a eventos para el marco de la cabecera y del rail; `forge_map_contours` (128 × 64), restringida a la zona ornamental de la cabecera detrás del filigrana existente; y `forge_brass_rule` (128 × 16), usada como separador entre secciones reales. Sus referencias en SpriteData y en el prefab forman parte de la actualización del código fuente. La zona de comandos, la entrada editable y el ledger de evidencias siguen sobre superficies limpias para que el detalle de las texturas no compita con las acciones ni con el texto técnico.

Estos recursos generados por el proyecto no añaden dependencias en tiempo de ejecución ni ilustraciones externas. Los recursos existentes de Game-icons y sus atribuciones no cambian. Las ubicaciones decorativas siguen siendo pasivas y acotadas; no se superponen a objetivos interactivos ni modifican la navegación, los comandos, las evidencias, la paginación o las protecciones del estado del juego.

## Validación y estado de entrega

La validación estructural está pendiente de ejecución para esta revisión. Los cambios en el código fuente no demuestran que el atlas generado o el paquete de texturas de ejecución contengan estas incorporaciones. El TPAC existente está desactualizado respecto de la actualización de los sprites fuente; la importación mediante Resource Browser y la visualización en vivo dentro del juego siguen pendientes. No se afirma ningún resultado visual en tiempo de ejecución. El producto permanece en la versión 22.0.0 y no se regeneraron ZIPs.

Este registro es append-only y no modifica Rev021 ni las evidencias anteriores.

## Nota de verificación posterior — sesión 2026-09-23

El atlas fuente se generó y sincronizó con `CalradiaForgeSpriteData.xml`; el validador confirmó los recortes de las tres texturas ornamentales nuevas. El Resource Browser estuvo abierto, pero en el árbol `Modules` solo aparecieron `Native` y `SandBox`, sin `CalradiaForge`. La aplicación se había iniciado sin argumentos explícitos para cargar módulos. El intento de filtrar la vista mediante UI Automation venció el tiempo; por ello no se importó un TPAC y no se verificó el render en vivo. Estos resultados no cambian el estado pendiente de la importación ni de la comprobación visual en ejecución.
