# Rev046 — Alcance preciso del hit testing de evidencia WPF

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Informe de evidencia WPF Desktop y aserciones de render fuera de pantalla.

## Problema observado y justificación técnica

La documentación Rev065 decía que la acción del ledger vacío había pasado “hit testing” sin indicar que la aserción se ejecuta en un host de render fuera de pantalla. Esa formulación podía interpretarse como evidencia de entrada del puntero del sistema operativo o de un clic correcto en la aplicación en vivo. La regresión comprueba únicamente la superficie de hit testing WPF y sus antecesores visibles.

## Aclaración y límites de validación

- El arnés de render llama a `VisualTreeHelper.HitTest` en el centro del botón del ledger vacío, verifica que el elemento visual detectado pertenezca al subárbol del botón, comprueba la visibilidad y `IsHitTestVisible` en sus antecesores, y confirma que la superposición de la paleta de comandos esté contraída.
- La acción conserva el binding al `OpenPaletteCommand` existente; la ilustración adyacente es pasiva. No cambiaron rutas, comandos, API, permisos, dependencias ni comportamiento en tiempo de ejecución.
- Esta es evidencia del árbol visual WPF fuera de pantalla. No inyecta eventos del puntero del sistema operativo, no ejecuta un clic real ni demuestra el comportamiento en una ventana WPF en vivo.

## Validación y evidencia

- Artefacto final posterior a la corrección: `artifacts/desktop-visual-report-rev066-review.json` (`passed: true`).
- La compilación terminó con cero advertencias y errores; las pruebas Desktop pasaron 59/59; el informe de render contiene 281 casos y 158 pases de layout, incluida la regresión de recomendaciones que solo contienen espacios en blanco.
- El JSON registra 7.821 ms totales y 1.707,2 ms en llamadas de layout. Son mediciones del arnés, no latencia de interacción de la aplicación.
- El comportamiento del puntero del sistema operativo y de WPF en vivo sigue sin verificarse.

Rev066 del changelog fuente y Rev046 del anexo del Registro de Mejoras protegido son contadores independientes. Este anexo registra la siguiente revisión del libro protegido sin modificar evidencias anteriores. La versión del producto sigue en 25.2.0; los ZIP existentes permanecen sin cambios.
