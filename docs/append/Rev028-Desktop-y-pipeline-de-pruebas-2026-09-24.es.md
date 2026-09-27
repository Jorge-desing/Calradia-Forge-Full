# Rev028 — optimización de Desktop y del pipeline de pruebas

**Fecha:** 2026-09-24  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** respuesta del analizador WPF, recursos de texturas y orquestación acotada de pruebas.  
**Distribución:** seguimiento solo del código fuente; sin cambios en paquetes.

## Mesa de trabajo WPF

El analizador acotado de archivos ya se ejecutaba fuera del Dispatcher WPF. Esta revisión también proyecta sus filas limitadas de evidencia y formatea el informe en esa tarea de fondo, conservando las comprobaciones de cancelación antes y después del análisis. El hilo de interfaz recibe el resultado completo; los DTOs del informe y la evidencia siguen separados de los controles WPF.

El ensamblado de ejecución ahora incluye derivaciones compactas de las texturas locales en vez de los PNG de autoría a resolución completa. El generador determinista recorta las dos franjas estrechas del título y reduce los adornos de márgenes reservados a dimensiones revisadas; los fondos materiales conservan sus dimensiones aprobadas. El conjunto de 11 recursos ocupa 6.901.545 bytes comprimidos y 11.489.464 bytes de imagen decodificada, frente a 17.864.675 y 64.474.344 del conjunto original. Reduce aproximadamente 61% los datos de texturas empaquetados y 82% los bytes decodificados. Estas cifras describen el conjunto de texturas, no la memoria del proceso ni el tiempo de arranque. El modo `--check` reprodujo cada derivación byte por byte. Las imágenes completas siguen disponibles como fuentes de autoría y no se incluyen en el ensamblado WPF.

La prueba de recursos de render ahora trata como esperado que no exista el URI de las fuentes sin optimizar y sigue fallando si esas imágenes maestras se incrustan accidentalmente. Los URI optimizados, dimensiones, modo alfa, hashes, tamaño total empaquetado y límites de imagen decodificada se comparan con los recursos de ejecución.

## Pipeline de pruebas

La prueba API reemplaza la espera fija de 500 ms con sondeo acotado de disponibilidad (intervalo de 10 ms, máximo de 2 s), libera el cliente y conserva errores accionables. En cinco ejecuciones, la mediana bajó de 2,72 s a 2,28 s. Las pruebas Asset Batch y Resource Browser ya no inician un proceso PowerShell por cada fixture: los puntos de entrada de scripts devuelven errores terminantes que se pueden componer, mientras se mantiene cubierta la ruta de error no cero del BAT. Asset Batch bajó de 14,699 s a 3,446 s; Resource Browser, de 22,390 s a 3,943 s. El validador Game Icons analiza el prefab una sola vez y una prueba protege esa condición. Los fixtures PNG del pipeline de assets usan carpetas temporales únicas, no nombres de artefacto compartidos.

El modo `--core-only` del runner compila los grafos de dependencias de pruebas Core y ForgeWeave en lugar de la solución completa y sigue ejecutando ambas suites. Una corrida medida bajó de 7,319 s a 6,115 s; el runner predeterminado aún compila y ejecuta la selección completa.

## Validación y límites

La corrida final completa mediante `.bat` compiló sin advertencias ni errores. Pasaron AssetPipeline, AssetBatchPlan, ResourceBrowser y preflight FBX; Core pasó 232/232, ForgeWeave 31/31, Desktop 42/42 y render WPF 268/268. La batería completa tardó 34,25 s e incluyó 9,866 s de harness de render con 507 pases de layout. Son observaciones del harness, no una medición de latencia de entrada de WPF en ejecución. Las compilaciones de pruebas y render Desktop también pasaron sin advertencias ni errores. No se inició una sesión del juego, campaña o batalla; no se importó TPAC, regeneró ZIP ni cambió un recurso instalado.

Esta entrada es append-only y no modifica Rev027 ni las evidencias anteriores.
