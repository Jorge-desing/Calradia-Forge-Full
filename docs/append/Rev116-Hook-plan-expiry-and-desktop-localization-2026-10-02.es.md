# Rev116 — Caducidad de planes de hooks e integridad de localización Desktop

**Fecha:** 2 de octubre de 2026
**Versión:** Calradia Forge 25.2.0, sin cambios; API SDK 13
**Alcance:** Caducidad de planes en Core, generación de localización WPF Desktop, proyección de estados, herramientas de prueba y documentación bilingüe.

## Problema observado y justificación técnica

La confirmación del plan aceptaba el token en el instante exacto de `ExpiresAtUtc` porque la caducidad usaba una comparación estricta mayor que. Por separado, el generador de recursos WPF reescribía los diccionarios completos desde un subconjunto fijo de 73 claves aunque los diccionarios comprometidos contenían 248 entradas, eliminando silenciosamente 175 cadenas de interfaz por idioma al regenerarlos. Los estados del Hook Workbench también conservaban el texto localizado anterior cuando el usuario cambiaba el idioma de Desktop.

## Solución técnica y decisiones arquitectónicas

La caducidad de planes ahora incluye el límite exacto y una regresión comprueba que el plan está vencido cuando `now == ExpiresAtUtc`. El generador Desktop lee un catálogo de extensiones explícito con traducciones para los 13 idiomas, conserva las 248 claves existentes y agrega un estado localizado para el cambio de idioma. Se rechazan las claves JSON duplicadas y las claves duplicadas en recursos generados; la auditoría de localización comprueba los valores exactos de las extensiones y la cobertura de idiomas. El Hook Workbench guarda un resolver para sus estados de interfaz y lo actualiza al cambiar la localización. Los errores crudos del host y su detalle de diagnóstico permanecen intactos.

## Cambios en activos, código y dependencias

El cambio actualiza la comparación de caducidad y su regresión Core, los ViewModels Desktop de localización y estado, los 13 diccionarios WPF generados, `localization/desktop-extensions.json`, la localización nativa de `Cancel` del Hook Workbench y su flujo de generación/auditoría, las comprobaciones Python mediante BAT, la documentación SDK/bibliotecas compartidas y las fuentes bilingües de changelog/registro. Se conservaron los valores de traducción WPF existentes; no cambiaron la versión, API SDK, comandos, rutas, comportamiento de hooks ni dependencias de ejecución.

## Validación y límites de la evidencia

- `tools/Run-CalradiaForge-Tests.bat --desktop-only --no-pause` pasó Desktop 65/65 y 295 casos de render WPF / 308 pases de layout.
- `tools/Run-CalradiaForge-Tests.bat --no-pause` pasó Core 414/414, el fixture serial x64 de hooks (incluidos Finalizer e ILHook), ForgeWeave 73/73, Desktop 65/65 y WPF con 295/295 casos de render / 308 pases de layout; la compilación reportó 0 advertencias y 0 errores.
- `tools/Run-CalradiaForge-Python-Checks.bat --ci --no-pause` pasó las comprobaciones de assets, localización, sprites e iconos; `tools/Verify-CalradiaForge-StatelessBehavior.bat` pasó las cuatro etapas de aceptación.
- Los tiempos WPF miden el harness de render, no la latencia interactiva de la aplicación. No se abrió ni verificó Bannerlord, campaña, batalla o una importación activa en Resource Browser.
- El empaquetado canónico y los hashes de los archivos son un gate de entrega separado y se registran en el informe de entrega.

Este anexo amplía el registro protegido sin reescribir revisiones anteriores.
