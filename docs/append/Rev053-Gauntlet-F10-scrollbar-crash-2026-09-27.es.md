# Rev053 — Corrección del bloqueo F10 por la barra de desplazamiento de Gauntlet

**Fecha:** 27 de septiembre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** Generación del prefab Gauntlet, auditoría estructural de barras de desplazamiento y validación en el menú principal para un jugador.

## Defecto observado y justificación técnica

Al presionar F10 en el menú principal apareció primero una aserción de Gauntlet de TaleWorlds indicando que `CreateBuiltinWidget(ScrollBarWidget)` no encontraba el widget integrado solicitado. Al reintentar se mostró una violación de acceso en `ScrollablePanel.UpdateScrollablePanel(Single)`. La inspección encontró que el panel de desplazamiento de Evidence generado no respetaba el contrato del prefab nativo: la referencia a la barra y la estructura anidada no resolvían a una barra hermana válida con un control asociado.

## Solución técnica y decisiones arquitectónicas

El prefab generado ahora enlaza `ForgeEvidenceScroll` con la barra hermana `ForgeEvidenceScrollBar` mediante la referencia relativa `VerticalScrollbar="..\\ForgeEvidenceScrollBar"`. La barra es un `ScrollbarWidget` hermano con `AlignmentAxis="Vertical"`, la propiedad `Handle` y un control hijo identificado correspondiente. El generador aplica el mismo contrato estructural a las cinco superficies desplazables. El auditor Gauntlet verifica el inventario de cinco paneles, las referencias relativas, la posición hermana, el eje vertical, la pertenencia del control y la ausencia de barras anidadas. La regresión de la superficie de evidencia permite únicamente la imagen propia del control de la barra y sigue rechazando sprites decorativos en el ledger.

## Cambios en activos, código y dependencias

- Se actualizaron `tools/generate_assets.py` y `modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml` para generar la disposición compatible con el widget nativo.
- Se reforzó `tools/audit_gauntlet_ui.py` y se ajustó `tests/CalradiaForge.Tests/AdvancedToolsTests.cs` para permitir la excepción del control de la barra.
- No cambiaron API pública, rutas, comandos, permisos, dependencias ni versión del producto. No se generó ningún ZIP ni se reemplazó un TPAC.

## Evidencia de validación y límites

- `tools/Run-CalradiaForge-Tests.bat --core-only --no-pause` compiló con cero advertencias/errores y pasó Core 333/333 y ForgeWeave 73/73.
- `tools/Run-CalradiaForge-Gauntlet-Visual-Checks.bat --no-pause` pasó las auditorías de recursos decorativos y de estructura Gauntlet con cero errores y cero advertencias; la suite Core de esa ejecución pasó 338/338.
- `tools/Deploy-CalradiaForge-ToGame.bat --no-pause` compiló y desplegó los perfiles Client y Modding Kit con respaldos de archivos verificados. Se conservó el TPAC instalado existente.
- En una sesión de Bannerlord iniciada desde Steam y con Calradia Forge v25.2.0 habilitado, F10 abrió el panel en el menú principal para un jugador y una segunda pulsación de F10 lo cerró. Se mostraron las ocho áreas de navegación y el juego continuó ejecutándose sin la aserción ni la violación de acceso. No se cargó campaña ni batalla.
- Esta comprobación en vivo confirma únicamente la ruta probada del menú principal; no establece el comportamiento en ejecución del Modding Kit ni valida todos los estados de las áreas desplazables en otras pantallas.

La versión del producto sigue en 25.2.0. Los párrafos y revisiones anteriores del ledger no se modifican.
