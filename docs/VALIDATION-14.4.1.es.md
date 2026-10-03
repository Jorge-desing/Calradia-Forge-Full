# Calradia Forge 14.4.1 — validación breve

## Reproducido y corregido

La ventana WPF real fallaba durante `Show()` con la misma `XamlParseException` que aparecía en la captura del usuario: un binding `TwoWay` no puede escribir en `RawResult` de `ToolPageViewModel`. La propiedad `IsReadOnly` de `TextBox` no cambia el modo de binding predeterminado. Ahora el binding de salida especifica explícitamente `OneWay`. El setter del ViewModel sigue siendo privado.

El nuevo ejecutable de renderizado exclusivo de Windows hace referencia al proyecto Desktop real e instancia sus recursos `App` compilados, `MainWindow` y `DataTemplates`. Comprueba 190 rutas, las notificaciones de origen a salida y la ausencia de escritura inversa; después carga los 13 idiomas en cuatro escalas de diseño: 242 casos aprobados. El resultado inicial fallido se conserva en `artifacts/desktop-render-before.json`. La ejecución correcta está en `artifacts/desktop-render-tests.json`, con una vista previa renderizada por WPF en `artifacts/desktop-render-tests.png`. Esta es una validación real del diseño, no una afirmación de que todas las etiquetas estén traducidas por completo ni de que ningún control se recorte en todas las escalas.

## Suite breve

- Compilación Release: cero advertencias y errores.
- Suite Core y heredada: 222 aprobadas, cero fallidas; se omitió el análisis del módulo instalado.
- ForgeWeave: 24 aprobadas, cero fallidas.
- Suite de protocolo y estructura de Desktop: 34 aprobadas, cero fallidas.
- Renderizado WPF real: 242 casos aprobados, aproximadamente 2,4 segundos en la ejecución observada.
- Recursos nativos: 394 claves con BOM UTF-8 y paridad de claves entre los 13 idiomas. Las traducciones conservadas que faltan recurren al inglés; la paridad de claves por sí sola no demuestra que la traducción esté completa. Las dos etiquetas de foco nuevas tienen traducciones explícitas en los 13 idiomas.
- Tres pruebas nativas resuelven los bindings del prefab frente a propiedades y comandos compilados, verifican las flags de eventos de decoración y confirman que el foco conserva la página, el argumento y el resultado sin una sesión de ejecución.

El script de compilación regenera los recursos antes de compilar. Tanto la compilación breve como el empaquetado ejecutan el ejecutable real de regresión WPF. ForgeWeave y las pruebas de renderizado WPF ahora forman parte de la solución, lo que evita ejecutar binarios de prueba obsoletos.

## Interfaz del juego

El panel nativo añade cuatro tarjetas informativas, un encabezado de evidencia y el número de página, además de un control para enfocar la evidencia. El área visible aumenta de 372 a 552 píxeles lógicos y la fuente de evidencia aumenta de 18 a 24. Ambos estados terminan por encima de las filas de acciones inferiores. El enfoque no cambia el estado del juego, la puerta de reproducción ni los datos guardados. Framework y Extensions tienen navegación directa, y al escribir se actualiza el argumento de inmediato.

Estos cambios nativos se compilaron y se probaron contra los contratos. La apariencia y la interacción dentro de Bannerlord siguen pendientes de una breve inspección en el menú principal; no se afirma haber probado una campaña, una batalla ni una ejecución de resistencia.

## Distribución

Los tres archivos 14.4.1 siguen siendo independientes. Desktop incluye `Run-CalradiaForge-Desktop.bat` y no incluye un ejecutable app-host. La auditoría de archivos comprueba rutas, XML, versiones, hashes, contenido requerido y exclusión de archivos del juego y partidas guardadas. Consulta los resultados de auditoría de archivos en `package-audit-1441.json`.
