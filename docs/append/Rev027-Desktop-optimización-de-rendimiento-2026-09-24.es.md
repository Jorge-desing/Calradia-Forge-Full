# Rev027 — optimización de Desktop y de las pruebas

**Fecha:** 2026-09-24  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** Búsqueda y colecciones WPF, virtualización del rail y orquestación de pruebas Desktop.  
**Distribución:** Seguimiento solo del código fuente; sin cambios en paquetes.

## Mesa de trabajo WPF

El shell construye una sola vez el texto de búsqueda de cada herramienta inmutable, omite reemplazos de colección sin cambios, agrupa los reinicios de listas y reutiliza los modelos de grupo mientras sigan visibles. Los snapshots de favoritos y recientes se conservan hasta que cambien sus listas fuente. El rail operativo usa una lista virtualizada con reciclaje: el harness encontró sus 194 rutas y 11 grupos en el origen de datos, pero solo materializó 10 filas al inicio y 7 después de desplazarse al footer (206 elementos contando encabezados y footer). Pinned y Recent mantienen viewports independientes y acotados.

## Mantenimiento de pruebas

El lanzador de solo Desktop usa `CalradiaForge.Desktop.slnf` para compilar ambos proyectos de prueba Desktop y sus dependencias transitivas en una sola llamada a `dotnet build`. Evita compilar toda la solución y repetir compilaciones del grafo compartido. El proceso de pruebas reutiliza los textos fuente y documentos XML analizados. El registro conserva 42 casos activos; se retiraron 31 helpers no registrados que comprobaban contratos de una interfaz anterior, después de trasladar sus aserciones útiles a pruebas activas.

El render WPF ya no programa una espera `ApplicationIdle` del Dispatcher en cada frame; `UpdateLayout()` se ejecuta de forma síncrona en el hilo STA de pruebas. Permanecen activas las comprobaciones de rutas, localización, temas/escalas, accesibilidad, contraste, estados vacíos, texturas, preferencias y viewports. La ejecución completa del `.bat` de Desktop pasó sin advertencias ni errores de compilación, con 42/42 pruebas unitarias y 268/268 casos de render WPF. Completó 507 pases de render/layout en 9,822 s; la corrida Desktop completa tardó 13,974 s. La corrida anterior del mismo turno tardó 12,314 s en el harness de render y 17,874 s en la ejecución completa. Son duraciones del harness, no mediciones del rendimiento del producto.

No cambiaron la versión, la API pública, los paquetes, el TPAC ni los recursos instalados del juego. No se inició campaña ni batalla.

Este registro es append-only y no modifica Rev021–Rev026 ni evidencias anteriores.
