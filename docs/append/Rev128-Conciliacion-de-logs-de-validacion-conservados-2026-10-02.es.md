# Rev128 — Conciliación de evidencia y contratos del showcase

**Fecha:** 02 de octubre de 2026  
**Versión del producto:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Verificación estática del showcase, evidencia de validación, documentación de evolución del SDK y registros append-only.

## Hallazgo

Rev127 informa una ejecución integrada final con Core 415/415, Patch Diagnostics 30/30, 320 pases WPF de layout/render y 20.903 ms. El último log integrado conservado en este checkout, `artifacts/sdk-evolution/post-final-reviewed-20261002.log`, registra una ejecución aprobada con Core 410/410, Patch Diagnostics 28/28, ForgeWeave 73/73, Desktop 65/65 y 295 casos WPF con 308 pases de layout/render. Su duración de render es 24.476 ms. No se encontró un log conservado que reproduzca los totales y la duración exactos de Rev127.

## Corrección y límites de evidencia

La discrepancia es una brecha de retención y reproducibilidad independiente; no demuestra que la ejecución reportada después haya fallado o no haya ocurrido. Rev127 se conserva intacta. Esta revisión identifica el log conservado como el resultado inspeccionable de forma independiente y marca los totales superiores de Rev127 como reportados, pero no reproducidos independientemente desde este checkout. El benchmark de `ForgeTimeSlicer.ProcessBatch` sigue siendo una medición sintética del arnés; no mide un callback de campaña completo ni el rendimiento dentro de Bannerlord.

El generador del showcase ahora comprueba que las entradas EN/SP de `language_data.xml` usen el `xml_path` esperado, que cada ruta permanezca dentro del directorio del idioma y que el archivo exista. También valida que el botón de cierre acepte foco y que su texto use `@CloseLabel`. Las regresiones negativas cubren rutas malformadas, archivos ausentes, estado de foco y binding del rótulo localizado. El BAT conservado valida estos contratos de fuente, el contenido generado, los esquemas, la salida determinista y la compilación del módulo; no demuestra render Gauntlet en vivo.

El log integrado conservado informa suites locales aprobadas y compilaciones limpias. Esas comprobaciones no permiten inferir un resultado de runtime o render dentro del juego.

## Validación

- Se inspeccionó `artifacts/sdk-evolution/post-final-reviewed-20261002.log` y se confirmaron sus resúmenes de suites, casos de render, pases de layout y duración.
- Se ejecutó `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` tras añadir las regresiones estáticas negativas; el log está en `artifacts/sdk-evolution/showcase-rev128.log`.
- Se conservó Rev127 y se anexó esta aclaración como Rev128 sin reescribir apéndices anteriores.
- Verificar la cadena de hashes con `tools\Append-CalradiaForge-Improvement-Record.bat --verify` tras compilar el registro protegido.
