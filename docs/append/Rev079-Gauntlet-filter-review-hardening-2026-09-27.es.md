# Rev079 — refuerzo tras la revisión del filtro Gauntlet

**Fecha:** 27 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Resolver dos hallazgos de revisión estática de la ronda del filtro de salida y registrar los límites de la revisión de F10.

## Hallazgos y correcciones

- El encabezado de evidencia compartía una fila fija de 205 DIP con el filtro y mostraba el nombre del área activa. Los títulos largos del SDK podían recortarse o acercarse al filtro. Ahora usa la etiqueta localizada y breve `Evidence`; el título del área permanece en la cabecera del espacio de trabajo. Los catálogos fuente y los recursos de juego generados incluyen la etiqueta en los 13 idiomas compatibles.
- `SubModule.Open()` construía `GauntletLayer` antes del bloque `try` de limpieza. Si el constructor fallaba después de crear el ViewModel del panel, podía quedar sin finalizar. La construcción de la capa ahora queda dentro del bloque protegido, y `Close()` finaliza el ViewModel incluso si nunca se asignó una capa. Una regresión estática protege este recorrido.
- La revisión estática no determinó la causa de la aserción histórica de F10. La prueba de la tecla sigue comprobando el orden del código; no simula entradas de Bannerlord, aserciones nativas ni el renderizado de Gauntlet. No se inició el juego ni se intentó reproducir F10.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: aprobado; las compilaciones `net472` y `net8.0` informaron cero advertencias y errores, Core 340/340 y ForgeWeave 73/73.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: aprobado; la validación de sprites y la auditoría estructural/de disposición de Gauntlet reportaron cero errores y cero avisos; Core pasó 340/340.
- Las comprobaciones del atlas fuente y de SpriteData son estáticas. El TPAC instalado es anterior al atlas fuente; siguen pendientes la importación, la actualización del TPAC y el renderizado de Gauntlet en vivo.
- La revisión del ciclo de F10 encontró que la telemetría de sesión se almacena en búfer antes de persistirse periódicamente, por lo que una terminación nativa abrupta podría omitir los últimos registros. Se consigna como límite de evidencia; no se cambió sin una causa verificable en ejecución.
