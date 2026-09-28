# Rev085 — Confirmación de reimportación del atlas en Resource Browser

**Fecha:** 28 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Repetición de la importación del atlas Gauntlet generado y sincronización de su TPAC fuente.

## Reporte y verificación

El usuario repitió la importación del atlas para asegurar que estuviera presente el paquete de textura más reciente. Resource Browser había vuelto a su estado normal, sin confirmación ni ventana de progreso pendiente. Texture Inspector seguía mostrando `ui_calradiaforge_1` en `Modules > CalradiaForge > Assets > GauntletUI` como textura. Los detalles de origen eran 4096×512 `B8G8R8A8`; los de ejecución, 4096×512 DXT5 con 13 niveles mip.

El flujo de importación consiste en seleccionar el `ui_calradiaforge_1.png` generado y confirmar **Update** en Resource Browser. El botón **Save** de Texture Inspector solo guarda los ajustes de importación; no realiza la importación. La importación repetida terminó sin cambiar el hash del paquete registrado después de la importación completada.

## Evidencia TPAC y respaldos conservados

El paquete del módulo instalado en Steam y el paquete fuente del espacio de trabajo contienen 539 bytes y comparten el SHA-256 `8899A48A407591ADA53573EFC0DD699EA47D2A30F32A0C12C1875003F7993047`. El paquete instalado está bajo `Mount & Blade II Bannerlord/Modules/CalradiaForge/Assets/GauntletUI/`; el paquete fuente está en `modules/CalradiaForge/Assets/GauntletUI/ui_calradiaforge_1_tex.tpac`. El atlas fuente de esta importación mide 4096×512 y tiene el SHA-256 `63E94707F5119CB3025DB30748BC0A63EC5349FF3CA3367B9FC3FB154D24C372`.

Los respaldos se conservan fuera del repositorio en `%LOCALAPPDATA%/CalradiaForge/resource-browser-backups/`. El TPAC instalado previo a la importación está guardado con SHA-256 `81B828AE0718F05E8565EEAA94327B6CC6C9CDB365ADD89F724891E31B829BBA`; el TPAC fuente previo está guardado con SHA-256 `1506C5EAECD4BFC752678C6E6CDB3FA87C4A7A51D8B3B7846B15715276D571EF`. También se verificó por separado un respaldo instalado posterior a la importación contra el hash final `8899A48A...93047`.

No se usó TpacTool. La igualdad de hash y tamaño demuestra que los archivos de paquete instalado y fuente coinciden; el inspector cargado de Resource Browser demuestra el nombre, tipo, dimensiones, formato de ejecución y cantidad de niveles mip observados. Ninguno de esos datos demuestra por sí mismo un render nuevo dentro del juego. No se realizó una nueva observación F10 ni del render en vivo después de esta reimportación, por lo que la verificación más reciente dentro del juego sigue pendiente.

## Alcance y compatibilidad

Esta revisión registra la reimportación y sincronización repetidas del paquete. La versión del producto permanece en 25.2.0; no cambian API, rutas, comandos, permisos, dependencias ni ZIPs.
