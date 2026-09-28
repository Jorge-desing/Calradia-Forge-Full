# Rev083 — Verificación del TPAC del atlas Gauntlet en Resource Browser

**Fecha:** 28 de septiembre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios
**Alcance:** Importación del atlas Gauntlet y verificación del recurso instalado.

## Problema observado y justificación técnica

Rev082 dejó pendiente el atlas Gauntlet actual y su importación mediante Resource Browser. El módulo instalado contenía un `ui_calradiaforge_1_tex.tpac` anterior, por lo que el atlas fuente regenerado todavía no podía considerarse el paquete de textura disponible para el módulo instalado. Las notas previas distinguen la selección genérica de fuentes de recursos del flujo de importación del atlas de SpriteSheetGenerator.

## Solución técnica y decisiones arquitectónicas

Se siguió el flujo documentado del atlas en Resource Browser: seleccionar el recurso de textura existente `ui_calradiaforge_1` en `Modules > CalradiaForge > Assets > GauntletUI`, conservar los ajustes de importación mostrados y guardar el recurso del atlas generado. Después se actualizó Resource Browser y se volvió a cargar el recurso seleccionado en el inspector de texturas. No se usó TpacTool porque su lector instalado está desactualizado y no es confiable para este paquete.

## Cambios en activos, código y dependencias

El archivo instalado `Assets/GauntletUI/ui_calradiaforge_1_tex.tpac` cambió de SHA-256 `69513B5617F026E1F106F6CA6D47CF5C5CE0B5869FFA472E9166B037CA8EDEA6` (539 bytes) a `131E623077708032C76790324F31EDC7862BDC6D5ACA79350B4A03C241A0529E` (539 bytes). El paquete instalado anterior se conserva en `C:\Users\Alex\AppData\Local\CalradiaForge\resource-browser-backups\Rev082-20260928-073823-248\installed-ui_calradiaforge_1_tex.tpac`; su hash se volvió a comprobar tras la importación. El respaldo del TPAC fuente también se conserva y se volvió a comprobar.

El TPAC fuente del espacio de trabajo no cambió; conserva SHA-256 `8619B64C386D61364A0599574C70C7B0F0F4149CF406EADE77BEE726D726D5E5` (538 bytes). No se recopiló desde el módulo instalado: la herramienta de recopilación existente exige validar los metadatos con el lector heredado de TpacTool, que deliberadamente no se consideró confiable ni se ejecutó. No cambiaron el código fuente, la versión del producto ni los paquetes ZIP.

## Validación y límites de la evidencia

- El inspector de texturas de Resource Browser mostró el recurso `ui_calradiaforge_1` como una textura. Los detalles de fuente fueron 4096×512, `B8G8R8A8`; los detalles de ejecución fueron 4096×512, DXT5, con 13 niveles mip. Estos detalles volvieron a estar visibles después de guardar y actualizar el navegador.
- Se usó una vez la acción Save de Resource Browser con los ajustes existentes: Albedo/DXT5 RGBA, sin cambio de tamaño y con mipmaps habilitados. No apareció un diálogo de confirmación. Las comprobaciones SHA-256 confirmaron que los respaldos previos del archivo instalado y del archivo fuente permanecieron intactos.
- La importación del TPAC queda verificada en Resource Browser por el nombre, tipo y dimensiones del recurso cargado. Esto no valida la composición completa del panel Gauntlet ni su aspecto visual dentro de una partida ejecutándose. Para esta importación no se inició campaña, batalla ni prueba F10.
- El operador detuvo la sesión posterior de Computer Use con Escape; no se realizaron más acciones en la interfaz.
- No se usó el lector TpacTool. El paquete instalado se conserva como importación verificada; la recopilación hacia la copia fuente queda pendiente hasta contar con una ruta de validación confiable.

Este anexo agrega evidencia sin reemplazar el texto de revisiones anteriores. El producto permanece en 25.2.0; no cambian API pública, rutas, comandos, permisos ni paquetes ZIP.
