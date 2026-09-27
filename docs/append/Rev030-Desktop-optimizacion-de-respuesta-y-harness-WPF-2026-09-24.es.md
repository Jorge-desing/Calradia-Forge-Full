# Rev030 — respuesta de Desktop y optimización del harness WPF

**Fecha:** 24-09-2026  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** medición de respuesta WPF, corrección de métricas asíncronas y cobertura representativa de render.  
**Distribución:** seguimiento solo del código fuente; sin cambios en paquetes.

## Medición de operaciones WPF

Esta revisión enfoca la medición en el arranque, el filtrado de herramientas, la selección de rutas y las operaciones asíncronas para optimizar según costos observados y no según suposiciones. Se usa 16,7 ms como referencia para señalar interacciones lentas a 60 Hz. Los tiempos del harness se mantienen separados de la latencia medida en la aplicación en ejecución.

Los contadores `GC.GetAllocatedBytesForCurrentThread` son locales al hilo. Describen una sección síncrona en el mismo hilo, pero no miden correctamente una operación que cruza `await` y puede continuar en otro hilo. Por eso, las métricas asíncronas conservan la duración y muestran los bytes asignados como no disponibles; las mediciones síncronas pueden seguir informando bytes.

## Cobertura del harness de render

El catálogo contiene 194 rutas que comparten una plantilla de página de trabajo. La suite conserva las comprobaciones de cada ruta, icono, enlace, selección y liberación, pero reserva el layout WPF completo para herramientas representativas y escenarios que cubren estados visuales distintos. La matriz de render mantiene etiquetas largas, evidencias vacías y pobladas, temas, idiomas compatibles y escalas del 100–200%. Así se evita repetir layout sin reducir las comprobaciones de contrato de las rutas.

El harness registra por separado el arranque, el filtrado, la navegación, las operaciones asíncronas y la duración de render. La actualización del filtro midió p50 de 0,08 ms y p95/máximo de 9,28 ms; ninguna muestra alcanzó 16,7 ms. La selección de rutas midió p50 de 0,62 ms y p95 de 1,16 ms; una de 194 selecciones superó 16,7 ms (máximo de 23,46 ms), por lo que el umbral es una señal diagnóstica y no una afirmación sobre la latencia total de la aplicación. Son mediciones sintéticas, no representan la latencia de extremo a extremo en una sesión de usuario.

Cinco corridas del BAT de render terminaron en 5,675, 5,677, 5,749, 5,703 y 5,738 segundos. La mediana de 5,703 segundos está por debajo de la meta aprobada de 9,0 segundos y es 46,0% menor que la referencia de 10,559 segundos. La ejecución verificada más reciente compiló con cero advertencias/errores, pasó Desktop 51/51 y 271 casos de render/recursos WPF, y usó 146 pases de layout representativos. El informe limita expresamente estos tiempos al harness; no miden la latencia del producto en vivo.

## Alcance de regresiones y limpieza

La cobertura de regresión incluye filtrado rápido y vacío, navegación y liberación de páginas, semántica de duración/asignaciones asíncronas, cancelación y estados del ledger. Al vaciar el filtro se libera la página seleccionada y se limpia su contenido; una página liberada ignora resultados, cancelaciones o errores asíncronos tardíos. No se eliminó más código de producción sin encontrar un caso confirmado de código sin uso. Antes de una futura eliminación se deben revisar referencias en C# y XAML, recursos dinámicos, reflexión, pruebas y empaquetado. No se ejecutó la comprobación UI Automation de WPF: la política de la herramienta rechazó el comando aislado de lanzamiento/inspección, por lo que no se afirma un resultado actual de la ventana del shell.

## Validación y límites

La compilación, las pruebas Desktop y de render, la paridad de 13 idiomas, la matriz de tres temas y las escalas y la comparación de cinco corridas pasaron mediante archivos `.bat` del repositorio. El harness cubre 100%, 125%, 150% y 200%; pasaron las suites de rutas, idiomas y temas. No se lanzó directamente ningún `.exe` ni `.dll` de pruebas. La inspección UI Automation queda sin ejecutar; el arranque e identificación de la ventana en un proceso separado siguen sin verificarse. La respuesta en ejecución se mide por separado del harness de render. La aplicación conserva `net8.0-windows`; la API pública, IPC, versión del producto, recursos del juego y comportamiento de Bannerlord no cambian.

Esta entrada es append-only y no modifica Rev029 ni evidencias anteriores.
