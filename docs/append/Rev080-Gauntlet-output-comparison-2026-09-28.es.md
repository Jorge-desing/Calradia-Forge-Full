# Rev080 — Comparación acotada de salidas en Gauntlet

**Fecha:** 28 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Mod y recursos de localización. Presentación de salidas Gauntlet y guía bilingüe de interfaz.

## Necesidad observada y justificación técnica

El filtro de salida en vivo permite acotar lo que se muestra, pero no conserva un resultado como referencia mientras una ejecución posterior produce una salida nueva. La comparación existente de snapshots de objetos cubre otro flujo y debe seguir siendo independiente. Los desarrolladores necesitan revisar dos salidas de texto completas sin reemplazar la salida original, el argumento de la herramienta, el historial de comandos ni la evidencia retenida.

## Solución técnica y decisiones arquitectónicas

Se añade una línea base explícita que dura lo mismo que el panel, una vista de comparación en dos columnas y una acción explícita para borrar la línea base. Se conservan las líneas idénticas y se alinean inserciones y eliminaciones en parejas de filas. El filtro existente, que no distingue mayúsculas, busca en ambos lados originales y conserva una pareja cuando coincide cualquiera de sus celdas. El ajuste de línea y la paginación permanecen sincronizados en páginas de 16 filas visuales. Una comparación activa se actualiza al llegar una salida nueva; la navegación entre áreas conserva la línea base. Limpiar la salida actual abandona la vista comparativa, pero conserva la línea base. La comparación queda limitada a 256 Ki caracteres y 4.096 líneas por lado, con un máximo de 8.192 filas alineadas, 500.000 pasos de búsqueda y 262.144 celdas de traza. Si se supera un límite configurado, aparece un estado localizado sin comparación parcial; intentar fijar una línea base demasiado grande no la modifica.

## Cambios en activos, código y dependencias

El panel Gauntlet incorpora acciones localizadas y encabezados para las salidas base y actual. Los recursos de idioma fuente y generados, la lista de claves del generador y la guía de diseño de interfaz en inglés y español documentan el flujo exclusivo de comparación de salidas. Los comandos y contratos de la comparación de snapshots de objetos permanecen separados. No se añade una dependencia externa en tiempo de ejecución ni se cambia la versión del producto.

## Validación y límites de la evidencia

- La paridad de catálogos, la estructura visual y las pruebas de ejecución están pendientes; este anexo no afirma que esas comprobaciones hayan pasado.
- Esta actualización documental no incluye una inspección en vivo de Bannerlord ni de la comparación dentro del juego.
- La función no modifica la salida sin procesar, el argumento de la herramienta, el historial de comandos, los datos del portapapeles o la exportación, ni la evidencia retenida.

Este anexo registra el comportamiento de comparación de salidas acotada sin reemplazar revisiones anteriores.
