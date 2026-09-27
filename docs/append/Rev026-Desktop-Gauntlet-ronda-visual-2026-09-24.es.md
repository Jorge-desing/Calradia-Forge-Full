# Rev026 — ronda visual de Desktop y Gauntlet

**Fecha:** 2026-09-24  
**Línea de versión:** Calradia Forge 22.0.0  
**Alcance:** Temas, texturas y navegación de Desktop WPF, más la validación de fuentes Gauntlet.  
**Distribución:** Seguimiento solo del código fuente; no se modificaron TPAC ni ZIP.

## Desktop WPF

Desktop usa materiales ilustrados empaquetados localmente para War Table y Parchment Light; High Contrast conserva superficies sólidas. La barra de título personalizada expone controles de minimizar, maximizar, restaurar y cerrar con nombres localizados y accesibles. El emblema sigue siendo un botón accesible enlazado al comando existente de la paleta. Un interruptor limitado a la sesión controla las ilustraciones pasivas; el ledger de evidencia vacío muestra texto localizado, una ilustración pasiva y una acción que abre la paleta existente. Los estados seleccionado y enfocado de ComboBox se adaptan al tema. El rail principal usa un `VirtualizingStackPanel` con reciclaje y conserva todas las rutas del catálogo y los viewports independientes de historial Pinned/Recent.

La comprobación WPF encontró 11 PNG locales empaquetados, incluido `parchment-field-journal-ornament-v1.png`, visible solo en Parchment (1254×1254 RGBA; SHA-256 `B6EBC6EECA36266E53B53DE9BB372C07A655DF946B42B9D570714E417B163AD3`). Las comprobaciones de High Contrast y del interruptor confirmaron superficies de color sólido y capas decorativas ocultas donde corresponde.

`tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` compiló las dependencias de las pruebas Desktop sin advertencias ni errores. Las pruebas unitarias Desktop pasaron 42/42; la suite de render WPF pasó 268 casos: 194 rutas del catálogo, 13 idiomas a 100%, 125%, 150% y 200% DPI, y tres temas en las mismas cuatro escalas con 20 rutas representativas por tema/escala. La suite también comprobó el footer materializado y sus viewports de historial independientes, el contraste de ComboBox, la accesibilidad y transiciones de la barra de título, el layout del ledger vacío y que las preferencias del usuario no cambiaran. La corrida tardó 10,047 s, con 507 pases de render/layout y 3151,5 ms dentro de `UpdateLayout`. Quitar la espera central de Dispatcher en prioridad ApplicationIdle mejoró los 12,3 s de la corrida inmediata anterior, aunque el resultado sigue 1,387 s por encima de la referencia anterior de 8,66 s. Son mediciones del harness WPF, no del rendimiento del producto. Las capturas y resultados actuales están en `artifacts/desktop-render-tests-en-100-current.png`, `artifacts/desktop-render-tests-en-200-current.png`, `artifacts/desktop-render-tests-war-table.png`, `artifacts/desktop-render-tests-parchment.png`, `artifacts/desktop-render-tests-high-contrast.png` y `artifacts/desktop-render-tests.json`.

## Validación de fuentes Gauntlet

El validador de fuentes pasó cuatro contratos PNG preparados: tela de 128×64 con alfa 22–24, overlay de 256×48 con alfa 0–112, regla de latón de 128×16 con alfa 82–88 y fieltro de 128×32 con alfa 37–40. La preparación de texturas fue determinista, produjo hashes idénticos y dejó intactas las fuentes. SpriteData se reportó vigente y la auditoría estructural del prefab terminó con cero errores y cero avisos. Core pasó 232/232.

## Límite de ejecución

El TPAC instalado es anterior al atlas fuente. No se reemplazó TPAC, no se importó mediante Resource Browser ni se observó un render dentro del juego; esas comprobaciones siguen pendientes. No se inició ninguna campaña ni batalla. Calradia Forge continúa en 22.0.0 y no se generaron ZIP.

Este registro es append-only y no modifica Rev021–Rev025 ni evidencias anteriores.
