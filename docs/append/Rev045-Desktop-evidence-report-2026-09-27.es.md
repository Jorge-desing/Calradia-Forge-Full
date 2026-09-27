# Rev045 — Superficie más clara para el informe de evidencia WPF

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Pestañas de informe WPF Desktop, presentación del ledger de evidencia y regresiones de render.

## Problema observado y justificación técnica

Las vistas Evidence y Raw Result no tenían una jerarquía visual clara en sus pestañas, por lo que era difícil identificar qué superficie del informe estaba seleccionada. Los hallazgos poblados aparecían como texto indiferenciado y la acción del estado vacío, aunque visible, estaba dentro de un contenedor que desactivaba el hit testing y no podía recibir entrada del puntero.

## Solución técnica y decisiones arquitectónicas

- Se agregó una plantilla explícita de `TabControl` y un estilo de `TabItem` para el informe. Los encabezados localizados Evidence y Raw Result ahora incluyen iconos semánticos, estados seleccionado/hover/foco y un conteo dinámico de evidencias.
- Las filas de evidencia se reorganizaron como tarjetas limpias que separan origen/estado, ID de regla, ubicación, texto de evidencia y recomendación. No se agregó ningún contrato nuevo de ViewModel ni comando.
- La ilustración del estado vacío sigue siendo pasiva, pero se restauró el hit testing de su contenedor para que el botón localizado invoque el `OpenPaletteCommand` existente.
- Se ampliaron las aserciones del render WPF para comprobar etiquetas localizadas, evidencia poblada realista, binding e hit testing de la acción vacía, y límites. El arnés fuera de pantalla no equivale a una inspección de la ventana en vivo.

## Cambios en activos, código y dependencias

Se actualizaron `src/CalradiaForge.Desktop/App.xaml`, `src/CalradiaForge.Desktop/Resources/Views/ToolPageTemplates.xaml` y `tests/CalradiaForge.Desktop.RenderTests/Program.cs`. No se agregaron texturas, dependencias, API pública, rutas, comandos, permisos ni activos empaquetados. El ZIP existente 25.2.0 permanece intacto.

## Validación y límites de la evidencia

- Se ejecutó `cmd.exe /d /c "tools\Run-CalradiaForge-Tests.bat --desktop-only --no-pause --render-output artifacts\desktop-visual-report-rev065-fix1.json"`.
- La compilación terminó con cero advertencias y errores; Desktop pasó 59/59; las comprobaciones de render/recursos WPF pasaron 281/281 con 158 pases de layout.
- El informe JSON registró 7.856 ms totales y 1.705,8 ms en llamadas síncronas de layout. Son mediciones del arnés, no latencia de la aplicación abierta.
- Se revisó la vista renderizada `artifacts/desktop-visual-report-rev065-fix1.png`. No se inspeccionó la aplicación en vivo.

Este anexo amplía la revisión protegida anterior sin sustituir párrafos ni evidencia previos.
