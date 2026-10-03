# Rev120 — Evolución del SDK y conocimiento de agentes basados en evidencia

**Fecha:** 2 de octubre de 2026  
**Versión:** Calradia Forge 25.2.0, sin cambios  
**Alcance:** SDK, diagnósticos de Core, incorporación de desarrolladores, muestra de contenido, herramientas de agentes, documentación y reglas del proyecto.

## Problema observado y justificación técnica

La evolución necesitaba distinguir las capacidades entregadas de las propuestas, ofrecer un punto de inicio local y reproducible, y evitar un requisito externo de Harmony. La guía existente sobre time-slicing también debía describir su costo real de recorrido y la distribución desigual de buckets, sin insinuar una mejora general de rendimiento.

## Solución técnica y decisiones arquitectónicas

- Se añadió un paquete local `CalradiaForge.Sdk` y una plantilla de módulo `.NET new` para `net472`; los consumidores resuelven localmente las referencias de TaleWorlds y no redistribuyen ensamblados del juego.
- Se añadieron generación determinista de tropas/ítems y una muestra instalable con página Gauntlet estática. Las facciones y el comportamiento en el motor quedan aplazados hasta verificar sus esquemas y ejecución.
- La superficie retirada de snapshots Harmony se sustituyó por diagnósticos de patches propios de Forge. La observación opcional usa reflexión acotada únicamente sobre un `0Harmony` compatible y exacto que ya haya cargado el host y solo cuando se solicita; Forge no carga ni modifica Harmony.
- Se actualizaron la guía de time-slicing por ID estable y las instrucciones de agentes para indicar que los buckets pueden ser desiguales y que el filtro sigue recorriendo la colección de entrada.

## Cambios en activos, código y dependencias

Se agregaron o sincronizaron el flujo de empaquetado/plantilla del SDK, el módulo y generadores de muestra, el inspector de diagnósticos acotado, las pruebas de validación, las guías bilingües y las instrucciones especializadas de agentes. Las dependencias del producto siguen siendo independientes de Harmony; no se empaquetan ensamblados propietarios de TaleWorlds.

## Validación y límites de la evidencia

- `tools\Run-CalradiaForge-Tests.bat --no-pause`: compilaciones limpias de `net472`, `net8.0` y Desktop; en la validación original de Rev120, Core 410/410, ForgeWeave 73/73, Desktop 65/65 y WPF con 295 casos/308 pases de layout-render. El tiempo WPF corresponde solo al arnés.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat --portable`: pasaron los cuatro criterios arquitectónicos.
- Pasaron Python/Ruff, las comprobaciones de assets y los BAT de humo de onboarding/plantilla y muestra de contenido.
- La instalación de dependencias opcionales de Antigravity no quedó verificada porque el mirror del paquete devolvió `InvalidChunkLength` anteriormente.
- No se probó una sesión real de Bannerlord, campaña, batalla, runtime/coexistencia real con Harmony, publicación pública de NuGet ni integración con mercados de IDE.

Este anexo conserva el relato del changelog existente para la validación original de Rev120. La regresión posterior de precisión/pesos finitos y sus nuevos totales de pruebas quedan registrados en Rev121.
