# Rev078 — filtro de salida en vivo de Gauntlet

**Fecha:** 27 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Añadir un filtro de texto de presentación para la salida actual de la herramienta nativa.

## Comportamiento previsto

La consulta de salida es independiente del argumento de la herramienta y del historial de comandos. Filtra líneas sin distinguir mayúsculas, permanece en memoria mientras el panel está abierto aunque se cambie de área y se aplica a las nuevas salidas. Las líneas coincidentes mantienen el ajuste de línea y la paginación existentes. Una consulta vacía muestra la salida sin filtrar y, si no hay coincidencias, aparece un estado localizado que lo explica. La salida original y la evidencia retenida no se modifican.

## Validación

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause`: aprobado; compilación limpia `net472` sin advertencias, Core 340/340 y ForgeWeave 73/73.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause`: aprobó las comprobaciones de sprites, la auditoría de prefab/disposición, los recursos generados y localizados, y la suite Core. La auditoría exige un ancho mínimo usable para el filtro en todos los viewports de referencia.
- Se compararon las traducciones del filtro con los 13 recursos de idioma generados para Bannerlord. Una consulta compuesta solo por espacios se trata de forma coherente como vacía.
- La revisión de F10 fue solo estática: no se encontró un defecto concreto en `SubModule.cs` ni en el orden de telemetría; la prueba existente comprueba estructura del código, no simula la entrada.

No se inició una sesión de Bannerlord. La importación en Resource Browser, la actualización del TPAC, el renderizado y la interacción dentro del juego siguen sin verificarse; el launcher visual indica que el TPAC instalado es anterior al atlas fuente. Las comprobaciones estáticas y automatizadas no prueban el comportamiento de Gauntlet en ejecución.
