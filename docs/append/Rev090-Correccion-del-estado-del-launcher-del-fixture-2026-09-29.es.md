# Rev090 — Reconciliación archivística de la fuente

**Fecha:** 29 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Recuperación archivística de la fuente desde el changelog publicado; sin cambios de código ni ejecución.

**Nota de reconciliación archivística:** Este anexo fuente se reconstruyó el 30/09/2026 a partir de las secciones ya publicadas en los changelogs inglés y español. No existía un anexo fuente original de esta revisión en `docs/append/`. Este respaldo no modifica ni sustituye ningún DOCX protegido o registro de integridad previo.

## Corrección de estado del launcher del fixture Calradia Forge — 29/09/2026 (Rev090)

- El wrapper PowerShell de timeout para el fixture nativo desechable no devolvió el control de forma fiable y se retiró. El BAT del fixture volvió a lanzar directamente el proceso hijo; no se afirma que haya un timeout garantizado.
- El smoke nativo actual sigue bloqueado/sin verificar. Las regresiones administradas Core más recientes pasan 363/363 y el fixture x64 compila limpiamente, pero el BAT Core no termina porque el proceso nativo hijo se bloquea. La evidencia anterior de fixture aprobado permanece limitada al árbol previo, registrado en Rev088.
- Esta entrada corrige únicamente la afirmación de timeout de Rev089. No hubo sesión del juego/Modding Kit ni cambio de ZIP; la versión permanece en 25.2.0 y el motor de parcheo sigue siendo experimental.
