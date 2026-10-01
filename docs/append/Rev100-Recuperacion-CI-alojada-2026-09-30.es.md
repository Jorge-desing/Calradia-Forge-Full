# Rev100 — Recuperación de CI alojada

**Fecha:** 30 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0; sin cambios  
**Alcance:** CI, herramientas Python y auditorías de assets y documentación.

## Problema observado y justificación técnica

La compilación alojada de la solución incluía proyectos Bannerlord sin ensamblados TaleWorlds licenciados ni GameBin configurado. Core ya protege sus fuentes del motor y admite intencionalmente net472 y net8.0; eliminar su destino portable rompería la arquitectura existente. Las auditorías conservaban supuestos sobre registros de parches mutables, un único archivo de simulación Desktop y workspaces Gauntlet visibles simultáneamente.

## Solución técnica y decisiones arquitectónicas

La CI alojada usa CalradiaForge.Portable.slnf y el launcher BAT existente para compilación y pruebas portables. Las compilaciones locales completas conservan la integración cuando GameBin está disponible. Las auditorías del registro reúnen todos los parciales DesktopSimulationViewModels y comprueban snapshots inmutables actuales. La auditoría decorativa excluye únicamente pares de visibilidad comprobados en el ViewModel y admite la cláusula actual del margen de Campaign Rule Builder; un fixture cubre visibilidad heredada y exclusión. Esta corrección de CI no cambia frameworks ni dependencias de runtime.

## Cambios en código, activos y dependencias

Actualizados los workflows CI, el BAT de pruebas, las auditorías Python y fixtures de assets. El changeset previsto excluye binarios propietarios, cachés, capturas de pruebas y ZIPs de distribución.

## Validación y límites de la evidencia

- BAT completo: compilación Release limpia, cero advertencias/errores; Core 401/401, ForgeWeave 73/73, Desktop 65/65 y render WPF 292/292. El fixture serial x64 aplicó y revirtió correctamente (15 → 32 → 15).
- BAT portable: pasaron compilación, assets, ForgeWeave, Desktop y render WPF.
- BAT Python CI: pasaron Ruff, 12 fixtures de assets, 5 pruebas de imágenes, sprites decorativos y metadatos de iconos.
- Las auditorías BAT del registro pasaron antes de este anexo; el registro ampliado se comprobará otra vez antes del commit.
- La instalación opcional de agentes falló localmente por una respuesta truncada del mirror configurado. No se declara aprobado ese perfil local.
- Antes de subir, Actions público contenía 35 ejecuciones CI fallidas, 4 del registro y 11 del workflow Conda antiguo. Un push inicia checks nuevos; los estados históricos no se reescriben. La validación remota queda pendiente en este anexo.
- No se inició Bannerlord, campaña ni batalla. La validación alojada portable no demuestra integración del juego ni seguridad concurrente de detours.

Este anexo conserva los párrafos previos y la versión del producto.
