# Rev055 — Ajuste de texto del Playbook y validación exacta del tipo de barra

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Disposición del texto del Playbook detallado Gauntlet, auditoría del flujo desplazable y validación exacta del tipo de widget.

## Defecto observado y justificación técnica

Algunos títulos, pasos, textos de solución de problemas y macros recomendadas del Playbook traducidos podían exceder las filas de altura fija y quedar recortados. La auditoría de widgets también necesitaba distinguir el tipo exacto `ScrollbarWidget` del parecido visual `ScrollBarWidget`, que no es válido: una comprobación que no distingue mayúsculas y minúsculas podía dejar pasar el error de escritura aunque Gauntlet sí distinga los nombres.

## Solución técnica y decisiones arquitectónicas

El `PanelViewModel` ahora ajusta los textos existentes del Playbook y de solución de problemas en límites de palabra según el ancho disponible, sin modificar los textos fuente, comandos ni catálogo de rutas. El prefab generado coloca el texto de altura variable y los separadores pasivos en un `ListPanel` vertical ordenado con `CoverChildren`, dentro de un `ScrollablePanel` con recorte; la acción de macro queda como el último elemento acotado y sigue siendo alcanzable mediante la barra. La auditoría estructural sigue la cadena real de ancestros para comprobar que cada texto del Playbook pertenece al flujo desplazable y valida el orden de separadores y el control final de macro. La validación de widgets ahora acepta únicamente la etiqueta exacta `ScrollbarWidget` y rechaza explícitamente `ScrollBarWidget`.

## Cambios en activos, código y dependencias

- Se actualizaron `src/CalradiaForge.Mod/PanelViewModel.cs`, `tools/generate_assets.py` y `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml` para añadir el ajuste explícito de texto y el flujo vertical desplazable del Playbook.
- Se ampliaron `tools/audit_gauntlet_ui.py` y `tests/CalradiaForge.Tests/AdvancedToolsTests.cs` para comprobar pertenencia al flujo, resolución de ancestros, orden y escritura exacta con mayúsculas y minúsculas.
- No cambiaron API pública, rutas, comandos, permisos, dependencias, versión del producto ni ZIPs.

## Evidencia de validación y límites

- La primera ejecución de `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` detectó una expectativa obsoleta de la auditoría decorativa respecto a `CoverChildren`. Se actualizó la auditoría para validar el orden vertical de separadores pasivos y el final de la macro sin relajar los límites de disposición.
- La ejecución final del BAT terminó con código 0 e informó `Gauntlet visual structural checks and core tests passed`, `RESULT: 338 passed, 0 failed`.
- El registro de la sesión F10 indicó alternancia de apertura y cierre, pero la captura asociada no mostró el overlay. Por tanto, el ajuste visual y la ubicación del Playbook de este seguimiento siguen pendientes de inspección en vivo; el resultado estructural del BAT no constituye aprobación del render dentro del juego.

La versión del producto sigue en 25.2.0. Este anexo agrega evidencia después de Rev054 sin alterar los párrafos anteriores del ledger.
