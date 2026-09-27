# Rev041 — Pulido visual y jerarquía de Desktop WPF

**Fecha:** 26 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Desktop WPF. Tratamiento de texturas Parchment, búsqueda y selección del rail, encuadre del tablero cartográfico y estado vacío del ledger de evidencia.

## Problema observado y justificación técnica

Las texturas Parchment tenían suficiente intensidad para competir con el texto y los controles. La ruta activa del rail necesitaba un estado seleccionado observable, el campo de búsqueda requería una indicación localizada, el marco cartográfico necesitaba espacio reservado predecible y el estado vacío del ledger de evidencia requería una jerarquía visual más clara.

## Solución técnica y decisiones arquitectónicas

- Se redujo la opacidad de las texturas de superficie y título de 0,34/0,32 a 0,11/0,12.
- El estado seleccionado de la ruta activa del rail ahora es observable y la búsqueda expone la indicación localizada `Ui.SearchHint`.
- Se reservó una columna de 168 DIP para el tablero cartográfico, con un marco de 160×90 DIP y margen de 8 DIP.
- Se amplió la ilustración vacía de evidencia a 42 píxeles y el botón de acción a 32 DIP.
- Los cambios son de presentación; no se modificaron intencionalmente contratos públicos, IPC, comandos ni permisos.

## Cambios en activos, código y dependencias

Se ajustó la presentación de recursos WPF locales existentes. No se agregaron dependencias ni se regeneraron ZIPs. La versión del producto permanece en 25.2.0.

## Validación y límites de la evidencia

- La última ejecución Desktop de `tools/Run-CalradiaForge-Tests.bat` compiló sin advertencias ni errores, pasó Desktop 56/56 y completó 277 casos de render WPF con 161 pases de layout.
- El arnés registró 7.808 ms en total y 1.824,9 ms en llamadas de layout. Son mediciones del arnés, no de la latencia de la aplicación Desktop abierta.
- `tools/Test-CalradiaForge-Desktop-Uia.bat` pasó 20/20 comprobaciones acotadas y de solo lectura en `artifacts/desktop-uia-rev059.json`; solo inspeccionó el árbol de accesibilidad.
- No se afirma aprobación visual en vivo; los resultados del arnés no equivalen a inspeccionar manualmente la aplicación en ejecución.
- No cambiaron la versión del producto, los contratos públicos ni los ZIPs.

Este anexo amplía la revisión protegida anterior sin sustituir sus párrafos ni sus evidencias.
